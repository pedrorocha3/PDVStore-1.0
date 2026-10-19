using System.ComponentModel.DataAnnotations;

namespace PDVStore.Api.Contracts;

public record FornecedorDto(
    int Id,
    string Nome,
    string? Cnpj,
    string? Telefone,
    string? Email,
    bool Ativo);

public class FornecedorQuery : PaginacaoQuery
{
    public string? Busca { get; set; }
    public bool? Ativo { get; set; } = true;
}

public class CriarFornecedorRequest
{
    [Required(ErrorMessage = "Nome do fornecedor é obrigatório.")]
    [StringLength(200)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(20)] public string? Cnpj { get; set; }
    [StringLength(30)] public string? Telefone { get; set; }
    [EmailAddress(ErrorMessage = "E-mail inválido."), StringLength(200)] public string? Email { get; set; }
}

public class AtualizarFornecedorRequest : CriarFornecedorRequest
{
}
