using Microsoft.EntityFrameworkCore;
using PDVStore.Api.Contracts;
using PDVStore.Api.Data;
using PDVStore.Api.Models;

namespace PDVStore.Api.Services;

/// <summary>
/// Clientes e crédito de fiado. Reproduz o ClienteService original: nome
/// obrigatório, cadastro com data UTC, filtro por nome/CPF, ativação lógica e
/// baixa de fiado que nunca deixa saldo negativo.
/// </summary>
public class ClienteService
{
    private readonly PdvStoreDbContext _context;

    public ClienteService(PdvStoreDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ClienteDto>> ListarAsync(ClienteQuery query, CancellationToken cancellationToken)
    {
        var consulta = _context.Clientes.AsNoTracking();

        if (query.Ativo.HasValue)
            consulta = consulta.Where(c => c.Ativo == query.Ativo.Value);

        if (!string.IsNullOrWhiteSpace(query.Busca))
        {
            var termo = query.Busca.Trim();
            consulta = consulta.Where(c => c.Nome.Contains(termo) ||
                                           (c.CpfCnpj != null && c.CpfCnpj.Contains(termo)));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var itens = await consulta
            .OrderBy(c => c.Nome)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken);

        return PagedResult<ClienteDto>.Create(itens, query.Page, query.PageSize, total);
    }

    public async Task<ClienteDto> ObterPorIdAsync(int id, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {id} não encontrado.");
        return cliente.ToDto();
    }

    public async Task<ClienteDto> CriarAsync(CriarClienteRequest request, CancellationToken cancellationToken)
    {
        var cliente = new Cliente
        {
            Nome = request.Nome.Trim(),
            CpfCnpj = Normalizar(request.CpfCnpj),
            Telefone = Normalizar(request.Telefone),
            Email = Normalizar(request.Email),
            Endereco = Normalizar(request.Endereco),
            LimiteCredito = request.LimiteCredito,
            CadastradoEm = DateTime.UtcNow,
            Ativo = true
        };

        Validar(cliente);

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync(cancellationToken);
        return cliente.ToDto();
    }

    public async Task<ClienteDto> AtualizarAsync(int id, AtualizarClienteRequest request, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {id} não encontrado.");

        cliente.Nome = request.Nome.Trim();
        cliente.CpfCnpj = Normalizar(request.CpfCnpj);
        cliente.Telefone = Normalizar(request.Telefone);
        cliente.Email = Normalizar(request.Email);
        cliente.Endereco = Normalizar(request.Endereco);
        cliente.LimiteCredito = request.LimiteCredito;

        Validar(cliente);
        await _context.SaveChangesAsync(cancellationToken);
        return cliente.ToDto();
    }

    public async Task<ClienteDto> AtualizarStatusAsync(int id, bool ativo, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {id} não encontrado.");

        cliente.Ativo = ativo;
        await _context.SaveChangesAsync(cancellationToken);
        return cliente.ToDto();
    }

    /// <summary>Registra o pagamento de parte do saldo devedor do fiado.</summary>
    public async Task<ClienteDto> ReceberFiadoAsync(int id, decimal valor, CancellationToken cancellationToken)
    {
        if (valor <= 0)
            throw new ValidacaoException("Valor deve ser maior que zero.");

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Cliente {id} não encontrado.");

        cliente.SaldoDevedor = Math.Max(0, cliente.SaldoDevedor - valor);
        await _context.SaveChangesAsync(cancellationToken);
        return cliente.ToDto();
    }

    private static void Validar(Cliente cliente)
    {
        if (string.IsNullOrWhiteSpace(cliente.Nome))
            throw new ValidacaoException("Nome do cliente é obrigatório.");
        if (cliente.LimiteCredito < 0)
            throw new ValidacaoException("Limite de crédito não pode ser negativo.");
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
