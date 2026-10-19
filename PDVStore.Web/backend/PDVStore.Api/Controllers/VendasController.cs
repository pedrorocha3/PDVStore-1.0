using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Api.Contracts;
using PDVStore.Api.Security;
using PDVStore.Api.Services;

namespace PDVStore.Api.Controllers;

[ApiController]
[Route("api/vendas")]
[Authorize]
public class VendasController : ControllerBase
{
    private readonly VendaService _vendaService;

    public VendasController(VendaService vendaService)
    {
        _vendaService = vendaService;
    }

    /// <summary>
    /// Registra a venda completa (estoque, movimentação, fiado e pagamento).
    /// Exige caixa aberto; o preço unitário vem do cadastro do produto.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.Pdv)]
    [ProducesResponseType(typeof(VendaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VendaDto>> Registrar(
        [FromBody] CriarVendaRequest request, CancellationToken cancellationToken)
    {
        var venda = await _vendaService.RegistrarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = venda.Id }, venda);
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(PagedResult<VendaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<VendaDto>>> Listar(
        [FromQuery] VendaQuery query, CancellationToken cancellationToken)
        => Ok(await _vendaService.ListarAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(VendaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VendaDto>> Obter(int id, CancellationToken cancellationToken)
        => Ok(await _vendaService.ObterPorIdAsync(id, cancellationToken));

    /// <summary>Cancela a venda, devolvendo estoque e saldo devedor (fiado).</summary>
    [HttpPost("{id:int}/cancelar")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(VendaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VendaDto>> Cancelar(
        int id, [FromBody] CancelarVendaRequest? request, CancellationToken cancellationToken)
        => Ok(await _vendaService.CancelarAsync(id, request?.Motivo, cancellationToken));

    /// <summary>Quantidade, faturamento e distribuição por forma de pagamento.</summary>
    [HttpGet("resumo")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(ResumoVendasDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResumoVendasDto>> Resumo(
        [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken cancellationToken)
        => Ok(await _vendaService.ResumoAsync(inicio, fim, cancellationToken));
}
