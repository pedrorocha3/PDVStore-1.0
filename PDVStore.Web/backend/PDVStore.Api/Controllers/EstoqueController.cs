using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Api.Contracts;
using PDVStore.Api.Security;
using PDVStore.Api.Services;

namespace PDVStore.Api.Controllers;

[ApiController]
[Route("api/estoque")]
[Authorize(Policy = Policies.AdminOnly)]
public class EstoqueController : ControllerBase
{
    private readonly EstoqueService _estoqueService;

    public EstoqueController(EstoqueService estoqueService)
    {
        _estoqueService = estoqueService;
    }

    /// <summary>Ajuste manual de entrada (aumenta o saldo e registra movimentação).</summary>
    [HttpPost("entradas")]
    [ProducesResponseType(typeof(MovimentacaoEstoqueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimentacaoEstoqueDto>> Entrada(
        [FromBody] MovimentacaoRequest request, CancellationToken cancellationToken)
    {
        var movimentacao = await _estoqueService.EntradaAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Historico), new { produtoId = movimentacao.ProdutoId }, movimentacao);
    }

    /// <summary>Ajuste manual de saída; nunca permite saldo negativo.</summary>
    [HttpPost("saidas")]
    [ProducesResponseType(typeof(MovimentacaoEstoqueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MovimentacaoEstoqueDto>> Saida(
        [FromBody] MovimentacaoRequest request, CancellationToken cancellationToken)
    {
        var movimentacao = await _estoqueService.SaidaAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Historico), new { produtoId = movimentacao.ProdutoId }, movimentacao);
    }

    [HttpGet("movimentacoes")]
    [ProducesResponseType(typeof(PagedResult<MovimentacaoEstoqueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MovimentacaoEstoqueDto>>> Historico(
        [FromQuery] MovimentacaoQuery query, CancellationToken cancellationToken)
        => Ok(await _estoqueService.HistoricoAsync(query, cancellationToken));

    /// <summary>Produtos ativos com saldo menor ou igual ao estoque mínimo.</summary>
    [HttpGet("alertas")]
    [ProducesResponseType(typeof(List<ProdutoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProdutoDto>>> Alertas(CancellationToken cancellationToken)
        => Ok(await _estoqueService.AlertasAsync(cancellationToken));
}
