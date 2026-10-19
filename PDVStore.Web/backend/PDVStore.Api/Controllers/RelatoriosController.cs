using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Api.Contracts;
using PDVStore.Api.Security;
using PDVStore.Api.Services;

namespace PDVStore.Api.Controllers;

[ApiController]
[Route("api/relatorios")]
[Authorize(Policy = Policies.AdminOnly)]
public class RelatoriosController : ControllerBase
{
    private readonly RelatorioService _relatorioService;

    public RelatoriosController(RelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    [HttpGet("itens-mais-vendidos")]
    [ProducesResponseType(typeof(List<ItemRelatorioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ItemRelatorioDto>>> ItensMaisVendidos(
        [FromQuery] PeriodoQuery query, CancellationToken cancellationToken)
        => Ok(await _relatorioService.ItensMaisVendidosAsync(
            query.Inicio, query.Fim, query.TopN, cancellationToken));

    [HttpGet("estoque-minimo")]
    [ProducesResponseType(typeof(List<ProdutoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProdutoDto>>> EstoqueMinimo(CancellationToken cancellationToken)
        => Ok(await _relatorioService.EstoqueMinimoAsync(cancellationToken));

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Dashboard(
        [FromQuery] PeriodoQuery query, CancellationToken cancellationToken)
        => Ok(await _relatorioService.DashboardAsync(query.Inicio, query.Fim, cancellationToken));
}
