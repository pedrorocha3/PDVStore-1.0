using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PDVStore.Api.Data;
using PDVStore.Api.Middleware;
using PDVStore.Api.Security;
using PDVStore.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// ---------- Banco de dados (mesmo schema SQL Server do PDVStore) ----------
var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<PdvStoreDbContext>(options =>
{
    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection é obrigatória para o provider Sqlite.");
        options.UseSqlite(connectionString);
    }
    else
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection não configurada. Use variável de ambiente " +
                "ConnectionStrings__DefaultConnection (SQL Server do PDVStore).");
        options.UseSqlServer(connectionString, sql => sql.CommandTimeout(60));
    }
});

// ---------- Serviços de aplicação ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IPagamentoIntegrator, MockPagamentoIntegrator>();
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<FornecedorService>();
builder.Services.AddScoped<CaixaService>();
builder.Services.AddScoped<VendaService>();
builder.Services.AddScoped<CompraService>();
builder.Services.AddScoped<RelatorioService>();
builder.Services.AddScoped<UsuarioService>();

// ---------- Autenticação / autorização (JWT lendo a tabela Usuarios) ----------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);

var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key não configurada (mínimo 32 caracteres). Defina a variável de ambiente Jwt__Key. " +
        "Nunca versione a chave real.");
}

builder.Services.AddSingleton<TokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Administrador));
    options.AddPolicy(Policies.Pdv, p => p.RequireRole(Roles.Administrador, Roles.Operador));
    options.AddPolicy(Policies.Catalogo, p => p.RequireRole(Roles.Administrador, Roles.Estoquista));
});

// ---------- Controllers, JSON, CORS, rate limiting ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();

var origens = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
              ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy => policy
        .WithOrigins(origens)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Proteção do login contra força bruta. Limites configuráveis para testes.
var loginPermitLimit = builder.Configuration.GetValue("RateLimiting:LoginPermitLimit", 5);
var globalPermitLimit = builder.Configuration.GetValue("RateLimiting:GlobalPermitLimit", 300);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = globalPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------- Swagger / OpenAPI ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PDVStore Web API",
        Version = "v1",
        Description = "API do PDVStore: produtos, estoque, vendas, compras, caixa, clientes, fornecedores, usuários e relatórios."
    });

    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe apenas o token retornado por POST /api/auth/login.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    options.AddSecurityDefinition("Bearer", esquema);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = Array.Empty<string>() });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PDVStore Web API v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    servico = "PDVStore.Api",
    versao = "1.0.0",
    databaseProvider = provider,
    utc = DateTime.UtcNow
})).AllowAnonymous();

// Cria o schema APENAS no SQLite de desenvolvimento/teste local.
// Nunca é executado no SQL Server compartilhado com o PDVStore.
if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue<bool>("Database:EnsureCreatedForDevelopment"))
{
    if (!provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Database:EnsureCreatedForDevelopment só é permitido com provider Sqlite. " +
            "O schema do SQL Server do PDVStore não pode ser alterado pela API.");
    }

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PdvStoreDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

/// <summary>Marcador público para testes de integração (WebApplicationFactory).</summary>
public partial class Program;

