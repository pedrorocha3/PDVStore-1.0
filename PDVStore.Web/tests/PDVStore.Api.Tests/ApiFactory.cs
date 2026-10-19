using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PDVStore.Api.Tests;

/// <summary>
/// Fábrica de integração. Sobe a API real em memória usando SQLite em arquivo
/// temporário (provider de desenvolvimento) e chave JWT exclusiva de teste.
/// O SQL Server do PDVStore nunca é acessado pelos testes.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"pdvstore-tests-{Guid.NewGuid():N}.db");

    public ApiFactory()
    {
        // WebApplicationFactory aplica ConfigureAppConfiguration depois que o
        // Program já leu a configuração (minimal hosting). Variáveis de ambiente
        // garantem que a API de teste use SQLite e uma chave JWT de teste.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("Jwt__Key", "chave-de-integracao-com-mais-de-32-caracteres");
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
        Environment.SetEnvironmentVariable("Database__EnsureCreatedForDevelopment", "true");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"Data Source={_dbPath}");
        Environment.SetEnvironmentVariable("RateLimiting__LoginPermitLimit", "1000");
        Environment.SetEnvironmentVariable("RateLimiting__GlobalPermitLimit", "10000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-de-integracao-com-mais-de-32-caracteres",
                ["Jwt:Issuer"] = "PDVStore.Api",
                ["Jwt:Audience"] = "PDVStore.Web",
                ["Database:Provider"] = "Sqlite",
                ["Database:EnsureCreatedForDevelopment"] = "true",
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                ["RateLimiting:LoginPermitLimit"] = "1000",
                ["RateLimiting:GlobalPermitLimit"] = "10000"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        foreach (var arquivo in new[] { _dbPath, _dbPath + "-shm", _dbPath + "-wal" })
        {
            try
            {
                if (File.Exists(arquivo))
                    File.Delete(arquivo);
            }
            catch
            {
                // Arquivo temporário: falha de limpeza não invalida o teste.
            }
        }
    }
}
