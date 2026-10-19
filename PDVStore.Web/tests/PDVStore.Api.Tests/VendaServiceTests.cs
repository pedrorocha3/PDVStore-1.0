using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Api.Contracts;
using PDVStore.Api.Services;

namespace PDVStore.Api.Tests;

[TestFixture]
public class VendaServiceTests : TesteBanco
{
    private static CriarVendaRequest Request(int produtoId, int quantidade = 1,
        string forma = "Dinheiro", decimal desconto = 0, int? clienteId = null) => new()
        {
            Itens = new List<ItemVendaRequest> { new() { ProdutoId = produtoId, Quantidade = quantidade } },
            FormaPagamento = forma,
            Desconto = desconto,
            ClienteId = clienteId
        };

    [Test]
    public void SemItens_Rejeita()
    {
        var request = Request(1);
        request.Itens.Clear();

        Assert.ThrowsAsync<ValidacaoException>(() => CriarVendaService().RegistrarAsync(request, CancellationToken.None));
    }

    [Test]
    public void SemFormaPagamento_Rejeita()
    {
        var produto = CriarProduto();
        CriarCaixaAberto();
        var request = Request(produto.Id, forma: " ");

        Assert.ThrowsAsync<ValidacaoException>(() => CriarVendaService().RegistrarAsync(request, CancellationToken.None));
    }

    [Test]
    public void SemCaixaAberto_Rejeita()
    {
        var produto = CriarProduto();

        var ex = Assert.ThrowsAsync<ConflitoException>(() =>
            CriarVendaService().RegistrarAsync(Request(produto.Id), CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("caixa aberto"));
    }

    [Test]
    public void FiadoSemCliente_Rejeita()
    {
        var produto = CriarProduto();
        CriarCaixaAberto();

        Assert.ThrowsAsync<ValidacaoException>(() => CriarVendaService()
            .RegistrarAsync(Request(produto.Id, forma: "Fiado"), CancellationToken.None));
    }

    [Test]
    public void FiadoComClienteInativo_Rejeita()
    {
        var produto = CriarProduto();
        CriarCaixaAberto();
        var cliente = CriarCliente(ativo: false);

        Assert.ThrowsAsync<ConflitoException>(() => CriarVendaService()
            .RegistrarAsync(Request(produto.Id, forma: "Fiado", clienteId: cliente.Id), CancellationToken.None));
    }

    [Test]
    public void FiadoAcimaDoLimite_Rejeita()
    {
        var produto = CriarProduto(preco: 10m);
        CriarCaixaAberto();
        var cliente = CriarCliente(limite: 15m, saldo: 10m);

        var ex = Assert.ThrowsAsync<ConflitoException>(() => CriarVendaService()
            .RegistrarAsync(Request(produto.Id, 1, "Fiado", clienteId: cliente.Id), CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("Limite de crédito"));
    }

    [Test]
    public async Task FiadoComLimiteZero_Permite()
    {
        var produto = CriarProduto(preco: 10m);
        CriarCaixaAberto();
        var cliente = CriarCliente(limite: 0m);

        var venda = await CriarVendaService()
            .RegistrarAsync(Request(produto.Id, 1, "Fiado", clienteId: cliente.Id), CancellationToken.None);

        Assert.That(venda.Status, Is.EqualTo("Concluida"));
        Assert.That((await Context.Clientes.FindAsync(cliente.Id))!.SaldoDevedor, Is.EqualTo(10m));
    }

    [Test]
    public async Task EstoqueInsuficiente_RejeitaSemGravar()
    {
        var produto = CriarProduto(estoque: 1);
        CriarCaixaAberto();

        Assert.ThrowsAsync<ConflitoException>(() => CriarVendaService()
            .RegistrarAsync(Request(produto.Id, 2), CancellationToken.None));

        Context.ChangeTracker.Clear();
        Assert.That(await Context.Vendas.CountAsync(), Is.Zero);
        Assert.That((await Context.Produtos.FindAsync(produto.Id))!.Estoque, Is.EqualTo(1));
    }

    [Test]
    public void TotalNegativo_Rejeita()
    {
        var produto = CriarProduto(preco: 5m);
        CriarCaixaAberto();

        Assert.ThrowsAsync<ValidacaoException>(() => CriarVendaService()
            .RegistrarAsync(Request(produto.Id, 1, desconto: 10m), CancellationToken.None));
    }

    [Test]
    public async Task VendaDinheiro_SucessoCompleto()
    {
        var produto = CriarProduto(preco: 10m, estoque: 10);
        var caixa = CriarCaixaAberto();

        var venda = await CriarVendaService().RegistrarAsync(
            Request(produto.Id, 2, desconto: 3m), CancellationToken.None);

        Assert.That(venda.ValorTotal, Is.EqualTo(17m));
        Assert.That(venda.CaixaId, Is.EqualTo(caixa.Id));
        Assert.That(venda.UsuarioCaixaId, Is.EqualTo(1));
        Assert.That(venda.Status, Is.EqualTo("Concluida"));
        Assert.That(venda.Itens.Single().PrecoUnitario, Is.EqualTo(10m));

        Context.ChangeTracker.Clear();
        Assert.That((await Context.Produtos.FindAsync(produto.Id))!.Estoque, Is.EqualTo(8));

        var mov = await Context.MovimentacoesEstoque.SingleAsync();
        Assert.That(mov.Tipo, Is.EqualTo("Saída"));
        Assert.That(mov.Quantidade, Is.EqualTo(2));
        Assert.That(mov.ReferenciaVendaId, Is.EqualTo(venda.Id));
        Assert.That(mov.Motivo, Is.EqualTo($"Venda #{venda.Id}"));
        Assert.That(mov.UsuarioId, Is.EqualTo(1));
    }

    [Test]
    public async Task VendaPix_GeraTxId()
    {
        var produto = CriarProduto(preco: 10m);
        CriarCaixaAberto();

        var venda = await CriarVendaService().RegistrarAsync(
            Request(produto.Id, forma: "PIX"), CancellationToken.None);

        Assert.That(venda.PixTxId, Is.EqualTo("TX-TESTE"));
    }

    [Test]
    public async Task Fiado_BaixaEstoqueEAumentaSaldoDevedor()
    {
        var produto = CriarProduto(preco: 10m, estoque: 5);
        CriarCaixaAberto();
        var cliente = CriarCliente(limite: 100m);

        await CriarVendaService().RegistrarAsync(
            Request(produto.Id, 2, "Fiado", clienteId: cliente.Id), CancellationToken.None);

        Context.ChangeTracker.Clear();
        Assert.That((await Context.Clientes.FindAsync(cliente.Id))!.SaldoDevedor, Is.EqualTo(20m));
        Assert.That((await Context.Produtos.FindAsync(produto.Id))!.Estoque, Is.EqualTo(3));
    }

    [Test]
    public void LinhasDuplicadas_SaoConsolidadasAntesDeValidarEstoque()
    {
        var produto = CriarProduto(estoque: 1);
        CriarCaixaAberto();
        var request = new CriarVendaRequest
        {
            FormaPagamento = "Dinheiro",
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1 },
                new() { ProdutoId = produto.Id, Quantidade = 1 }
            }
        };

        Assert.ThrowsAsync<ConflitoException>(() =>
            CriarVendaService().RegistrarAsync(request, CancellationToken.None));
    }

