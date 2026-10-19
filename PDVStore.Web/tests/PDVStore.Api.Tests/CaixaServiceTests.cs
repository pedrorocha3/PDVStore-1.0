using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Api.Contracts;
using PDVStore.Api.Models;
using PDVStore.Api.Services;

namespace PDVStore.Api.Tests;

[TestFixture]
public class CaixaServiceTests : TesteBanco
{
    private CaixaService Service => new(Context, UsuarioAtual);

    [Test]
    public async Task Abrir_QuandoNaoHaCaixaAberto_CriaAberto()
    {
        var caixa = await Service.AbrirAsync(new AbrirCaixaRequest { ValorInicial = 50m }, CancellationToken.None);

        Assert.That(caixa.Status, Is.EqualTo("Aberto"));
        Assert.That(caixa.ValorInicial, Is.EqualTo(50m));
        Assert.That(caixa.UsuarioCaixaId, Is.EqualTo(1));
    }

    [Test]
    public void Abrir_ComOutroAberto_RetornaConflito()
    {
        CriarCaixaAberto();

        var ex = Assert.ThrowsAsync<ConflitoException>(() =>
            Service.AbrirAsync(new AbrirCaixaRequest { ValorInicial = 10m }, CancellationToken.None));
        Assert.That(ex!.Message, Does.Contain("Já existe um caixa aberto"));
    }

    [Test]
    public async Task Sangria_AcumulaSomenteComCaixaAberto()
    {
        var caixa = CriarCaixaAberto(100m);

        await Service.RegistrarSangriaAsync(caixa.Id, new SangriaRequest { Valor = 20m }, CancellationToken.None);
        var dto = await Service.RegistrarSangriaAsync(caixa.Id, new SangriaRequest { Valor = 5m }, CancellationToken.None);

        Assert.That(dto.Sangria, Is.EqualTo(25m));
        Assert.That(dto.ValorFinal, Is.EqualTo(100m));
    }

    [Test]
    public void Sangria_ValorInvalido_Rejeita()
    {
        Assert.ThrowsAsync<ValidacaoException>(() =>
            Service.RegistrarSangriaAsync(1, new SangriaRequest { Valor = 0m }, CancellationToken.None));
    }

    [Test]
    public async Task Fechar_CalculaInicialMaisVendasConcluidasMenosSangria()
    {
        var caixa = CriarCaixaAberto(100m);
        await Service.RegistrarSangriaAsync(caixa.Id, new SangriaRequest { Valor = 10m }, CancellationToken.None);

        Context.Vendas.AddRange(
            new Venda { CaixaId = caixa.Id, UsuarioCaixaId = 1, ValorTotal = 30m, Status = "Concluida", FormaPagamento = "Dinheiro", DataVenda = DateTime.UtcNow },
            new Venda { CaixaId = caixa.Id, UsuarioCaixaId = 1, ValorTotal = 99m, Status = "Cancelada", FormaPagamento = "Dinheiro", DataVenda = DateTime.UtcNow });
        await Context.SaveChangesAsync();

        var dto = await Service.FecharAsync(caixa.Id, CancellationToken.None);

        Assert.That(dto.Status, Is.EqualTo("Fechado"));
        Assert.That(dto.ValorFinal, Is.EqualTo(120m));
        Assert.That(dto.Fechamento, Is.Not.Null);
    }

    [Test]
    public async Task Fechar_CaixaJaFechado_RetornaConflito()
    {
        var caixa = CriarCaixaAberto();
        await Service.FecharAsync(caixa.Id, CancellationToken.None);

        Assert.ThrowsAsync<ConflitoException>(() => Service.FecharAsync(caixa.Id, CancellationToken.None));
    }

    [Test]
    public async Task Listar_OrdenaDoMaisRecente()
    {
        var antigo = CriarCaixaAberto();
        antigo.Abertura = DateTime.UtcNow.AddDays(-2);
        antigo.Status = "Fechado";
        var recente = CriarCaixaAberto();
        recente.Abertura = DateTime.UtcNow;
        await Context.SaveChangesAsync();

        var lista = await Service.ListarAsync(new PaginacaoQuery(), CancellationToken.None);

        Assert.That(lista.TotalItems, Is.EqualTo(3)); // inclui o caixa seed id=1
        Assert.That(lista.Items[0].Id, Is.EqualTo(recente.Id));
    }
}
