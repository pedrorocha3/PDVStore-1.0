using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Api.Contracts;
using PDVStore.Api.Security;
using PDVStore.Api.Services;

namespace PDVStore.Api.Controllers;

[ApiController]
[Route("api/produtos")]
[Authorize]
public class ProdutosController : ControllerBase
{
    private readonly ProdutoService _produtoService;

    public ProdutosController(ProdutoService produtoService)
    {
        _produtoService = produtoService;
    }

    /// <summary>Lista paginada com filtros de busca, categoria, status e ordenação.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProdutoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProdutoDto>>> Listar(
        [FromQuery] ProdutoQuery query, CancellationToken cancellationToken)
        => Ok(await _produtoService.ListarAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProdutoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProdutoDto>> Obter(int id, CancellationToken cancellationToken)
        => Ok(await _produtoService.ObterPorIdAsync(id, cancellationToken));

    /// <summary>Busca por código de barras (somente produtos ativos), usada pelo PDV.</summary>
    [HttpGet("codigo-barras/{codigo}")]
    [ProducesResponseType(typeof(ProdutoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProdutoDto>> ObterPorCodigo(string codigo, CancellationToken cancellationToken)
        => Ok(await _produtoService.ObterPorCodigoBarrasAsync(codigo, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.Catalogo)]
    [ProducesResponseType(typeof(ProdutoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProdutoDto>> Criar(
        [FromBody] CriarProdutoRequest request, CancellationToken cancellationToken)
    {
        var produto = await _produtoService.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = produto.Id }, produto);
    }

    /// <summary>
    /// Atualiza o cadastro. O estoque NÃO é alterado por este endpoint; use as
    /// movimentações de estoque venda/compra.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Catalogo)]
    [ProducesResponseType(typeof(ProdutoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProdutoDto>> Atualizar(
        int id, [FromBody] AtualizarProdutoRequest request, CancellationToken cancellationToken)
        => Ok(await _produtoService.AtualizarAsync(id, request, cancellationToken));

    /// <summary>Exclusão lógica (Ativo = false), preservando histórico.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Catalogo)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desativar(int id, CancellationToken cancellationToken)
    {
        await _produtoService.DesativarAsync(id, cancellationToken);
        return NoContent();
    }
}
