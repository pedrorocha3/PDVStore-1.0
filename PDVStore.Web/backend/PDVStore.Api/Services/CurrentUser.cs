using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PDVStore.Api.Models;

namespace PDVStore.Api.Services;

/// <summary>
/// Usuário autenticado na requisição. Substitui o <c>Session.CurrentUser</c>
/// estático da aplicação Windows, que não é seguro em um servidor web
/// (estado global compartilhado entre requisições).
/// </summary>
public interface ICurrentUser
{
    int? Id { get; }
    string? Nome { get; }
    TipoPermissao? Permissao { get; }
    bool Autenticado { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool Autenticado => Principal?.Identity?.IsAuthenticated == true;

    public int? Id
    {
        get
        {
            var valor = Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(valor, out var id) ? id : null;
        }
    }

    public string? Nome => Principal?.FindFirstValue("name") ?? Principal?.Identity?.Name;

    public TipoPermissao? Permissao
    {
        get
        {
            var valor = Principal?.FindFirstValue("role") ?? Principal?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<TipoPermissao>(valor, out var permissao) ? permissao : null;
        }
    }
}
