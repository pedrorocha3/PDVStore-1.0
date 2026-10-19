using Microsoft.EntityFrameworkCore;
using PDVStore.Api.Contracts;
using PDVStore.Api.Data;

namespace PDVStore.Api.Services;

/// <summary>
/// Relatórios do dashboard. Preserva as consultas do RelatorioService e do
/// DashboardViewModel originais: apenas vendas concluídas, período em UTC,
/// ranking por quantidade vendida, alerta de estoque (saldo &lt;= mínimo) e
/// agrupamentos por forma de pagamento/dia.
/// </summary>
public class RelatorioService
{
    private readonly PdvStoreDbContext _context;

    public RelatorioService(PdvStoreDbContext context)
    {
        _context = context;
    }

    public async Task<List<ItemRelatorioDto>> ItensMaisVendidosAsync(
        DateTime? inicio, DateTime? fim, int topN, CancellationToken cancellationToken)
    {
        var (de, ate) = PeriodoHelper.Normalizar(inicio, fim);
        var limite = topN is < 1 or > 100 ? 10 : topN;

        var consulta = from item in _context.ItensVendas.AsNoTracking()
                       join produto in _context.Produtos.AsNoTracking() on item.ProdutoId equals produto.Id
                       join venda in _context.Vendas.AsNoTracking() on item.VendaId equals venda.Id
                       where venda.Status == "Concluida" && venda.DataVenda >= de && venda.DataVenda <= ate
                       group new { item, produto } by produto into g
                       select new
                       {
                           g.Key.Nome,
                           g.Key.CodigoBarras,
                           TotalVendido = g.Sum(x => x.item.Quantidade),
                           ValorTotalVendido = g.Sum(x => x.item.Quantidade * x.item.PrecoUnitario),
                           g.Key.Estoque,
                           g.Key.EstoqueMinimo
                       };

        var dados = await consulta
            .OrderByDescending(x => x.TotalVendido)
            .Take(limite)
            .ToListAsync(cancellationToken);

        return dados.Select(x => new ItemRelatorioDto(
            x.Nome, x.CodigoBarras, x.TotalVendido, x.ValorTotalVendido,
            x.Estoque, x.EstoqueMinimo,
            x.Estoque <= x.EstoqueMinimo ? "Baixo" : "OK")).ToList();
    }

    public async Task<List<ProdutoDto>> EstoqueMinimoAsync(CancellationToken cancellationToken)
    {
        return await _context.Produtos
            .AsNoTracking()
            .Where(p => p.Ativo && p.Estoque <= p.EstoqueMinimo)
            .OrderBy(p => p.Nome)
            .Select(p => new ProdutoDto(p.Id, p.Nome, p.CodigoBarras, p.Preco, p.PrecoCusto,
                p.Estoque, p.EstoqueMinimo, p.Categoria, p.Descricao, p.Ativo))
            .ToListAsync(cancellationToken);
    }

    public async Task<DashboardDto> DashboardAsync(DateTime? inicio, DateTime? fim, CancellationToken cancellationToken)
    {
        var (de, ate) = PeriodoHelper.Normalizar(inicio, fim);

        var vendas = _context.Vendas.AsNoTracking()
            .Where(v => v.DataVenda >= de && v.DataVenda <= ate && v.Status == "Concluida");

        var quantidadeVendas = await vendas.CountAsync(cancellationToken);
        var totalPeriodo = await vendas.SumAsync(v => v.ValorTotal, cancellationToken);

        var pagamentos = await vendas
            .GroupBy(v => v.FormaPagamento)
            .Select(g => new { Forma = g.Key, Quantidade = g.Count(), Total = g.Sum(v => v.ValorTotal) })
            .ToListAsync(cancellationToken);

        var vendasDia = await vendas
            .Select(v => new { v.DataVenda, v.ValorTotal })
            .ToListAsync(cancellationToken);

        var movimentos = await _context.MovimentacoesEstoque.AsNoTracking()
            .Where(m => m.DataMovimentacao >= de && m.DataMovimentacao <= ate)
            .Select(m => new { m.DataMovimentacao, m.Tipo, m.Quantidade })
            .ToListAsync(cancellationToken);

        var itens = await ItensMaisVendidosAsync(de, ate, 10, cancellationToken);
        var alertas = await EstoqueMinimoAsync(cancellationToken);

        return new DashboardDto(
            de, ate, totalPeriodo, quantidadeVendas, itens, alertas,
            pagamentos.Select(g => new ResumoFormaPagamentoDto(g.Forma, g.Quantidade, g.Total)).ToList(),
            vendasDia.GroupBy(v => v.DataVenda.Date)
                .OrderBy(g => g.Key)
                .Select(g => new ResumoDiaDto(g.Key, g.Sum(v => v.ValorTotal)))
                .ToList(),
            movimentos.GroupBy(m => m.DataMovimentacao.Date)
                .OrderBy(g => g.Key)
                .Select(g => new MovimentacaoDiaDto(
                    g.Key,
                    g.Where(m => m.Tipo == "Entrada").Sum(m => m.Quantidade),
                    g.Where(m => m.Tipo == "Saída").Sum(m => m.Quantidade)))
                .ToList());
    }
}
