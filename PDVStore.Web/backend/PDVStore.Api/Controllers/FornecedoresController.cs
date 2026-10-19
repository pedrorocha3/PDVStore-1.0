using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Api.Contracts;
using PDVStore.Api.Security;
using PDVStore.Api.Services;

namespace PDVStore.Api.Controllers;

[ApiController]
[Route("api/fornecedores")]
[Authorize(Policy = Policies.AdminOnly)]
public class FornecedoresController : ControllerBase
{
    private readonly FornecedorService _fornecedorService;

    public FornecedoresController(FornecedorService fornecedorService)
    {
        _fornecedorService = fornecedorService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FornecedorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FornecedorDto>>> Listar(
        [FromQuery] FornecedorQuery query, CancellationToken cancellationToken)
        => Ok(await _fornecedorService.ListarAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FornecedorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FornecedorDto>> Obter(int id, CancellationToken cancellationToken)
        => Ok(await _fornecedorService.ObterPorIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(FornecedorDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<FornecedorDto>> Criar(
        [FromBody] CriarFornecedorRequest request, CancellationToken cancellationToken)
    {
        var fornecedor = await _fornecedorService.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = fornecedor.Id }, fornecedor);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(FornecedorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FornecedorDto>> Atualizar(
        int id, [FromBody] AtualizarFornecedorRequest request, CancellationToken cancellationToken)
        => Ok(await _fornecedorService.AtualizarAsync(id, request, cancellationToken));

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(FornecedorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FornecedorDto>> Status(
        int id, [FromBody] StatusRequest request, CancellationToken cancellationToken)
        => Ok(await _fornecedorService.AtualizarStatusAsync(id, request.Ativo, cancellationToken));
}
