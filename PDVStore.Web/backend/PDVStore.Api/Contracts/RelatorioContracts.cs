namespace PDVStore.Api.Contracts;

public record ItemRelatorioDto(
    string NomeProduto,
    string? CodigoBarras,
    int TotalVendido,
    decimal ValorTotalVendido,
    int EstoqueAtual,
    int EstoqueMinimo,
    string StatusMinimo);

public record ResumoDiaDto(DateTime Dia, decimal Total);

public record MovimentacaoDiaDto(DateTime Dia, int Entradas, int Saidas);

public record DashboardDto(
    DateTime Inicio,
    DateTime Fim,
    decimal TotalPeriodo,
    int QuantidadeVendas,
    IReadOnlyList<ItemRelatorioDto> ItensMaisVendidos,
    IReadOnlyList<ProdutoDto> AlertasEstoque,
    IReadOnlyList<ResumoFormaPagamentoDto> PagamentosPorForma,
    IReadOnlyList<ResumoDiaDto> VendasPorDia,
    IReadOnlyList<MovimentacaoDiaDto> MovimentacoesPorDia);

public class PeriodoQuery
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public int TopN { get; set; } = 10;
}
