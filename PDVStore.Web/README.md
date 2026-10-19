# PDVStore.Web

Versão web do **PDVStore**, criada como solução independente a partir do sistema
Windows Forms existente. O aplicativo desktop **não foi alterado** e continua
funcionando sozinho; a API usa o **mesmo schema** do banco SQL Server do PDVStore.

## O que está implementado

- Login com JWT e os mesmos perfis do desktop (Administrador, Operador e Estoquista).
- Dashboard com faturamento, vendas, ranking de produtos, formas de pagamento,
  alertas de estoque e gráfico diário.
- Produtos: consulta paginada, cadastro, edição, busca por código de barras e
  desativação lógica.
- Estoque: entradas/saídas manuais, alertas de reposição e histórico auditável.
- PDV: carrinho, desconto, PIX/cartão/dinheiro/fiado, comprovante e baixa de estoque.
- Vendas: histórico, detalhes e cancelamento com estorno de estoque/fiado.
- Caixa: abertura, sangria, fechamento calculado e histórico.
- Clientes: cadastro, limite de crédito, saldo devedor e baixa de fiado.
- Fornecedores e compras: entrada de mercadoria com atualização de custo e estoque.
- Usuários: cadastro, perfis, ativação/desativação e troca de senha.
- Swagger/OpenAPI, tratamento centralizado de erros, rate limiting e CORS.

> O front-end usa **React + TypeScript** (item 7 do briefing, mais específico que a
> menção genérica a JavaScript). A tipagem garante os contratos da API.

## Arquitetura

```text
PDVStore.Web/
├── backend/PDVStore.Api/        ASP.NET Core Web API + EF Core (SQL Server)
├── frontend/                    React 18 + TypeScript + Vite
├── tests/PDVStore.Api.Tests/    NUnit: serviços, integração HTTP e modelo SQL Server
├── docs/                        diagnóstico, arquitetura, API, banco, decisões e testes
├── PDVStore.Web.sln
├── .editorconfig / .gitignore
└── README.md
```

Fluxo: `React -> fetch centralizado -> Controllers -> Services -> EF Core -> SQL Server`.
Controllers só traduzem HTTP; regras de negócio ficam em `Services`. DTOs isolam o
contrato público das entidades e evitam ciclos de navegação.

## Tecnologias

| Camada | Tecnologia / versão |
|---|---|
| API | .NET 8 (ASP.NET Core Web API) |
| ORM | EF Core 9.0.17 (SQL Server; SQLite apenas em desenvolvimento/teste) |
| Banco | SQL Server / LocalDB — banco `PDV_StoreDB` do PDVStore |
| Autenticação | JWT Bearer + BCrypt (mesma tabela `Usuarios`) |
| Documentação | Swagger / OpenAPI (Swashbuckle 6.6.2) |
| Logs | Serilog (console + arquivo diário) |
| Testes | NUnit 3.13 + SQLite in-memory + `WebApplicationFactory` |
| Front-end | React 18.3, TypeScript 5.5, Vite 5.4, React Router 6 |

## Pré-requisitos

- .NET SDK 8.0+
- Node.js 20+ e npm
- SQL Server LocalDB (Windows) ou SQL Server/Express acessível
- Banco `PDV_StoreDB` já criado pelo PDVStore desktop (não crie outro por conveniência)

## Configuração do banco (importante)

A API **não executa migrations nem `EnsureCreated` no SQL Server**. Ela apenas lê e
grava no schema existente. A string de conexão do desktop fica em
`Helpers/ConnectionHelper.cs` (`Database=PDV_StoreDB`); informe a mesma base por
variável de ambiente:

```bash
ConnectionStrings__DefaultConnection="Server=(localdb)\\MSSQLLocalDB;Database=PDV_StoreDB;Trusted_Connection=True;"
```

Em produção, use autenticação do SQL Server/identidade gerenciada, nunca
`sa`/senha em texto no repositório. Migrations devem ser criadas no projeto
Windows, se e quando o schema mudar; a API não é dona do schema.

Para desenvolvimento em Linux/macOS sem SQL Server, `appsettings.Development.json`
usa **SQLite** (`pdvstore-dev.db`) e cria o schema automaticamente. Esse modo é
apenas local/demonstração: não é o banco de produção e nunca toca no SQL Server.

## Variáveis de ambiente

Copie `backend/PDVStore.Api/.env.example` e defina:

