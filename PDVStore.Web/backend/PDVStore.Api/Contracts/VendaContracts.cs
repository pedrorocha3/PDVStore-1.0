using System.ComponentModel.DataAnnotations;

namespace PDVStore.Api.Contracts;

public record ItemVendaDto(
    int Id,
    int ProdutoId,
    string? ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);

public record VendaDto(
    int Id,
    int UsuarioCaixaId,
    string? UsuarioNome,
    DateTime DataVenda,
    decimal ValorTotal,
    decimal Desconto,
    string FormaPagamento,
    string? PixTxId,
    string? Status,
    int CaixaId,
    int? ClienteId,
    string? ClienteNome,
    IReadOnlyList<ItemVendaDto> Itens);

public class CriarVendaRequest
{
    [Required(ErrorMessage = "A venda deve conter pelo menos um item.")]
    [MinLength(1, ErrorMessage = "A venda deve conter pelo menos um item.")]
    public List<ItemVendaRequest> Itens { get; set; } = new();

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Desconto não pode ser negativo.")]
    public decimal Desconto { get; set; }

    [Required(ErrorMessage = "Selecione uma forma de pagamento.")]
    [StringLength(50)]
    public string FormaPagamento { get; set; } = string.Empty;

    public int? ClienteId { get; set; }
}

public class ItemVendaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Produto inválido.")]
    public int ProdutoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero.")]
    public int Quantidade { get; set; }
}

public class VendaQuery : PaginacaoQuery
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public int? CaixaId { get; set; }
    /// <summary>Concluida | Cancelada | Pendente</summary>
    public string? Status { get; set; }
}

public class CancelarVendaRequest
{
    [StringLength(300)]
    public string? Motivo { get; set; }
}

public record ResumoVendasDto(DateTime Inicio, DateTime Fim, int Quantidade, decimal Total, IReadOnlyList<ResumoFormaPagamentoDto> PorFormaPagamento);

public record ResumoFormaPagamentoDto(string FormaPagamento, int Quantidade, decimal Total);
