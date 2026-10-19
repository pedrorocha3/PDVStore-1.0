using System.Data;
using Microsoft.EntityFrameworkCore;
using PDVStore.Api.Contracts;
using PDVStore.Api.Data;
using PDVStore.Api.Models;

namespace PDVStore.Api.Services;

/// <summary>
/// Movimentações manuais de estoque e histórico. Preserva as regras do
/// EstoqueService original: quantidade > 0, produto existente, saída não pode
/// deixar saldo negativo e cada ajuste gera MovimentacaoEstoque com usuário,
/// motivo e data UTC. Operações críticas usam transação Serializable para
/// evitar perda de atualização sem alterar o schema compartilhado.
/// </summary>
public class EstoqueService
{
    private readonly PdvStoreDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EstoqueService> _logger;

    public EstoqueService(PdvStoreDbContext context, ICurrentUser currentUser, ILogger<EstoqueService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public Task<MovimentacaoEstoqueDto> EntradaAsync(MovimentacaoRequest request, CancellationToken cancellationToken)
        => MovimentarAsync(request, entrada: true, cancellationToken);

    public Task<MovimentacaoEstoqueDto> SaidaAsync(MovimentacaoRequest request, CancellationToken cancellationToken)
        => MovimentarAsync(request, entrada: false, cancellationToken);

    private async Task<MovimentacaoEstoqueDto> MovimentarAsync(
        MovimentacaoRequest request, bool entrada, CancellationToken cancellationToken)
    {
        if (request.Quantidade <= 0)
            throw new ValidacaoException("Quantidade deve ser maior que zero.");

        await using var transacao = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var produto = await _context.Produtos
            .FirstOrDefaultAsync(p => p.Id == request.ProdutoId, cancellationToken)
            ?? throw new NaoEncontradoException($"Produto {request.ProdutoId} não encontrado.");

        if (!entrada && produto.Estoque < request.Quantidade)
            throw new ConflitoException($"Estoque insuficiente para o produto '{produto.Nome}'.");

        produto.Estoque += entrada ? request.Quantidade : -request.Quantidade;

        var movimentacao = new MovimentacaoEstoque
        {
            ProdutoId = produto.Id,
            Tipo = entrada ? "Entrada" : "Saída",
            Quantidade = request.Quantidade,
            DataMovimentacao = DateTime.UtcNow,
            UsuarioId = _currentUser.Id,
            Motivo = string.IsNullOrWhiteSpace(request.Motivo) ? null : request.Motivo.Trim()
        };

        _context.MovimentacoesEstoque.Add(movimentacao);
        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Estoque {Tipo}: produto {ProdutoId} quantidade {Quantidade} por usuário {UsuarioId}",
            movimentacao.Tipo, produto.Id, request.Quantidade, _currentUser.Id);

        movimentacao.Produto = produto;
        return movimentacao.ToDto();
    }

    public async Task<PagedResult<MovimentacaoEstoqueDto>> HistoricoAsync(
        MovimentacaoQuery query, CancellationToken cancellationToken)
    {
        var consulta = _context.MovimentacoesEstoque
            .AsNoTracking()
            .Include(m => m.Produto)
            .Include(m => m.Usuario)
            .AsQueryable();

        if (query.Inicio.HasValue)
        {
            var inicio = query.Inicio.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(query.Inicio.Value, DateTimeKind.Utc)
                : query.Inicio.Value.ToUniversalTime();
            consulta = consulta.Where(m => m.DataMovimentacao >= inicio);
        }

        if (query.Fim.HasValue)
        {
            var fim = query.Fim.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(query.Fim.Value, DateTimeKind.Utc)
                : query.Fim.Value.ToUniversalTime();
            consulta = consulta.Where(m => m.DataMovimentacao <= fim);
        }

        if (query.ProdutoId.HasValue)
            consulta = consulta.Where(m => m.ProdutoId == query.ProdutoId.Value);

        if (!string.IsNullOrWhiteSpace(query.Tipo))
        {
            var tipo = query.Tipo.Trim();
            consulta = consulta.Where(m => m.Tipo == tipo);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var itens = await consulta
            .OrderByDescending(m => m.DataMovimentacao)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => m.ToDto())
            .ToListAsync(cancellationToken);

        return PagedResult<MovimentacaoEstoqueDto>.Create(itens, query.Page, query.PageSize, total);
    }

    /// <summary>Produtos ativos com saldo menor ou igual ao estoque mínimo.</summary>
    public async Task<List<ProdutoDto>> AlertasAsync(CancellationToken cancellationToken)
    {
        return await _context.Produtos
            .AsNoTracking()
            .Where(p => p.Ativo && p.Estoque <= p.EstoqueMinimo)
            .OrderBy(p => p.Nome)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken);
    }
}
