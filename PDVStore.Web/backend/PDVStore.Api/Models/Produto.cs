using System.ComponentModel.DataAnnotations.Schema;

namespace PDVStore.Api.Models;

/// <summary>
/// Produto do catálogo. Mesma entidade/tabela da aplicação Windows (Produtos).
/// </summary>
public class Produto : IHasId
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public decimal Preco { get; set; }
    public decimal PrecoCusto { get; set; }
    public int Estoque { get; set; }
    public int EstoqueMinimo { get; set; }
    public string? Categoria { get; set; }
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>Espelho de <see cref="Estoque"/> usado apenas para exibição; não é coluna.</summary>
    [NotMapped]
    public int EstoqueAtual
    {
        get => Estoque;
        set => Estoque = value;
    }
}
