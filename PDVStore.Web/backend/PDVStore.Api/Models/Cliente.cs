namespace PDVStore.Api.Models;

/// <summary>
/// Cliente do PDV, incluindo controle de crédito para vendas fiadas.
/// Mesma entidade/tabela da aplicação Windows (Clientes).
/// </summary>
public class Cliente : IHasId
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public DateTime CadastradoEm { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;
    public decimal LimiteCredito { get; set; }
    public decimal SaldoDevedor { get; set; }
}
