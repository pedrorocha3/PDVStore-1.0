using System.Data;
using Microsoft.EntityFrameworkCore;
using PDVStore.Api.Contracts;
using PDVStore.Api.Data;
using PDVStore.Api.Models;

namespace PDVStore.Api.Services;

/// <summary>
/// Núcleo do PDV. Reproduz VendaService.RegistrarVendaAsync e CancelarVendaAsync
/// com as mesmas regras e ordem: caixa aberto, fiado com cliente ativo e limite,
/// total = itens - desconto (nunca negativo), estoque validado antes de gravar,
/// baixa + MovimentacaoEstoque "Saída", saldo devedor e pagamento mock (PIX).
/// Tudo em uma única transação Serializable.
/// </summary>
public class VendaService
{
    private readonly PdvStoreDbContext _context;
    private readonly IPagamentoIntegrator _pagamento;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<VendaService> _logger;

    public VendaService(
        PdvStoreDbContext context,
        IPagamentoIntegrator pagamento,
        ICurrentUser currentUser,
        ILogger<VendaService> logger)
    {
        _context = context;
        _pagamento = pagamento;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<VendaDto> RegistrarAsync(CriarVendaRequest request, CancellationToken cancellationToken)
    {
        if (request.Itens == null || request.Itens.Count == 0)
            throw new ValidacaoException("A venda deve conter pelo menos um item.");

        if (string.IsNullOrWhiteSpace(request.FormaPagamento))
            throw new ValidacaoException("Selecione uma forma de pagamento.");

        if (request.Itens.Any(i => i.Quantidade <= 0))
            throw new ValidacaoException("Quantidade deve ser maior que zero.");

        var usuarioId = _currentUser.Id
            ?? throw new ValidacaoException("Usuário autenticado não identificado.");

        var formaPagamento = request.FormaPagamento.Trim();
        bool fiado = formaPagamento.Equals("Fiado", StringComparison.OrdinalIgnoreCase);

        await using var transacao = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var caixa = await _context.Caixas
            .FirstOrDefaultAsync(c => c.Status == "Aberto", cancellationToken)
            ?? throw new ConflitoException("Nenhum caixa aberto. Abra o caixa antes de registrar vendas.");

        Cliente? cliente = null;
        if (fiado)
        {
            if (!request.ClienteId.HasValue)
                throw new ValidacaoException("Venda fiada exige um cliente selecionado.");

            cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == request.ClienteId.Value, cancellationToken);

            if (cliente == null || !cliente.Ativo)
                throw new ConflitoException("Cliente informado não está ativo.");
        }

        // Consolida linhas repetidas do mesmo produto para validar estoque corretamente.
        var quantidades = request.Itens
            .GroupBy(i => i.ProdutoId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantidade));

        var produtos = await _context.Produtos
            .Where(p => quantidades.Keys.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var produtoId in quantidades.Keys)
        {
            if (!produtos.ContainsKey(produtoId))
                throw new ValidacaoException($"Produto ID {produtoId} não encontrado.");
        }

        // O preço vem SEMPRE do cadastro do produto, nunca do cliente HTTP.
        decimal totalItens = quantidades.Sum(q => q.Value * produtos[q.Key].Preco);
        decimal valorTotal = totalItens - request.Desconto;

        if (valorTotal < 0)
            throw new ValidacaoException("O valor total da venda não pode ser negativo.");

        if (fiado && cliente != null && cliente.LimiteCredito > 0 &&
            cliente.SaldoDevedor + valorTotal > cliente.LimiteCredito)
        {
            throw new ConflitoException(
                $"Limite de crédito do cliente excedido. Saldo atual: {cliente.SaldoDevedor:C2}.");
        }

        foreach (var (produtoId, quantidade) in quantidades)
        {
            var produto = produtos[produtoId];
            if (produto.Estoque < quantidade)
                throw new ConflitoException($"Estoque insuficiente para o produto '{produto.Nome}'.");
        }


        var venda = new Venda
        {
            UsuarioCaixaId = usuarioId,
            DataVenda = DateTime.UtcNow,
            ValorTotal = valorTotal,
            Desconto = request.Desconto,
            FormaPagamento = formaPagamento,
            Status = "Concluida",
            CaixaId = caixa.Id,
            ClienteId = fiado ? request.ClienteId : null,
            Itens = quantidades.Select(q => new ItemVenda
            {
                ProdutoId = q.Key,
                Quantidade = q.Value,
                PrecoUnitario = produtos[q.Key].Preco
            }).ToList()
        };

