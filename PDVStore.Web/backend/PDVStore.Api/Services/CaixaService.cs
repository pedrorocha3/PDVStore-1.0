using System.Data;
using Microsoft.EntityFrameworkCore;
using PDVStore.Api.Contracts;
using PDVStore.Api.Data;
using PDVStore.Api.Models;

namespace PDVStore.Api.Services;

/// <summary>
/// Operações de caixa. Preserva as regras do CaixaService original:
/// apenas um caixa aberto por vez, sangria maior que zero apenas com caixa
/// aberto e fechamento = valor inicial + vendas concluídas - sangrias.
/// </summary>
public class CaixaService
{
    private readonly PdvStoreDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CaixaService(PdvStoreDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CaixaDto?> ObterAbertoAsync(CancellationToken cancellationToken)
    {
        var caixa = await _context.Caixas
            .AsNoTracking()
            .Include(c => c.UsuarioCaixa)
            .FirstOrDefaultAsync(c => c.Status == "Aberto", cancellationToken);

        return caixa?.ToDto();
    }

    public async Task<CaixaDto> AbrirAsync(AbrirCaixaRequest request, CancellationToken cancellationToken)
    {
        if (request.ValorInicial < 0)
            throw new ValidacaoException("Valor inicial não pode ser negativo.");

        var usuarioId = _currentUser.Id
            ?? throw new ValidacaoException("Usuário autenticado não identificado.");

        await using var transacao = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var existente = await _context.Caixas
            .FirstOrDefaultAsync(c => c.Status == "Aberto", cancellationToken);

        if (existente != null)
            throw new ConflitoException("Já existe um caixa aberto. Feche o caixa atual antes de abrir outro.");

        var caixa = new Caixa
        {
            UsuarioCaixaId = usuarioId,
            Abertura = DateTime.UtcNow,
            ValorInicial = request.ValorInicial,
            Status = "Aberto"
        };

        _context.Caixas.Add(caixa);
        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        caixa.UsuarioCaixa = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId, cancellationToken);

        return caixa.ToDto();
    }

    public async Task<CaixaDto> RegistrarSangriaAsync(int caixaId, SangriaRequest request, CancellationToken cancellationToken)
    {
        if (request.Valor <= 0)
            throw new ValidacaoException("Valor da sangria deve ser maior que zero.");

        var caixa = await _context.Caixas.FirstOrDefaultAsync(c => c.Id == caixaId, cancellationToken)
            ?? throw new NaoEncontradoException($"Caixa {caixaId} não encontrado.");

        if (caixa.Status != "Aberto")
            throw new ConflitoException("O caixa não está aberto.");

        caixa.Sangria = (caixa.Sangria ?? 0) + request.Valor;
        caixa.ValorFinal ??= caixa.ValorInicial;

        await _context.SaveChangesAsync(cancellationToken);
        return caixa.ToDto();
    }

    public async Task<CaixaDto> FecharAsync(int caixaId, CancellationToken cancellationToken)
    {
        await using var transacao = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var caixa = await _context.Caixas.FirstOrDefaultAsync(c => c.Id == caixaId, cancellationToken)
            ?? throw new NaoEncontradoException($"Caixa {caixaId} não encontrado.");

        if (caixa.Status != "Aberto")
            throw new ConflitoException("O caixa não está aberto.");

        var totalVendas = await _context.Vendas
            .Where(v => v.CaixaId == caixaId && v.Status == "Concluida")
            .SumAsync(v => v.ValorTotal, cancellationToken);

        decimal sangria = caixa.Sangria ?? 0;

        caixa.ValorFinal = caixa.ValorInicial + totalVendas - sangria;
        caixa.Fechamento = DateTime.UtcNow;
        caixa.Status = "Fechado";

        await _context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return caixa.ToDto();
    }

    public async Task<PagedResult<CaixaDto>> ListarAsync(PaginacaoQuery query, CancellationToken cancellationToken)
    {
        var consulta = _context.Caixas
            .AsNoTracking()
            .Include(c => c.UsuarioCaixa);

        var total = await consulta.CountAsync(cancellationToken);
        var itens = await consulta
            .OrderByDescending(c => c.Abertura)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken);

        return PagedResult<CaixaDto>.Create(itens, query.Page, query.PageSize, total);
    }
}