    [Test]
    public async Task PagamentoRecusado_FazRollback()
    {
        var produto = CriarProduto(estoque: 5);
        CriarCaixaAberto();
        var pagamento = new FakePagamentoIntegrator { Aprovar = false };

        Assert.ThrowsAsync<ConflitoException>(() => CriarVendaService(pagamento)
            .RegistrarAsync(Request(produto.Id, 1), CancellationToken.None));

        Context.ChangeTracker.Clear();
        Assert.That(await Context.Vendas.CountAsync(), Is.Zero);
        Assert.That((await Context.Produtos.FindAsync(produto.Id))!.Estoque, Is.EqualTo(5));
        Assert.That(await Context.MovimentacoesEstoque.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Cancelar_RestauraEstoqueEMarcaCancelada()
    {
        var produto = CriarProduto(preco: 10m, estoque: 5);
        CriarCaixaAberto();
        var service = CriarVendaService();
        var venda = await service.RegistrarAsync(Request(produto.Id, 2), CancellationToken.None);

        var cancelada = await service.CancelarAsync(venda.Id, "desistência", CancellationToken.None);

        Assert.That(cancelada.Status, Is.EqualTo("Cancelada"));

        Context.ChangeTracker.Clear();
        Assert.That((await Context.Produtos.FindAsync(produto.Id))!.Estoque, Is.EqualTo(5));

        var movimentos = await Context.MovimentacoesEstoque.OrderBy(m => m.Id).ToListAsync();
        Assert.That(movimentos.Count, Is.EqualTo(2));
        Assert.That(movimentos[^1].Tipo, Is.EqualTo("Entrada"));
        Assert.That(movimentos[^1].Motivo, Is.EqualTo($"Estorno - Venda #{venda.Id}"));
    }

    [Test]
    public async Task CancelarFiado_DevolveSaldoDevedorSemNegativar()
    {
        var produto = CriarProduto(preco: 10m);
        CriarCaixaAberto();
        var cliente = CriarCliente(limite: 100m);
        var service = CriarVendaService();
        var venda = await service.RegistrarAsync(
            Request(produto.Id, 1, "Fiado", clienteId: cliente.Id), CancellationToken.None);

        await service.CancelarAsync(venda.Id, null, CancellationToken.None);

        Context.ChangeTracker.Clear();
        Assert.That((await Context.Clientes.FindAsync(cliente.Id))!.SaldoDevedor, Is.Zero);
    }

    [Test]
    public async Task CancelarDuasVezes_RetornaConflito()
    {
        var produto = CriarProduto();
        CriarCaixaAberto();
        var service = CriarVendaService();
        var venda = await service.RegistrarAsync(Request(produto.Id), CancellationToken.None);
        await service.CancelarAsync(venda.Id, null, CancellationToken.None);

        Assert.ThrowsAsync<ConflitoException>(() =>
            service.CancelarAsync(venda.Id, null, CancellationToken.None));
    }
}