        _context.Vendas.Add(venda);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var item in venda.Itens)
        {
            var produto = produtos[item.ProdutoId];
            produto.Estoque -= item.Quantidade;

            _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                ProdutoId = item.ProdutoId,
                Tipo = "Saída",
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.PrecoCusto,
                DataMovimentacao = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Motivo = $"Venda #{venda.Id}",
                ReferenciaVendaId = venda.Id
            });
        }

        if (fiado && cliente != null)
            cliente.SaldoDevedor += venda.ValorTotal;

        if (!await _pagamento.ProcessarPagamentoAsync(venda, cancellationToken))
            throw new ConflitoException("Falha ao processar pagamento.");

        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        _logger.LogInformation("Venda {VendaId} registrada por {UsuarioId}: {Total} ({Forma})",
            venda.Id, usuarioId, venda.ValorTotal, venda.FormaPagamento);

        return MontarDto(venda, produtos, cliente);
    }

    /// <summary>
    /// Cancela a venda e desfaz todos os efeitos: devolve estoque, registra
    /// movimentação de estorno e reduz o saldo devedor (fiado) sem ficar negativo.
    /// </summary>
    public async Task<VendaDto> CancelarAsync(int vendaId, string? motivo, CancellationToken cancellationToken)
    {
        await using var transacao = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var venda = await _context.Vendas
            .Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.Id == vendaId, cancellationToken)
            ?? throw new NaoEncontradoException($"Venda {vendaId} não encontrada.");

        if (venda.Status == "Cancelada")
            throw new ConflitoException("A venda já está cancelada.");

        var idsProdutos = venda.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await _context.Produtos
            .Where(p => idsProdutos.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in venda.Itens)
        {
            if (produtos.TryGetValue(item.ProdutoId, out var produto))
                produto.Estoque += item.Quantidade;

            _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                ProdutoId = item.ProdutoId,
                Tipo = "Entrada",
                Quantidade = item.Quantidade,
                DataMovimentacao = DateTime.UtcNow,
                UsuarioId = _currentUser.Id,
                Motivo = $"Estorno - Venda #{venda.Id}",
                ReferenciaVendaId = venda.Id
            });
        }

        Cliente? clienteVenda = null;
        if (venda.ClienteId.HasValue &&
            venda.FormaPagamento.Equals("Fiado", StringComparison.OrdinalIgnoreCase))
        {
            clienteVenda = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == venda.ClienteId.Value, cancellationToken);
            if (clienteVenda != null)
                clienteVenda.SaldoDevedor = Math.Max(0, clienteVenda.SaldoDevedor - venda.ValorTotal);
        }

        venda.Status = "Cancelada";
        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        _logger.LogInformation("Venda {VendaId} cancelada por {UsuarioId}. Motivo informado: {Motivo}",
            venda.Id, _currentUser.Id, motivo ?? "(não informado)");

        return MontarDto(venda, produtos, clienteVenda);
    }


    public async Task<VendaDto> ObterPorIdAsync(int id, CancellationToken cancellationToken)
    {
        var venda = await _context.Vendas
            .AsNoTracking()
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.UsuarioCaixa)
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Venda {id} não encontrada.");

        return venda.ToDto();
    }

    public async Task<PagedResult<VendaDto>> ListarAsync(VendaQuery query, CancellationToken cancellationToken)
    {
        var (inicio, fim) = PeriodoHelper.Normalizar(query.Inicio, query.Fim);
        var consulta = _context.Vendas
            .AsNoTracking()
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.UsuarioCaixa)
            .Include(v => v.Cliente)
            .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim);

        if (query.CaixaId.HasValue)
            consulta = consulta.Where(v => v.CaixaId == query.CaixaId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            consulta = consulta.Where(v => v.Status == status);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var vendas = await consulta
            .OrderByDescending(v => v.DataVenda)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<VendaDto>.Create(
            vendas.Select(v => v.ToDto()).ToList(), query.Page, query.PageSize, total);
    }

    /// <summary>Totais do período e distribuição por forma de pagamento (somente concluídas).</summary>
    public async Task<ResumoVendasDto> ResumoAsync(DateTime? inicio, DateTime? fim, CancellationToken cancellationToken)
    {
        var (de, ate) = PeriodoHelper.Normalizar(inicio, fim);
        var consulta = _context.Vendas
            .AsNoTracking()
            .Where(v => v.DataVenda >= de && v.DataVenda <= ate && v.Status == "Concluida");

        var quantidade = await consulta.CountAsync(cancellationToken);
        var total = await consulta.SumAsync(v => v.ValorTotal, cancellationToken);
        var porForma = await consulta
            .GroupBy(v => v.FormaPagamento)
            .Select(g => new { Forma = g.Key, Quantidade = g.Count(), Total = g.Sum(v => v.ValorTotal) })
            .ToListAsync(cancellationToken);

        return new ResumoVendasDto(de, ate, quantidade, total,
            porForma.Select(g => new ResumoFormaPagamentoDto(g.Forma, g.Quantidade, g.Total)).ToList());
    }

    private VendaDto MontarDto(Venda venda, IReadOnlyDictionary<int, Produto> produtos, Cliente? cliente)
    {
        var itens = venda.Itens
            .OrderBy(i => i.Id)
            .Select(i => new ItemVendaDto(
                i.Id, i.ProdutoId,
                produtos.TryGetValue(i.ProdutoId, out var p) ? p.Nome : i.Produto?.Nome,
                i.Quantidade, i.PrecoUnitario, i.Subtotal))
            .ToList();

        return new VendaDto(
            venda.Id, venda.UsuarioCaixaId, _currentUser.Nome, venda.DataVenda,
            venda.ValorTotal, venda.Desconto, venda.FormaPagamento, venda.PixTxId,
            venda.Status, venda.CaixaId, venda.ClienteId, cliente?.Nome, itens);
    }
}