| Variável | Obrigatória | Exemplo |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | sim | `Development` |
| `ConnectionStrings__DefaultConnection` | sim (SQL Server) | ver acima |
| `Jwt__Key` | sim (≥32 chars) | gere com `openssl rand -base64 48` |
| `Jwt__Issuer` / `Jwt__Audience` | não | `PDVStore.Api` / `PDVStore.Web` |
| `Cors__AllowedOrigins__0` | não | `http://localhost:5173` |
| `Database__Provider` | não | `SqlServer` (padrão) ou `Sqlite` (dev) |

Sem `Jwt__Key` a API falha ao iniciar com mensagem explícita — não há chave
versionada. O front-end aceita `VITE_API_URL` (vazio = usa proxy do Vite).

## Como executar

### Back-end

```bash
cd backend/PDVStore.Api
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1   # apenas Linux sem ICU
export ASPNETCORE_ENVIRONMENT=Development
export Jwt__Key="$(openssl rand -base64 48)"
export ConnectionStrings__DefaultConnection="Data Source=pdvstore-dev.db"
dotnet run
```

API em `http://localhost:5080`. Swagger em `http://localhost:5080/swagger`.

Para SQL Server, use `Database__Provider=SqlServer` e a connection string do
PDVStore. Em Windows com LocalDB, `dotnet run` com a env var é suficiente.

### Front-end

```bash
cd frontend
npm ci
npm run dev
```

Interface em `http://localhost:5173`; o proxy encaminha `/api` e `/health` para
`http://localhost:5080`.

Login inicial: usuário **Admin** com a senha já cadastrada no banco do PDVStore
(seed original `admin123`). Troque a senha em “Alterar senha” após o primeiro acesso.

## Testes

```bash
cd tests/PDVStore.Api.Tests
dotnet test
```

Resultado desta entrega: **74 testes aprovados, 0 falhas**. Cobrem regras de venda,
estoque, compras, caixa, clientes, usuários, autenticação/autorização, contratos
HTTP e o DDL gerado para SQL Server. Detalhes em `docs/TESTES.md`.

```bash
cd frontend && npm run build   # tsc + build de produção
```

## Endpoints (resumo)

| Método | Rota | Perfil |
|---|---|---|
| POST | `/api/auth/login` | público (rate limit 5/min) |
| GET | `/api/auth/me` | autenticado |
| GET | `/api/produtos`, `/api/produtos/{id}`, `/codigo-barras/{codigo}` | autenticado |
| POST/PUT/DELETE | `/api/produtos` | Admin, Estoquista |
| GET | `/api/clientes` | Admin, Operador (PDV fiado) |
| POST/PUT/PATCH | `/api/clientes` | Admin |
| GET/POST/PATCH | `/api/fornecedores` | Admin |
| GET | `/api/caixa/aberto` | Admin, Operador |
| POST | `/api/caixa/abrir`, `/sangria`, `/fechar` | Admin |
| POST | `/api/vendas` | Admin, Operador |
| GET/POST | `/api/vendas`, `/resumo`, `/{id}/cancelar` | Admin |
| POST | `/api/estoque/entradas`, `/saidas` | Admin |
| GET | `/api/estoque/movimentacoes`, `/alertas` | Admin |
| POST/GET/POST | `/api/compras` | Admin |
| GET | `/api/relatorios/dashboard` | Admin |
| GET/POST/PUT/DELETE | `/api/usuarios` | Admin |

Documentação completa com payloads e exemplos: `docs/API.md`.

## Segurança

- Senhas BCrypt na tabela original; a API nunca devolve `SenhaHash`.
- JWT com expiração configurável, validação de issuer/audience/assinatura.
- Autorização por política alinhada ao menu do desktop.
- Rate limit global e específico no login; CORS restrito por configuração.
- Connection string/JWT key apenas em variáveis de ambiente.
- Erros padronizados em `ProblemDetails`, sem stack trace para o cliente.
- Operações de estoque/venda/compra/cancelamento em transação `Serializable`.

## Relação com o PDVStore desktop

- Nenhum arquivo do projeto Windows foi criado, alterado ou removido por esta solução.
- Não há referência de projeto entre a API e o WinForms; o compartilhamento é o banco.
- As entidades foram **reimplementadas** na API com os mesmos nomes, tabelas e regras.
- Divergências deliberadas e limitações estão em `docs/DECISOES.md` e `docs/BANCO-DE-DADOS.md`.

## Limitações conhecidas

- Não foi possível executar contra um SQL Server real neste ambiente; a
  compatibilidade foi validada por testes de modelo/DDL e SQLite. Rode o checklist
  de `docs/BANCO-DE-DADOS.md` antes de apontar para produção.
- Integração de pagamento continua mock (sempre aprova e gera `PixTxId` no PIX).
- Exportações PDF/Excel do desktop não foram portadas.
- Sem rowversion no schema: a concorrência é tratada por transação serializável.
