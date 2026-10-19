using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Api.Contracts;
using PDVStore.Api.Security;
using PDVStore.Api.Services;

namespace PDVStore.Api.Controllers;

[ApiController]
[Route("api/compras")]
[Authorize(Policy = Policies.AdminOnly)]
public class ComprasController : ControllerBase
{
    private readonly CompraService _compraService;

    public ComprasController(CompraService compraService)
    {
        _compraService = compraService;
    }

    /// <summary>Registra a compra e dá entrada no estoque (transação única).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CompraDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompraDto>> Registrar(
        [FromBody] CriarCompraRequest request, CancellationToken cancellationToken)
    {
        var compra = await _compraService.RegistrarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = compra.Id }, compra);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CompraDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CompraDto>>> Listar(
        [FromQuery] CompraQuery query, CancellationToken cancellationToken)
        => Ok(await _compraService.ListarAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CompraDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompraDto>> Obter(int id, CancellationToken cancellationToken)
        => Ok(await _compraService.ObterPorIdAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType(typeof(CompraDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompraDto>> Cancelar(int id, CancellationToken cancellationToken)
        => Ok(await _compraService.CancelarAsync(id, cancellationToken));
}
