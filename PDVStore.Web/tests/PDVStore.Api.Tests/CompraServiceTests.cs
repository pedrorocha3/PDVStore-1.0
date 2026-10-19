using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PDVStore.Api.Contracts;
using PDVStore.Api.Models;
using PDVStore.Api.Services;

namespace PDVStore.Api.Tests;

[TestFixture]
public class CompraServiceTests : TesteBanco
{
    private CompraService Service => new(Context, UsuarioAtual, NullLogger<CompraService>.Instance);

    private Fornecedor CriarFornecedor(bool ativo = true)
    {
        var fornecedor = new Fornecedor { Nome = "Distribuidora", Ativo = ativo };
        Context.Fornecedores.Add(fornecedor);
        Context.SaveChanges();
        return fornecedor;
    }

    private static CriarCompraRequest Request(int fornecedorId, int produtoId,
        int quantidade = 2, decimal custo = 7m) => new()
        {
            FornecedorId = fornecedorId,
            NumeroNota = "NF-1",
            Itens = new List<ItemCompraRequest>
            {
                new() { ProdutoId = produtoId, Quantidade = quantidade, PrecoCusto = custo }
            }
        };

    [Test]
    public async Task Registrar_AtualizaEstoqueCustoEMovimentacao()
    {
        var produto = CriarProduto(preco: 10m, estoque: 3);
        var fornecedor = CriarFornecedor();

        var compra = await Service.RegistrarAsync(Request(fornecedor.Id, produto.Id), CancellationToken.None);

        Assert.That(compra.ValorTotal, Is.EqualTo(14m));
        Assert.That(compra.Status, Is.EqualTo("Concluida"));
        Assert.That(compra.NumeroNota, Is.EqualTo("NF-1"));

        Context.ChangeTracker.Clear();
        var atualizado = await Context.Produtos.FindAsync(produto.Id);
        Assert.That(atualizado!.Estoque, Is.EqualTo(5));
        Assert.That(atualizado.PrecoCusto, Is.EqualTo(7m));

        var mov = await Context.MovimentacoesEstoque.SingleAsync();
        Assert.That(mov.Tipo, Is.EqualTo("Entrada"));
        Assert.That(mov.Motivo, Is.EqualTo($"Compra #{compra.Id} - NF-1"));
        Assert.That(mov.UsuarioId, Is.EqualTo(1));
    }

    [Test]
    public void Registrar_SemItens_Rejeita()
    {
        var fornecedor = CriarFornecedor();
        var request = Request(fornecedor.Id, 1);
        request.Itens.Clear();

        Assert.ThrowsAsync<ValidacaoException>(() => Service.RegistrarAsync(request, CancellationToken.None));
    }

    [Test]
    public void Registrar_QuantidadeOuCustoInvalido_Rejeita()
    {
        var fornecedor = CriarFornecedor();
        var produto = CriarProduto();

        Assert.ThrowsAsync<ValidacaoException>(() =>
            Service.RegistrarAsync(Request(fornecedor.Id, produto.Id, quantidade: 0), CancellationToken.None));

        Assert.ThrowsAsync<ValidacaoException>(() =>
            Service.RegistrarAsync(Request(fornecedor.Id, produto.Id, custo: -1m), CancellationToken.None));
    }

    [Test]
    public void Registrar_ProdutoInexistente_Rejeita()
    {
        var fornecedor = CriarFornecedor();

        Assert.ThrowsAsync<ValidacaoException>(() =>
            Service.RegistrarAsync(Request(fornecedor.Id, 999), CancellationToken.None));
    }

    [Test]
    public void Registrar_FornecedorInativo_RetornaConflito()
    {
        var fornecedor = CriarFornecedor(ativo: false);
        var produto = CriarProduto();

        Assert.ThrowsAsync<ConflitoException>(() =>
            Service.RegistrarAsync(Request(fornecedor.Id, produto.Id), CancellationToken.None));
    }

    [Test]
    public async Task Cancelar_EstornaEstoqueSemNegativar()
    {
        var produto = CriarProduto(estoque: 1);
        var fornecedor = CriarFornecedor();
        var compra = await Service.RegistrarAsync(
            Request(fornecedor.Id, produto.Id, quantidade: 2), CancellationToken.None);

        // Consome o estoque para testar a proteção Math.Max(0,...).
        var rastreado = await Context.Produtos.FindAsync(produto.Id);
        rastreado!.Estoque = 0;
        await Context.SaveChangesAsync();

        var cancelada = await Service.CancelarAsync(compra.Id, CancellationToken.None);

        Assert.That(cancelada.Status, Is.EqualTo("Cancelada"));
        Context.ChangeTracker.Clear();
        Assert.That((await Context.Produtos.FindAsync(produto.Id))!.Estoque, Is.Zero);
    }

    [Test]
    public async Task Cancelar_DuasVezes_RetornaConflito()
    {
        var produto = CriarProduto();
        var fornecedor = CriarFornecedor();
        var compra = await Service.RegistrarAsync(Request(fornecedor.Id, produto.Id), CancellationToken.None);
        await Service.CancelarAsync(compra.Id, CancellationToken.None);

        Assert.ThrowsAsync<ConflitoException>(() => Service.CancelarAsync(compra.Id, CancellationToken.None));
    }
}
