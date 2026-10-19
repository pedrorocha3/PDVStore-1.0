namespace PDVStore.Api.Models;

public class Venda : IHasId
{
    public int Id { get; set; }
    public int UsuarioCaixaId { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.UtcNow;
    public decimal ValorTotal { get; set; }
    public decimal Desconto { get; set; }
    public string FormaPagamento { get; set; } = "Dinheiro";
    public string? PixTxId { get; set; }
    public string? Status { get; set; } = "Concluida"; // Concluida, Cancelada, Pendente
    public int CaixaId { get; set; } = 1;
    public int? ClienteId { get; set; }

    public UsuarioCaixa? UsuarioCaixa { get; set; }
    public Caixa? Caixa { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();
}
