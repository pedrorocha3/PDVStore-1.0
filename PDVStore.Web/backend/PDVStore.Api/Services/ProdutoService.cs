using Microsoft.EntityFrameworkCore;
using PDVStore.Api.Contracts;
using PDVStore.Api.Data;
using PDVStore.Api.Models;

namespace PDVStore.Api.Services;

/// <summary>
/// Regras de cadastro/consulta de produtos. Preserva as validações da tela
/// Windows (código, nome, preço, custo, estoque mínimo, categoria e descrição)
/// e o soft delete (Ativo = false). A edição de cadastro NÃO altera o estoque:
/// saldo só muda por movimentação, venda ou compra.
/// </summary>
public class ProdutoService
{
    private readonly PdvStoreDbContext _context;

    public ProdutoService(PdvStoreDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProdutoDto>> ListarAsync(ProdutoQuery query, CancellationToken cancellationToken)
    {
        var consulta = _context.Produtos.AsNoTracking();

        if (query.Ativo.HasValue)
            consulta = consulta.Where(p => p.Ativo == query.Ativo.Value);

        if (!string.IsNullOrWhiteSpace(query.Busca))
        {
            var termo = query.Busca.Trim().ToLowerInvariant();
            consulta = consulta.Where(p => p.Nome.ToLower().Contains(termo) ||
                                           (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(termo)));
        }

        if (!string.IsNullOrWhiteSpace(query.Categoria))
        {
            var categoria = query.Categoria.Trim();
            consulta = consulta.Where(p => p.Categoria == categoria);
        }

        consulta = (query.OrdenarPor?.ToLowerInvariant()) switch
        {
            "codigo" => query.Descendente ? consulta.OrderByDescending(p => p.CodigoBarras) : consulta.OrderBy(p => p.CodigoBarras),
            "preco" => query.Descendente ? consulta.OrderByDescending(p => p.Preco) : consulta.OrderBy(p => p.Preco),
            "estoque" => query.Descendente ? consulta.OrderByDescending(p => p.Estoque) : consulta.OrderBy(p => p.Estoque),
            _ => query.Descendente ? consulta.OrderByDescending(p => p.Nome) : consulta.OrderBy(p => p.Nome)
        };

        var total = await consulta.CountAsync(cancellationToken);
        var itens = await consulta
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken);

        return PagedResult<ProdutoDto>.Create(itens, query.Page, query.PageSize, total);
    }

    public async Task<ProdutoDto> ObterPorIdAsync(int id, CancellationToken cancellationToken)
    {
        var produto = await _context.Produtos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Produto {id} não encontrado.");

        return produto.ToDto();
    }

    public async Task<ProdutoDto> ObterPorCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken)
    {
        var codigo = codigoBarras.Trim();
        var produto = await _context.Produtos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.CodigoBarras == codigo && p.Ativo, cancellationToken)
            ?? throw new NaoEncontradoException("Produto ativo não encontrado para o código informado.");

        return produto.ToDto();
    }


    public async Task<ProdutoDto> CriarAsync(CriarProdutoRequest request, CancellationToken cancellationToken)
    {
        var produto = new Produto
        {
            CodigoBarras = Normalizar(request.CodigoBarras),
            Nome = request.Nome.Trim(),
            Preco = request.Preco,
            PrecoCusto = request.PrecoCusto,
            Estoque = request.Estoque,
            EstoqueMinimo = request.EstoqueMinimo,
            Categoria = Normalizar(request.Categoria),
            Descricao = Normalizar(request.Descricao),
            Ativo = true
        };

        Validar(produto);

        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync(cancellationToken);
        return produto.ToDto();
    }

    public async Task<ProdutoDto> AtualizarAsync(int id, AtualizarProdutoRequest request, CancellationToken cancellationToken)
    {
        var produto = await _context.Produtos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Produto {id} não encontrado.");

        produto.CodigoBarras = Normalizar(request.CodigoBarras);
        produto.Nome = request.Nome.Trim();
        produto.Preco = request.Preco;
        produto.PrecoCusto = request.PrecoCusto;
        produto.EstoqueMinimo = request.EstoqueMinimo;
        produto.Categoria = Normalizar(request.Categoria);
        produto.Descricao = Normalizar(request.Descricao);

        Validar(produto);

        await _context.SaveChangesAsync(cancellationToken);
        return produto.ToDto();
    }

    /// <summary>Soft delete: o produto deixa de aparecer, preservando histórico.</summary>
    public async Task DesativarAsync(int id, CancellationToken cancellationToken)
    {
        var produto = await _context.Produtos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException($"Produto {id} não encontrado.");

        if (!produto.Ativo)
            return;

        produto.Ativo = false;
        await _context.SaveChangesAsync(cancellationToken);
    }

    internal static void Validar(Produto produto)
    {
        if (string.IsNullOrWhiteSpace(produto.CodigoBarras))
            throw new ValidacaoException("Código de barras é obrigatório.");
        if (string.IsNullOrWhiteSpace(produto.Nome))
            throw new ValidacaoException("Nome do produto é obrigatório.");
        if (produto.Preco < 0)
            throw new ValidacaoException("Informe um preço de venda válido.");
        if (produto.PrecoCusto < 0)
            throw new ValidacaoException("Informe um preço de custo válido.");
        if (produto.Estoque < 0)
            throw new ValidacaoException("Informe um estoque válido.");
        if (produto.EstoqueMinimo < 0)
            throw new ValidacaoException("Informe um estoque mínimo válido.");
        if (string.IsNullOrWhiteSpace(produto.Categoria))
            throw new ValidacaoException("Categoria é obrigatória.");
        if (string.IsNullOrWhiteSpace(produto.Descricao))
            throw new ValidacaoException("Descrição é obrigatória.");
    }

    private static string? Normalizar(string? valor) => valor?.Trim();
}
