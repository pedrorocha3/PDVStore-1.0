using System.ComponentModel.DataAnnotations;

namespace PDVStore.Api.Contracts;

public record CaixaDto(
    int Id,
    int UsuarioCaixaId,
    string? UsuarioNome,
    DateTime Abertura,
    DateTime? Fechamento,
    decimal ValorInicial,
    decimal? ValorFinal,
    decimal? Sangria,
    string Status);

public class AbrirCaixaRequest
{
    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Valor inicial não pode ser negativo.")]
    public decimal ValorInicial { get; set; }
}

public class SangriaRequest
{
    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Valor da sangria deve ser maior que zero.")]
    public decimal Valor { get; set; }
}
