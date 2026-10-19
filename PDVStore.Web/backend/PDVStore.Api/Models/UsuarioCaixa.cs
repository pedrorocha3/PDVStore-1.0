namespace PDVStore.Api.Models;

/// <summary>
/// Usuário do caixa. A senha é sempre armazenada como hash BCrypt, exatamente
/// como na aplicação Windows (tabela Usuarios).
/// </summary>
public class UsuarioCaixa : IHasId
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? FotoPath { get; set; }
    public bool Ativo { get; set; } = true;
    public TipoPermissao Permissao { get; set; } = TipoPermissao.Operador;

    public bool Autenticar(string senha) => BCrypt.Net.BCrypt.Verify(senha, SenhaHash);

    public void SetSenha(string senha) => SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha);

    public bool EhAdmin() => Permissao == TipoPermissao.Administrador;

    public static string DescreverPermissao(TipoPermissao permissao) => permissao switch
    {
        TipoPermissao.Operador => "Operador (Caixa)",
        TipoPermissao.Administrador => "Administrador",
        TipoPermissao.Estoquista => "Estoquista",
        _ => permissao.ToString()
    };
}

/// <summary>Valores idênticos aos persistidos pela aplicação Windows.</summary>
public enum TipoPermissao
{
    Operador = 1,
    Administrador = 2,
    Estoquista = 3
}
