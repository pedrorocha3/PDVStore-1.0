using System.ComponentModel.DataAnnotations;

namespace PDVStore.Api.Contracts;

public record MovimentacaoEstoqueDto(
    int Id,
    int ProdutoId,
    string? ProdutoNome,
    string Tipo,
    int Quantidade,
    decimal? PrecoUnitario,
    DateTime DataMovimentacao,
    int? UsuarioId,
    string? UsuarioNome,
    string? Motivo,
    int? ReferenciaVendaId);

public class MovimentacaoQuery : PaginacaoQuery
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public int? ProdutoId { get; set; }
    /// <summary>Entrada | Saída</summary>
    public string? Tipo { get; set; }
}

public class MovimentacaoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Produto inválido.")]
    public int ProdutoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero.")]
    public int Quantidade { get; set; }

    [StringLength(300, ErrorMessage = "Motivo deve ter no máximo 300 caracteres.")]
    public string? Motivo { get; set; }
}
