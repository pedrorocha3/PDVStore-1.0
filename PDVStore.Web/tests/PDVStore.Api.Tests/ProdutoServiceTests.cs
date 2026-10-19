using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Api.Contracts;
using PDVStore.Api.Services;

namespace PDVStore.Api.Tests;

[TestFixture]
public class ProdutoServiceTests : TesteBanco
{
    private ProdutoService Service => new(Context);

    private static CriarProdutoRequest RequestValido() => new()
    {
        CodigoBarras = "7891000000011",
        Nome = "Café",
        Preco = 12.50m,
        PrecoCusto = 8m,
        Estoque = 10,
        EstoqueMinimo = 2,
        Categoria = "Bebidas",
        Descricao = "Pacote 500g"
    };

    [Test]
    public async Task Criar_Valido_PersisteAtivo()
    {
        var dto = await Service.CriarAsync(RequestValido(), CancellationToken.None);

        Assert.That(dto.Id, Is.GreaterThan(0));
        Assert.That(dto.Ativo, Is.True);
        Assert.That(dto.Nome, Is.EqualTo("Café"));
        Assert.That(await Context.Produtos.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Criar_SemCodigoBarras_Rejeita()
    {
        var request = RequestValido();
        request.CodigoBarras = "  ";

        var ex = Assert.ThrowsAsync<ValidacaoException>(() => Service.CriarAsync(request, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("Código de barras"));
        Assert.That(await Context.Produtos.CountAsync(), Is.Zero);
    }

    [Test]
    public void Criar_SemNome_Rejeita()
    {
        var request = RequestValido();
        request.Nome = "";

        Assert.ThrowsAsync<ValidacaoException>(() => Service.CriarAsync(request, CancellationToken.None));
    }

    [Test]
    public void Criar_PrecoNegativo_Rejeita()
    {
        var request = RequestValido();
        request.Preco = -1m;

        Assert.ThrowsAsync<ValidacaoException>(() => Service.CriarAsync(request, CancellationToken.None));
    }

    [Test]
    public void Criar_SemCategoriaOuDescricao_Rejeita()
    {
        var semCategoria = RequestValido();
        semCategoria.Categoria = "";
        Assert.ThrowsAsync<ValidacaoException>(() => Service.CriarAsync(semCategoria, CancellationToken.None));

        var semDescricao = RequestValido();
        semDescricao.Descricao = "";
        Assert.ThrowsAsync<ValidacaoException>(() => Service.CriarAsync(semDescricao, CancellationToken.None));
    }

    [Test]
    public async Task Atualizar_NaoAlteraEstoque()
    {
        var produto = CriarProduto(estoque: 7);

        var dto = await Service.AtualizarAsync(produto.Id, new AtualizarProdutoRequest
        {
            CodigoBarras = produto.CodigoBarras!,
            Nome = "Café Premium",
            Preco = 15m,
            PrecoCusto = 9m,
            EstoqueMinimo = 3,
            Categoria = "Bebidas",
            Descricao = "Nova descrição"
        }, CancellationToken.None);

        Assert.That(dto.Nome, Is.EqualTo("Café Premium"));
        Assert.That(dto.Preco, Is.EqualTo(15m));
        Assert.That(dto.Estoque, Is.EqualTo(7), "Edição de cadastro nunca deve alterar o estoque.");
        Assert.That(dto.EstoqueMinimo, Is.EqualTo(3));
    }

    [Test]
    public async Task Desativar_FazSoftDeleteEPreservaRegistro()
    {
        var produto = CriarProduto();

        await Service.DesativarAsync(produto.Id, CancellationToken.None);

        var noBanco = await Context.Produtos.SingleAsync();
        Assert.That(noBanco.Ativo, Is.False);
        Assert.That(noBanco.Nome, Is.EqualTo("Café"));
    }

    [Test]
    public async Task Listar_FiltraPorBuscaCategoriaEPagina()
    {
        CriarProduto("Café", "111", estoque: 1);
        CriarProduto("Arroz", "222", estoque: 5);
        var terceiro = CriarProduto("Feijão", "333", estoque: 9);
        terceiro.Categoria = "Mercearia";
        await Context.SaveChangesAsync();

        var pagina = await Service.ListarAsync(new ProdutoQuery { Page = 1, PageSize = 2 }, CancellationToken.None);
        Assert.That(pagina.TotalItems, Is.EqualTo(3));
        Assert.That(pagina.Items.Count, Is.EqualTo(2));

        var busca = await Service.ListarAsync(new ProdutoQuery { Busca = "arr" }, CancellationToken.None);
        Assert.That(busca.TotalItems, Is.EqualTo(1));
        Assert.That(busca.Items[0].Nome, Is.EqualTo("Arroz"));

        var categoria = await Service.ListarAsync(new ProdutoQuery { Categoria = "Mercearia" }, CancellationToken.None);
        Assert.That(categoria.TotalItems, Is.EqualTo(1));
        Assert.That(categoria.Items[0].Nome, Is.EqualTo("Feijão"));
    }

    [Test]
    public async Task ObterPorCodigoBarras_IgnoraInativos()
    {
        var produto = CriarProduto();
        produto.Ativo = false;
        await Context.SaveChangesAsync();

        Assert.ThrowsAsync<NaoEncontradoException>(() =>
            Service.ObterPorCodigoBarrasAsync("7891000000011", CancellationToken.None));
    }
}
