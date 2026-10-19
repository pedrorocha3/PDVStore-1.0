using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using PDVStore.Api.Contracts;

namespace PDVStore.Api.Tests;

[TestFixture]
public class ApiIntegrationTests
{
    private static readonly JsonSerializerOptions JsonApi = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> LoginAdminAsync()
    {
        var resposta = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Nome = "Admin", Senha = "admin123" });
        resposta.EnsureSuccessStatusCode();
        var login = await resposta.Content.ReadFromJsonAsync<LoginResponse>(JsonApi);
        return login!.Token;
    }

    private void Autorizar(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Test]
    public async Task Health_EhAnonimo()
    {
        var resposta = await _client.GetAsync("/health");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await resposta.Content.ReadAsStringAsync();
        Assert.That(json, Does.Contain("PDVStore.Api"));
        Assert.That(json, Does.Not.Contain("Password="));
    }

    [Test]
    public async Task Swagger_EstaDisponivel()
    {
        var resposta = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await resposta.Content.ReadAsStringAsync();
        Assert.That(json, Does.Contain("/api/vendas"));
        Assert.That(json, Does.Contain("bearer"));
    }

    [Test]
    public async Task Login_Invalido_Retorna401()
    {
        var resposta = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Nome = "Admin", Senha = "errada" });

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task EndpointProtegido_SemToken_Retorna401()
    {
        var resposta = await _client.GetAsync("/api/produtos");

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Admin_ConsegueCriarProdutoEListar()
    {
        Autorizar(await LoginAdminAsync());

        var criar = await _client.PostAsJsonAsync("/api/produtos", new CriarProdutoRequest
        {
            CodigoBarras = "7891000000099",
            Nome = "Produto Integração",
            Preco = 9.90m,
            PrecoCusto = 5m,
            Estoque = 4,
            EstoqueMinimo = 1,
            Categoria = "Testes",
            Descricao = "Criado via teste de integração"
        });

        Assert.That(criar.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var lista = await _client.GetFromJsonAsync<PagedResult<ProdutoDto>>("/api/produtos");
        Assert.That(lista!.TotalItems, Is.EqualTo(1));
        Assert.That(lista.Items[0].Nome, Is.EqualTo("Produto Integração"));
    }

    [Test]
    public async Task Operador_NaoPodeGerenciarProdutosNemEstoque()
    {
        Autorizar(await LoginAdminAsync());

        var criarUsuario = await _client.PostAsJsonAsync("/api/usuarios", new CriarUsuarioRequest
        {
            Nome = "Operador Integração",
            Senha = "senha123",
            Permissao = PDVStore.Api.Models.TipoPermissao.Operador
        });
        Assert.That(criarUsuario.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Nome = "Operador Integração", Senha = "senha123" });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>(JsonApi))!.Token;
        Autorizar(token);

        var tentativaProduto = await _client.PostAsJsonAsync("/api/produtos", new CriarProdutoRequest
        {
            CodigoBarras = "1",
            Nome = "X",
            Categoria = "X",
            Descricao = "X"
        });

        Assert.That(tentativaProduto.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await _client.GetAsync("/api/estoque/alertas")).StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await _client.GetAsync("/api/produtos")).StatusCode,
            Is.EqualTo(HttpStatusCode.OK), "Operador precisa consultar produtos no PDV.");
    }

    [Test]
    public async Task ErroDeValidacao_Retorna400ComProblemDetails()
    {
        Autorizar(await LoginAdminAsync());

        var resposta = await _client.PostAsJsonAsync("/api/produtos", new CriarProdutoRequest
        {
            CodigoBarras = "",
            Nome = "",
            Categoria = "",
            Descricao = ""
        });

        Assert.That(resposta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var json = await resposta.Content.ReadAsStringAsync();
        Assert.That(json, Does.Contain("errors").Or.Contain("Código de barras"));
    }

    [Test]
    public async Task FluxoVendaCompleto_PelaApi()
    {
        Autorizar(await LoginAdminAsync());

        await _client.PostAsJsonAsync("/api/produtos", new CriarProdutoRequest
        {
            CodigoBarras = "555", Nome = "Vendável", Preco = 10m, PrecoCusto = 4m,
            Estoque = 5, EstoqueMinimo = 1, Categoria = "Teste", Descricao = "Teste"
        });
        var caixaResp = await _client.PostAsJsonAsync("/api/caixa/abrir", new AbrirCaixaRequest { ValorInicial = 50m });
        Assert.That(caixaResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var vendaResp = await _client.PostAsJsonAsync("/api/vendas", new CriarVendaRequest
        {
            FormaPagamento = "Dinheiro",
            Desconto = 2m,
            Itens = new List<ItemVendaRequest> { new() { ProdutoId = 1, Quantidade = 2 } }
        });

        Assert.That(vendaResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var venda = await vendaResp.Content.ReadFromJsonAsync<VendaDto>(JsonApi);
        Assert.That(venda!.ValorTotal, Is.EqualTo(18m));

        var produto = await _client.GetFromJsonAsync<ProdutoDto>("/api/produtos/1");
        Assert.That(produto!.Estoque, Is.EqualTo(3));

        var fechar = await _client.PostAsync($"/api/caixa/{venda.CaixaId}/fechar", null);
        Assert.That(fechar.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var caixa = await fechar.Content.ReadFromJsonAsync<CaixaDto>(JsonApi);
        Assert.That(caixa!.ValorFinal, Is.EqualTo(68m));
    }
}

