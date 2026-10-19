using System.ComponentModel.DataAnnotations;

namespace PDVStore.Api.Contracts;

public record ClienteDto(
    int Id,
    string Nome,
    string? CpfCnpj,
    string? Telefone,
    string? Email,
    string? Endereco,
    DateTime CadastradoEm,
    bool Ativo,
    decimal LimiteCredito,
    decimal SaldoDevedor);

public class ClienteQuery : PaginacaoQuery
{
    public string? Busca { get; set; }
    public bool? Ativo { get; set; } = true;
}

public class CriarClienteRequest
{
    [Required(ErrorMessage = "Nome do cliente é obrigatório.")]
    [StringLength(200)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(20)] public string? CpfCnpj { get; set; }
    [StringLength(30)] public string? Telefone { get; set; }
    [EmailAddress(ErrorMessage = "E-mail inválido."), StringLength(200)] public string? Email { get; set; }
    [StringLength(300)] public string? Endereco { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Limite de crédito não pode ser negativo.")]
    public decimal LimiteCredito { get; set; }
}

public class AtualizarClienteRequest
{
    [Required(ErrorMessage = "Nome do cliente é obrigatório.")]
    [StringLength(200)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(20)] public string? CpfCnpj { get; set; }
    [StringLength(30)] public string? Telefone { get; set; }
    [EmailAddress(ErrorMessage = "E-mail inválido."), StringLength(200)] public string? Email { get; set; }
    [StringLength(300)] public string? Endereco { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Limite de crédito não pode ser negativo.")]
    public decimal LimiteCredito { get; set; }
}

public class StatusRequest
{
    public bool Ativo { get; set; }
}

public class ReceberFiadoRequest
{
    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Valor deve ser maior que zero.")]
    public decimal Valor { get; set; }
}
