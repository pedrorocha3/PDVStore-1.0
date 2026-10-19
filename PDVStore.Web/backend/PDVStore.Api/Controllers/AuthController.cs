using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PDVStore.Api.Contracts;
using PDVStore.Api.Services;

namespace PDVStore.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UsuarioService _usuarioService;
    private readonly ICurrentUser _currentUser;

    public AuthController(UsuarioService usuarioService, ICurrentUser currentUser)
    {
        _usuarioService = usuarioService;
        _currentUser = currentUser;
    }

    /// <summary>Autentica o usuário da tabela Usuarios e devolve um JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var resposta = await _usuarioService.AutenticarAsync(request, cancellationToken);
        if (resposta == null)
            return Unauthorized(new { mensagem = "Usuário ou senha inválidos." });

        return Ok(resposta);
    }

    /// <summary>Usuário autenticado no token atual.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioDto>> Me(CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not int id)
            return Unauthorized();

        return Ok(await _usuarioService.ObterPorIdAsync(id, cancellationToken));
    }
}
