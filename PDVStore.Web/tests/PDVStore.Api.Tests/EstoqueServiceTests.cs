using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PDVStore.Api.Contracts;
using PDVStore.Api.Services;

namespace PDVStore.Api.Tests;

[TestFixture]
public class EstoqueServiceTests : TesteBanco
{
    private EstoqueService Service => new(Context, UsuarioAtual, NullLogger<EstoqueService>.Instance);

    [Test]
    public async Task Entrada_AumentaEstoqueERegistraMovimentacao()
    {
        var produto = CriarProduto(estoque: 5);

        var mov = await Service.EntradaAsync(new MovimentacaoRequest
        {
            ProdutoId = produto.Id,
            Quantidade = 3,
            Motivo = "Ajuste de inventário"
        }, CancellationToken.None);

        Assert.That(mov.Tipo, Is.EqualTo("Entrada"));
        Assert.That(mov.UsuarioId, Is.EqualTo(1));
        Assert.That((await Context.Produtos.FindAsync(produto.Id))!.Estoque, Is.EqualTo(8));

        var registrada = await Context.MovimentacoesEstoque.SingleAsync();
        Assert.That(registrada.Motivo, Is.EqualTo("Ajuste de inventário"));
        Assert.That(registrada.DataMovimentacao.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public async Task Saida_ReduzEstoqueERegistraSaida()
    {
        var produto = CriarProduto(estoque: 5);

        var mov = await Service.SaidaAsync(new MovimentacaoRequest
        {
            ProdutoId = produto.Id,
            Quantidade = 2,
            Motivo = "Perda"
        }, CancellationToken.None);

        Assert.That(mov.Tipo, Is.EqualTo("Saída"));
        Assert.That((await Context.Produtos.FindAsync(produto.Id))!.Estoque, Is.EqualTo(3));
    }

    [Test]
    public async Task Saida_SemEstoqueSuficiente_NaoAlteraNemGrava()
    {
        var produto = CriarProduto(estoque: 1);

        Assert.ThrowsAsync<ConflitoException>(() => Service.SaidaAsync(new MovimentacaoRequest
        {
            ProdutoId = produto.Id,
            Quantidade = 2
        }, CancellationToken.None));

        Context.ChangeTracker.Clear();
        Assert.That((await Context.Produtos.FindAsync(produto.Id))!.Estoque, Is.EqualTo(1));
        Assert.That(await Context.MovimentacoesEstoque.CountAsync(), Is.Zero);
    }

    [Test]
    public void QuantidadeInvalida_Rejeita()
    {
        Assert.ThrowsAsync<ValidacaoException>(() => Service.EntradaAsync(new MovimentacaoRequest
        {
            ProdutoId = 1,
            Quantidade = 0
        }, CancellationToken.None));
    }

    [Test]
    public void ProdutoInexistente_Retorna404()
    {
        Assert.ThrowsAsync<NaoEncontradoException>(() => Service.EntradaAsync(new MovimentacaoRequest
        {
            ProdutoId = 999,
            Quantidade = 1
        }, CancellationToken.None));
    }

    [Test]
    public async Task Historico_FiltraPorTipoEPeriodo()
    {
        var produto = CriarProduto(estoque: 10);
        await Service.EntradaAsync(new MovimentacaoRequest { ProdutoId = produto.Id, Quantidade = 1 }, CancellationToken.None);
        await Service.SaidaAsync(new MovimentacaoRequest { ProdutoId = produto.Id, Quantidade = 1 }, CancellationToken.None);

        var saidas = await Service.HistoricoAsync(new MovimentacaoQuery { Tipo = "Saída" }, CancellationToken.None);
        Assert.That(saidas.TotalItems, Is.EqualTo(1));
        Assert.That(saidas.Items[0].Tipo, Is.EqualTo("Saída"));

        var futuro = await Service.HistoricoAsync(new MovimentacaoQuery
        {
            Inicio = DateTime.UtcNow.AddDays(1),
            Fim = DateTime.UtcNow.AddDays(2)
        }, CancellationToken.None);
        Assert.That(futuro.TotalItems, Is.Zero);
    }

    [Test]
    public async Task Alertas_ListaSomenteAtivosNoLimite()
    {
        CriarProduto("Baixo", "111", estoque: 2, estoqueMinimo: 2);
        CriarProduto("Ok", "222", estoque: 10, estoqueMinimo: 2);
        var inativo = CriarProduto("Inativo", "333", estoque: 0, estoqueMinimo: 5);
        inativo.Ativo = false;
        await Context.SaveChangesAsync();

        var alertas = await Service.AlertasAsync(CancellationToken.None);

        Assert.That(alertas.Count, Is.EqualTo(1));
        Assert.That(alertas[0].Nome, Is.EqualTo("Baixo"));
    }
}
