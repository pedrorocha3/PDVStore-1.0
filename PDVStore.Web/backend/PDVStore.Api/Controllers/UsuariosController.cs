using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Api.Contracts;
using PDVStore.Api.Security;
using PDVStore.Api.Services;

namespace PDVStore.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = Policies.AdminOnly)]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioService _usuarioService;

    public UsuariosController(UsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UsuarioDto>>> Listar(
        [FromQuery] bool? ativo, [FromQuery] PaginacaoQuery query, CancellationToken cancellationToken)
        => Ok(await _usuarioService.ListarAsync(ativo, query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioDto>> Obter(int id, CancellationToken cancellationToken)
        => Ok(await _usuarioService.ObterPorIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Criar(
        [FromBody] CriarUsuarioRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = usuario.Id }, usuario);
    }

    /// <summary>Atualiza apenas nome, permissão, status e foto (nunca a senha).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioDto>> Atualizar(
        int id, [FromBody] AtualizarUsuarioRequest request, CancellationToken cancellationToken)
        => Ok(await _usuarioService.AtualizarAsync(id, request, cancellationToken));

    /// <summary>Soft delete (Ativo = false).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desativar(int id, CancellationToken cancellationToken)
    {
        await _usuarioService.DesativarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Troca de senha do próprio usuário (exige senha atual).</summary>
    [HttpPost("{id:int}/alterar-senha")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AlterarSenha(
        int id, [FromBody] AlterarSenhaRequest request, CancellationToken cancellationToken)
    {
        await _usuarioService.AlterarSenhaAsync(id, request, cancellationToken);
        return NoContent();
    }
}
