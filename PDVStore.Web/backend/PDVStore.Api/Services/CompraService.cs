using System.Data;
using Microsoft.EntityFrameworkCore;
using PDVStore.Api.Contracts;
using PDVStore.Api.Data;
using PDVStore.Api.Models;

namespace PDVStore.Api.Services;

/// <summary>
/// Compras de fornecedor. Preserva o CompraService original: ao menos um item,
/// quantidade > 0, custo >= 0, produtos existentes, total pela soma dos
/// subtotais, entrada de estoque, atualização do preço de custo quando > 0 e
/// movimentação "Entrada". Cancelamento reduz o estoque (nunca abaixo de zero).
/// </summary>
public class CompraService
{
    private readonly PdvStoreDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CompraService> _logger;

    public CompraService(PdvStoreDbContext context, ICurrentUser currentUser, ILogger<CompraService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<CompraDto> RegistrarAsync(CriarCompraRequest request, CancellationToken cancellationToken)
    {
        if (request.Itens == null || request.Itens.Count == 0)
            throw new ValidacaoException("A compra deve conter pelo menos um item.");
        if (request.Itens.Any(i => i.Quantidade <= 0))
            throw new ValidacaoException("Quantidade dos itens deve ser maior que zero.");
        if (request.Itens.Any(i => i.PrecoCusto < 0))
            throw new ValidacaoException("Preço de custo não pode ser negativo.");

        var usuarioId = _currentUser.Id
            ?? throw new ValidacaoException("Usuário autenticado não identificado.");

        await using var transacao = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var fornecedor = await _context.Fornecedores
            .FirstOrDefaultAsync(f => f.Id == request.FornecedorId, cancellationToken)
            ?? throw new NaoEncontradoException($"Fornecedor {request.FornecedorId} não encontrado.");

        if (!fornecedor.Ativo)
            throw new ConflitoException("Fornecedor informado não está ativo.");

        var ids = request.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await _context.Produtos
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in request.Itens)
        {
            if (!produtos.ContainsKey(item.ProdutoId))
                throw new ValidacaoException($"Produto ID {item.ProdutoId} não encontrado.");
        }

        var compra = new Compra
        {
            FornecedorId = fornecedor.Id,
            Fornecedor = fornecedor,
            UsuarioCaixaId = usuarioId,
            DataCompra = DateTime.UtcNow,
            NumeroNota = string.IsNullOrWhiteSpace(request.NumeroNota) ? null : request.NumeroNota.Trim(),
            Status = "Concluida",
            Itens = request.Itens.Select(i => new ItemCompra
            {
                ProdutoId = i.ProdutoId,
                Quantidade = i.Quantidade,
                PrecoCusto = i.PrecoCusto
            }).ToList()
        };

        compra.ValorTotal = compra.Itens.Sum(i => i.Subtotal);

        _context.Compras.Add(compra);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var item in compra.Itens)
        {
            var produto = produtos[item.ProdutoId];
            produto.Estoque += item.Quantidade;
            if (item.PrecoCusto > 0)
                produto.PrecoCusto = item.PrecoCusto;

            _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                ProdutoId = produto.Id,
                Tipo = "Entrada",
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoCusto,
                DataMovimentacao = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Motivo = $"Compra #{compra.Id} - {compra.NumeroNota ?? "sem nota"}"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        _logger.LogInformation("Compra {CompraId} registrada por {UsuarioId} com total {Total}",
            compra.Id, usuarioId, compra.ValorTotal);

        return compra.ToDto();
    }

    public async Task<PagedResult<CompraDto>> ListarAsync(CompraQuery query, CancellationToken cancellationToken)
    {
        var (inicio, fim) = PeriodoHelper.Normalizar(query.Inicio, query.Fim);
        var consulta = _context.Compras
            .AsNoTracking()
            .Include(c => c.Fornecedor)
            .Include(c => c.UsuarioCaixa)
            .Include(c => c.Itens).ThenInclude(i => i.Produto)
            .Where(c => c.DataCompra >= inicio && c.DataCompra <= fim);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            consulta = consulta.Where(c => c.Status == status);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var compras = await consulta
            .OrderByDescending(c => c.DataCompra)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<CompraDto>.Create(
            compras.Select(c => c.ToDto()).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<CompraDto> ObterPorIdAsync(int id, CancellationToken cancellationToken)
    {
        var compra = await _context.Compras
            .AsNoTracking()
            .Include(c => c.Fornecedor)
            .Include(c => c.UsuarioCaixa)
            .Include(c => c.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Compra {id} não encontrada.");

        return compra.ToDto();
    }

    /// <summary>Cancela a compra e estorna o estoque (mesma regra do original).</summary>
    public async Task<CompraDto> CancelarAsync(int id, CancellationToken cancellationToken)
    {
        await using var transacao = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var compra = await _context.Compras
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Compra {id} não encontrada.");

        if (compra.Status == "Cancelada")
            throw new ConflitoException("A compra já está cancelada.");

        var ids = compra.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await _context.Produtos
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in compra.Itens)
        {
            if (produtos.TryGetValue(item.ProdutoId, out var produto))
                produto.Estoque = Math.Max(0, produto.Estoque - item.Quantidade);
        }

        compra.Status = "Cancelada";
        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        _logger.LogInformation("Compra {CompraId} cancelada por {UsuarioId}", compra.Id, _currentUser.Id);

        var fornecedor = await _context.Fornecedores.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == compra.FornecedorId, cancellationToken);
        compra.Fornecedor = fornecedor;
        return compra.ToDto();
    }
}
