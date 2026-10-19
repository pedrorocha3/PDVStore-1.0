using System.ComponentModel.DataAnnotations;

namespace PDVStore.Api.Contracts;

public record ItemCompraDto(
    int Id,
    int ProdutoId,
    string? ProdutoNome,
    int Quantidade,
    decimal PrecoCusto,
    decimal Subtotal);

public record CompraDto(
    int Id,
    int FornecedorId,
    string? FornecedorNome,
    int UsuarioCaixaId,
    string? UsuarioNome,
    DateTime DataCompra,
    string? NumeroNota,
    decimal ValorTotal,
    string Status,
    IReadOnlyList<ItemCompraDto> Itens);

public class CriarCompraRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Fornecedor inválido.")]
    public int FornecedorId { get; set; }

    [StringLength(60)]
    public string? NumeroNota { get; set; }

    [Required(ErrorMessage = "A compra deve conter pelo menos um item.")]
    [MinLength(1, ErrorMessage = "A compra deve conter pelo menos um item.")]
    public List<ItemCompraRequest> Itens { get; set; } = new();
}

public class ItemCompraRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Produto inválido.")]
    public int ProdutoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantidade dos itens deve ser maior que zero.")]
    public int Quantidade { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Preço de custo não pode ser negativo.")]
    public decimal PrecoCusto { get; set; }
}

public class CompraQuery : PaginacaoQuery
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public string? Status { get; set; }
}
