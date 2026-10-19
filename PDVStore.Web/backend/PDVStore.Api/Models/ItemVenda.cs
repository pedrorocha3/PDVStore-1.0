using System.ComponentModel.DataAnnotations.Schema;

namespace PDVStore.Api.Models;

public class ItemVenda : IHasId
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }

    /// <summary>Total do item (quantidade x preço unitário); não é coluna.</summary>
    [NotMapped]
    public decimal Subtotal => Quantidade * PrecoUnitario;

    [NotMapped]
    public string? NomeProduto => Produto?.Nome;

    public Produto? Produto { get; set; }
    public Venda? Venda { get; set; }
}
