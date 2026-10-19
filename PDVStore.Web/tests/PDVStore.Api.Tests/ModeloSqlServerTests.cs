using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Api.Data;

namespace PDVStore.Api.Tests;

/// <summary>
/// Valida o mapeamento do modelo para SQL Server SEM conectar ao banco.
/// Garante que a API usa as mesmas tabelas, colunas e precisão do PDVStore
/// original e que nenhuma migration é necessária para o schema existente.
/// </summary>
[TestFixture]
public class ModeloSqlServerTests
{
    private static string GerarScript()
    {
        var options = new DbContextOptionsBuilder<PdvStoreDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=PDV_StoreDB;Trusted_Connection=True;")
            .Options;

        using var context = new PdvStoreDbContext(options);
        return context.Database.GenerateCreateScript();
    }

    [Test]
    public void Script_ContemAsDezTabelasDoPdvStore()
    {
        var script = GerarScript();

        foreach (var tabela in new[]
                 {
                     "Produtos", "Clientes", "Fornecedores", "Usuarios", "Caixas",
                     "Vendas", "ItensVendas", "Compras", "ItensCompras", "MovimentacoesEstoque"
                 })
        {
            Assert.That(script, Does.Contain($"CREATE TABLE [{tabela}]"), $"Tabela {tabela} ausente.");
        }
    }

    [Test]
    public void Script_UsaColunasEPrecisaoCompativeis()
    {
        var script = GerarScript();

        Assert.That(script, Does.Contain("[EstoqueMinimo] int NOT NULL"));
        Assert.That(script, Does.Contain("[Preco] decimal(18,2) NOT NULL"));
        Assert.That(script, Does.Contain("[SaldoDevedor] decimal(18,2) NOT NULL"));
        Assert.That(script, Does.Contain("[PixTxId] nvarchar(max) NULL"));
        Assert.That(script, Does.Contain("[ReferenciaVendaId] int NULL"));
        Assert.That(script, Does.Contain("[ClienteId] int NULL"));
    }

    [Test]
    public void Script_NaoCriaTabelaDeMigrationsNemAlteraNomeDeTabelas()
    {
        var script = GerarScript();

        Assert.That(script, Does.Not.Contain("__EFMigrationsHistory"));
        Assert.That(script, Does.Not.Contain("CREATE TABLE [ItemVenda]"));
        Assert.That(script, Does.Not.Contain("CREATE TABLE [UsuarioCaixa]"));
    }
}
