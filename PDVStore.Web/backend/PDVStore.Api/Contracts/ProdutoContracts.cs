using System.ComponentModel.DataAnnotations;

namespace PDVStore.Api.Contracts;

public record ProdutoDto(
    int Id,
    string Nome,
    string? CodigoBarras,
    decimal Preco,
    decimal PrecoCusto,
    int Estoque,
    int EstoqueMinimo,
    string? Categoria,
    string? Descricao,
    bool Ativo);

public class ProdutoQuery : PaginacaoQuery
{
    /// <summary>Filtro por nome ou código de barras (Contains).</summary>
    public string? Busca { get; set; }
    public string? Categoria { get; set; }
    public bool? Ativo { get; set; } = true;
    /// <summary>nome | codigo | preco | estoque</summary>
    public string? OrdenarPor { get; set; } = "nome";
    public bool Descendente { get; set; }
}

public class CriarProdutoRequest
{
    [Required(ErrorMessage = "Código de barras é obrigatório.")]
    [StringLength(60, ErrorMessage = "Código de barras deve ter no máximo 60 caracteres.")]
    public string CodigoBarras { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome do produto é obrigatório.")]
    [StringLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Informe um preço de venda válido.")]
    public decimal Preco { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Informe um preço de custo válido.")]
    public decimal PrecoCusto { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Informe um estoque válido.")]
    public int Estoque { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Informe um estoque mínimo válido.")]
    public int EstoqueMinimo { get; set; }

    [Required(ErrorMessage = "Categoria é obrigatória.")]
    [StringLength(120, ErrorMessage = "Categoria deve ter no máximo 120 caracteres.")]
    public string Categoria { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descrição é obrigatória.")]
    [StringLength(1000, ErrorMessage = "Descrição deve ter no máximo 1000 caracteres.")]
    public string Descricao { get; set; } = string.Empty;
}

/// <summary>
/// Edição de cadastro. O estoque NÃO faz parte deste contrato: alterações de
/// saldo só podem ocorrer por movimentações, vendas ou compras.
/// </summary>
public class AtualizarProdutoRequest
{
    [Required(ErrorMessage = "Código de barras é obrigatório.")]
    [StringLength(60)]
    public string CodigoBarras { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome do produto é obrigatório.")]
    [StringLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Informe um preço de venda válido.")]
    public decimal Preco { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Informe um preço de custo válido.")]
    public decimal PrecoCusto { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Informe um estoque mínimo válido.")]
    public int EstoqueMinimo { get; set; }

    [Required(ErrorMessage = "Categoria é obrigatória.")]
    [StringLength(120)]
    public string Categoria { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descrição é obrigatória.")]
    [StringLength(1000)]
    public string Descricao { get; set; } = string.Empty;
}
