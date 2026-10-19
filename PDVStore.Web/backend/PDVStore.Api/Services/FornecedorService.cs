using Microsoft.EntityFrameworkCore;
using PDVStore.Api.Contracts;
using PDVStore.Api.Data;
using PDVStore.Api.Models;

namespace PDVStore.Api.Services;

/// <summary>
/// Fornecedores: nome obrigatório, consulta por nome/CNPJ, upsert de cadastro
/// e desativação lógica (preserva compras históricas), como no original.
/// </summary>
public class FornecedorService
{
    private readonly PdvStoreDbContext _context;

    public FornecedorService(PdvStoreDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<FornecedorDto>> ListarAsync(FornecedorQuery query, CancellationToken cancellationToken)
    {
        var consulta = _context.Fornecedores.AsNoTracking();

        if (query.Ativo.HasValue)
            consulta = consulta.Where(f => f.Ativo == query.Ativo.Value);

        if (!string.IsNullOrWhiteSpace(query.Busca))
        {
            var termo = query.Busca.Trim().ToLowerInvariant();
            consulta = consulta.Where(f => f.Nome.ToLower().Contains(termo) ||
                                           (f.Cnpj != null && f.Cnpj.ToLower().Contains(termo)));
        }

        var total = await consulta.CountAsync(cancellationToken);
        var itens = await consulta
            .OrderBy(f => f.Nome)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(f => f.ToDto())
            .ToListAsync(cancellationToken);

        return PagedResult<FornecedorDto>.Create(itens, query.Page, query.PageSize, total);
    }

    public async Task<FornecedorDto> ObterPorIdAsync(int id, CancellationToken cancellationToken)
    {
        var fornecedor = await _context.Fornecedores.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Fornecedor {id} não encontrado.");
        return fornecedor.ToDto();
    }

    public async Task<FornecedorDto> CriarAsync(CriarFornecedorRequest request, CancellationToken cancellationToken)
    {
        var fornecedor = new Fornecedor
        {
            Nome = request.Nome.Trim(),
            Cnpj = Normalizar(request.Cnpj),
            Telefone = Normalizar(request.Telefone),
            Email = Normalizar(request.Email),
            Ativo = true
        };

        Validar(fornecedor);

        _context.Fornecedores.Add(fornecedor);
        await _context.SaveChangesAsync(cancellationToken);
        return fornecedor.ToDto();
    }

    public async Task<FornecedorDto> AtualizarAsync(int id, AtualizarFornecedorRequest request, CancellationToken cancellationToken)
    {
        var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Fornecedor {id} não encontrado.");

        fornecedor.Nome = request.Nome.Trim();
        fornecedor.Cnpj = Normalizar(request.Cnpj);
        fornecedor.Telefone = Normalizar(request.Telefone);
        fornecedor.Email = Normalizar(request.Email);

        Validar(fornecedor);
        await _context.SaveChangesAsync(cancellationToken);
        return fornecedor.ToDto();
    }

    public async Task<FornecedorDto> AtualizarStatusAsync(int id, bool ativo, CancellationToken cancellationToken)
    {
        var fornecedor = await _context.Fornecedores.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Fornecedor {id} não encontrado.");

        fornecedor.Ativo = ativo;
        await _context.SaveChangesAsync(cancellationToken);
        return fornecedor.ToDto();
    }

    private static void Validar(Fornecedor fornecedor)
    {
        if (string.IsNullOrWhiteSpace(fornecedor.Nome))
            throw new ValidacaoException("Nome do fornecedor é obrigatório.");
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
