using System.ComponentModel.DataAnnotations.Schema;

namespace PDVStore.Api.Models;

public class ItemCompra : IHasId
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoCusto { get; set; }

    /// <summary>Custo total do item (quantidade x custo); não é coluna.</summary>
    [NotMapped]
    public decimal Subtotal => Quantidade * PrecoCusto;

    public Compra? Compra { get; set; }
    public Produto? Produto { get; set; }
}
