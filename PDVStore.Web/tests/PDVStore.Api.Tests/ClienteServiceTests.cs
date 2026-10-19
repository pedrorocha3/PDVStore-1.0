using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Api.Contracts;
using PDVStore.Api.Services;

namespace PDVStore.Api.Tests;

[TestFixture]
public class ClienteServiceTests : TesteBanco
{
    private ClienteService Service => new(Context);

    [Test]
    public async Task Criar_Valido_DefineDataUtcEAtivo()
    {
        var dto = await Service.CriarAsync(new CriarClienteRequest
        {
            Nome = "Maria",
            CpfCnpj = "123",
            LimiteCredito = 50m
        }, CancellationToken.None);

        Assert.That(dto.Id, Is.GreaterThan(0));
        Assert.That(dto.Ativo, Is.True);
        Assert.That(dto.CadastradoEm.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void Criar_SemNome_Rejeita()
    {
        Assert.ThrowsAsync<ValidacaoException>(() => Service.CriarAsync(
            new CriarClienteRequest { Nome = "  " }, CancellationToken.None));
    }

    [Test]
    public async Task Atualizar_NaoAlteraSaldoNemStatus()
    {
        var cliente = CriarCliente(limite: 50m, saldo: 20m);
        cliente.Ativo = false;
        await Context.SaveChangesAsync();

        var dto = await Service.AtualizarAsync(cliente.Id, new AtualizarClienteRequest
        {
            Nome = "Maria Silva",
            LimiteCredito = 80m
        }, CancellationToken.None);

        Assert.That(dto.Nome, Is.EqualTo("Maria Silva"));
        Assert.That(dto.LimiteCredito, Is.EqualTo(80m));
        Assert.That(dto.SaldoDevedor, Is.EqualTo(20m));
        Assert.That(dto.Ativo, Is.False);
    }

    [Test]
    public async Task AtualizarStatus_AtivaEDesativa()
    {
        var cliente = CriarCliente();

        await Service.AtualizarStatusAsync(cliente.Id, false, CancellationToken.None);
        Assert.That((await Context.Clientes.FindAsync(cliente.Id))!.Ativo, Is.False);

        await Service.AtualizarStatusAsync(cliente.Id, true, CancellationToken.None);
        Assert.That((await Context.Clientes.FindAsync(cliente.Id))!.Ativo, Is.True);
    }

    [Test]
    public async Task ReceberFiado_ReduzSaldoSemNegativar()
    {
        var cliente = CriarCliente(saldo: 30m);

        var dto = await Service.ReceberFiadoAsync(cliente.Id, 10m, CancellationToken.None);
        Assert.That(dto.SaldoDevedor, Is.EqualTo(20m));

        var quitado = await Service.ReceberFiadoAsync(cliente.Id, 100m, CancellationToken.None);
        Assert.That(quitado.SaldoDevedor, Is.Zero);
    }

    [Test]
    public void ReceberFiado_ValorInvalido_Rejeita()
    {
        Assert.ThrowsAsync<ValidacaoException>(() =>
            Service.ReceberFiadoAsync(1, 0m, CancellationToken.None));
    }

    [Test]
    public async Task Listar_FiltraPorNomeCpfEStatus()
    {
        var maria = CriarCliente(nome: "Maria");
        maria.CpfCnpj = "999";
        CriarCliente(nome: "João");
        var inativo = CriarCliente(nome: "Pedro");
        inativo.Ativo = false;
        await Context.SaveChangesAsync();

        var busca = await Service.ListarAsync(new ClienteQuery { Busca = "999" }, CancellationToken.None);
        Assert.That(busca.TotalItems, Is.EqualTo(1));
        Assert.That(busca.Items[0].Nome, Is.EqualTo("Maria"));

        var ativos = await Service.ListarAsync(new ClienteQuery(), CancellationToken.None);
        Assert.That(ativos.TotalItems, Is.EqualTo(2));
    }
}
