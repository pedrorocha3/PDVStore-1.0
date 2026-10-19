using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Api.Contracts;
using PDVStore.Api.Security;
using PDVStore.Api.Services;

namespace PDVStore.Api.Controllers;

[ApiController]
[Route("api/caixa")]
[Authorize]
public class CaixaController : ControllerBase
{
    private readonly CaixaService _caixaService;

    public CaixaController(CaixaService caixaService)
    {
        _caixaService = caixaService;
    }

    /// <summary>Caixa aberto atual (204 quando não há). Necessário ao PDV.</summary>
    [HttpGet("aberto")]
    [Authorize(Policy = Policies.Pdv)]
    [ProducesResponseType(typeof(CaixaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<CaixaDto>> Aberto(CancellationToken cancellationToken)
    {
        var caixa = await _caixaService.ObterAbertoAsync(cancellationToken);
        return caixa == null ? NoContent() : Ok(caixa);
    }

    [HttpPost("abrir")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(CaixaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CaixaDto>> Abrir(
        [FromBody] AbrirCaixaRequest request, CancellationToken cancellationToken)
    {
        var caixa = await _caixaService.AbrirAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Aberto), caixa);
    }

    [HttpPost("{id:int}/sangria")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(CaixaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CaixaDto>> Sangria(
        int id, [FromBody] SangriaRequest request, CancellationToken cancellationToken)
        => Ok(await _caixaService.RegistrarSangriaAsync(id, request, cancellationToken));

    /// <summary>Fecha o caixa: inicial + vendas concluídas - sangrias.</summary>
    [HttpPost("{id:int}/fechar")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(CaixaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CaixaDto>> Fechar(int id, CancellationToken cancellationToken)
        => Ok(await _caixaService.FecharAsync(id, cancellationToken));

    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(PagedResult<CaixaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CaixaDto>>> Listar(
        [FromQuery] PaginacaoQuery query, CancellationToken cancellationToken)
        => Ok(await _caixaService.ListarAsync(query, cancellationToken));
}
