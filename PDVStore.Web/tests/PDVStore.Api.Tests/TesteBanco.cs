using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PDVStore.Api.Data;
using PDVStore.Api.Models;
using PDVStore.Api.Services;

namespace PDVStore.Api.Tests;

/// <summary>
/// Base dos testes de serviço. Usa SQLite em memória (mesma estratégia dos
/// testes do projeto Windows), garantindo isolamento total entre testes.
/// </summary>
public abstract class TesteBanco
{
    protected SqliteConnection Conexao = null!;
    protected PdvStoreDbContext Context = null!;
    protected FakeCurrentUser UsuarioAtual = null!;

    [SetUp]
    public void BaseSetup()
    {
        Conexao = new SqliteConnection("DataSource=:memory:");
        Conexao.Open();

        var options = new DbContextOptionsBuilder<PdvStoreDbContext>()
            .UseSqlite(Conexao)
            .Options;

        Context = new PdvStoreDbContext(options);
        Context.Database.EnsureCreated();

        UsuarioAtual = new FakeCurrentUser { Id = 1, Nome = "Admin", Permissao = TipoPermissao.Administrador };
    }

    [TearDown]
    public void BaseTearDown()
    {
        Context.Dispose();
        Conexao.Dispose();
    }

    protected Produto CriarProduto(
        string nome = "Café", string codigo = "7891000000011", decimal preco = 10m,
        int estoque = 10, int estoqueMinimo = 2)
    {
        var produto = new Produto
        {
            Nome = nome,
            CodigoBarras = codigo,
            Preco = preco,
            PrecoCusto = 6m,
            Estoque = estoque,
            EstoqueMinimo = estoqueMinimo,
            Categoria = "Bebidas",
            Descricao = "Produto de teste",
            Ativo = true
        };
        Context.Produtos.Add(produto);
        Context.SaveChanges();
        return produto;
    }

    protected Caixa CriarCaixaAberto(decimal valorInicial = 100m)
    {
        var caixa = new Caixa
        {
            UsuarioCaixaId = 1,
            Abertura = DateTime.UtcNow,
            ValorInicial = valorInicial,
            Status = "Aberto"
        };
        Context.Caixas.Add(caixa);
        Context.SaveChanges();
        return caixa;
    }

    protected Cliente CriarCliente(decimal limite = 0m, decimal saldo = 0m, bool ativo = true, string nome = "Cliente Teste")
    {
        var cliente = new Cliente
        {
            Nome = nome,
            Ativo = ativo,
            LimiteCredito = limite,
            SaldoDevedor = saldo,
            CadastradoEm = DateTime.UtcNow
        };
        Context.Clientes.Add(cliente);
        Context.SaveChanges();
        return cliente;
    }

    protected VendaService CriarVendaService(FakePagamentoIntegrator? pagamento = null)
        => new(Context, pagamento ?? new FakePagamentoIntegrator(), UsuarioAtual, NullLogger<VendaService>.Instance);
}

/// <summary>Usuário autenticado controlável, no lugar do Session global do WinForms.</summary>
public class FakeCurrentUser : ICurrentUser
{
    public int? Id { get; set; } = 1;
    public string? Nome { get; set; } = "Admin";
    public TipoPermissao? Permissao { get; set; } = TipoPermissao.Administrador;
    public bool Autenticado => Id.HasValue;
}

/// <summary>Pagamento sem latência, com resultado controlável.</summary>
public class FakePagamentoIntegrator : IPagamentoIntegrator
{
    public bool Aprovar { get; set; } = true;

    public Task<bool> ProcessarPagamentoAsync(Venda venda, CancellationToken cancellationToken = default)
    {
        if (Aprovar && venda.FormaPagamento.Equals("PIX", StringComparison.OrdinalIgnoreCase))
            venda.PixTxId = "TX-TESTE";

        return Task.FromResult(Aprovar);
    }
}
