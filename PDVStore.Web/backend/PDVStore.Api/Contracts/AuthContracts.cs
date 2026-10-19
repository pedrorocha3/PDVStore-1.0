using System.ComponentModel.DataAnnotations;
using PDVStore.Api.Models;

namespace PDVStore.Api.Contracts;

public class LoginRequest
{
    [Required(ErrorMessage = "Informe o usuário.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(100)]
    public string Senha { get; set; } = string.Empty;
}

public record LoginResponse(string Token, DateTime ExpiraEm, UsuarioDto Usuario);

public record UsuarioDto(
    int Id,
    string Nome,
    TipoPermissao Permissao,
    string PermissaoDescricao,
    bool Ativo,
    DateTime CreatedAt,
    string? FotoPath);

public class CriarUsuarioRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "Senha deve ter pelo menos 6 caracteres.")]
    [StringLength(100)]
    public string Senha { get; set; } = string.Empty;

    [EnumDataType(typeof(TipoPermissao), ErrorMessage = "Permissão inválida.")]
    public TipoPermissao Permissao { get; set; } = TipoPermissao.Operador;
}

/// <summary>Edição permitida de usuário: nunca altera a senha.</summary>
public class AtualizarUsuarioRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [EnumDataType(typeof(TipoPermissao), ErrorMessage = "Permissão inválida.")]
    public TipoPermissao Permissao { get; set; }

    public bool Ativo { get; set; } = true;

    [StringLength(500)]
    public string? FotoPath { get; set; }
}

public class AlterarSenhaRequest
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [MinLength(6, ErrorMessage = "Nova senha deve ter pelo menos 6 caracteres.")]
    [StringLength(100)]
    public string NovaSenha { get; set; } = string.Empty;
}
