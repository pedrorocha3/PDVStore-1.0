namespace PDVStore.Api.Security;

/// <summary>Configuração JWT lida de variáveis de ambiente/User Secrets.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PDVStore.Api";
    public string Audience { get; set; } = "PDVStore.Web";
    public int ExpirationMinutes { get; set; } = 480;
}

/// <summary>Nomes de políticas de autorização alinhados aos papéis do PDVStore.</summary>
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string Pdv = "Pdv";
    public const string Catalogo = "Catalogo";
}

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Operador = "Operador";
    public const string Estoquista = "Estoquista";
}
