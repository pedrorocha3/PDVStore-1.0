using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Api.Contracts;
using PDVStore.Api.Security;
using PDVStore.Api.Services;

namespace PDVStore.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Policy = Policies.AdminOnly)]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _clienteService;

    public ClientesController(ClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClienteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClienteDto>>> Listar(
        [FromQuery] ClienteQuery query, CancellationToken cancellationToken)
        => Ok(await _clienteService.ListarAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> Obter(int id, CancellationToken cancellationToken)
        => Ok(await _clienteService.ObterPorIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ClienteDto>> Criar(
        [FromBody] CriarClienteRequest request, CancellationToken cancellationToken)
    {
        var cliente = await _clienteService.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> Atualizar(
        int id, [FromBody] AtualizarClienteRequest request, CancellationToken cancellationToken)
        => Ok(await _clienteService.AtualizarAsync(id, request, cancellationToken));

    /// <summary>Ativa/desativa o cliente (soft delete), preservando histórico.</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClienteDto>> Status(
        int id, [FromBody] StatusRequest request, CancellationToken cancellationToken)
        => Ok(await _clienteService.AtualizarStatusAsync(id, request.Ativo, cancellationToken));

    /// <summary>Baixa de fiado: reduz o saldo devedor sem ficar negativo.</summary>
    [HttpPost("{id:int}/receber-fiado")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClienteDto>> ReceberFiado(
        int id, [FromBody] ReceberFiadoRequest request, CancellationToken cancellationToken)
        => Ok(await _clienteService.ReceberFiadoAsync(id, request.Valor, cancellationToken));
}
