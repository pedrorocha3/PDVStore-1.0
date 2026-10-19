# Arquitetura da solução web

## Visão geral

```text
┌────────────────────────────┐        HTTPS/JSON        ┌──────────────────────────────┐
│ React 18 + TypeScript      │ ───────────────────────▶ │ ASP.NET Core 8 Web API       │
│ Vite · React Router        │ ◀─────────────────────── │ Controllers → Services       │
└────────────────────────────┘      ProblemDetails      │ EF Core 9 (SQL Server)       │
                                                        └──────────────┬───────────────┘
                                                                       │ mesmo schema
                                                        ┌──────────────▼───────────────┐
                                                        │ SQL Server · PDV_StoreDB     │
                                                        │ (compartilhado com WinForms) │
                                                        └──────────────────────────────┘
```

## Back-end: camadas e responsabilidades

| Pasta | Conteúdo |
|---|---|
| `Controllers` | Rotas HTTP, `[Authorize]`, status codes; sem regra de negócio |
| `Services` | Casos de uso, validações, transações, mapeamento para DTO |
| `Contracts` | DTOs de entrada/saída, queries paginadas, validação por DataAnnotations |
| `Models` | Entidades com os mesmos conceitos do desktop |
| `Data` | `PdvStoreDbContext` (mapeamento Fluent API, sem migrations) |
| `Security` | `JwtOptions`, `TokenService`, políticas e papéis |
| `Middleware` | `ExceptionHandlingMiddleware` → ProblemDetails |

### Injeção de dependência

- `PdvStoreDbContext`: scoped.
- Serviços de domínio: scoped (`ProdutoService`, `VendaService`, etc.).
- `ICurrentUser`: scoped, lê claims do JWT (`sub`, `name`, `role`).
- `IPagamentoIntegrator`: scoped, mock substituível em testes.
- `TokenService`: singleton (configuração imutável).

### Tratamento de erros

| Exceção | HTTP | Uso |
|---|---|---|
| `ValidacaoException` | 400 | dados/regra de entrada inválidos |
| `NaoEncontradoException` | 404 | recurso inexistente |
| `ConflitoException` | 409 | caixa aberto, estoque insuficiente, limite, já cancelado |
| `KeyNotFoundException` | 404 | compatibilidade com serviços |
| demais | 500 | logado com stack trace; cliente recebe mensagem genérica |

Formato:

```json
{
  "title": "Conflito de regra de negócio",
  "status": 409,
  "detail": "Estoque insuficiente para o produto 'Café'.",
  "instance": "/api/vendas",
  "traceId": "0HN..."
}
```

### Concorrência e transações

Venda, compra, cancelamentos, abertura de caixa e ajustes de estoque usam
`BeginTransactionAsync(IsolationLevel.Serializable)`. Isso evita que duas
requisições concorrentes validem o mesmo saldo e gerem estoque negativo, sem
exigir `rowversion` (que alteraria o schema compartilhado).

Para grandes volumes, o próximo passo recomendado é adicionar `rowversion`
apenas com autorização e migration criada no projeto Windows.

### Paginação, filtros e ordenação

`PagedResult<T>` e `PaginacaoQuery` centralizam o contrato. `PageSize` é limitado
a 100. Produtos aceitam busca, categoria, status e ordenação; movimentos aceitam
período, produto e tipo; vendas/compras aceitam período e status.

### Logging

Serilog com console + arquivo diário (`logs/pdvstore-web-.log`, 14 dias). O
middleware registra erros não tratados; vendas, compras e ajustes registram
usuário, identificador e valores (sem senhas/tokens).

## Front-end

```text
src/
├── components/    UI reutilizável + formulários por domínio
├── hooks/         AuthContext e ToastContext
├── layouts/       Layout com menu por perfil
├── pages/         Login, Dashboard, Produtos, Estoque, PDV, Vendas, Caixa,
│                  Clientes, Fornecedores, Compras, Usuários, 404
├── routes/        Rotas protegidas por autenticação e papel
├── services/      Cliente HTTP central + um serviço por domínio
├── types/         Contratos TypeScript da API
└── utils/         Formatação BRL/data e períodos
```

- `services/api.ts` centraliza `fetch`, token, tratamento de `ProblemDetails`,
  `204 No Content` e logout automático em 401.
- `RequireAuth` valida a sessão com `/api/auth/me`; `RequireRole` replica as
  permissões do menu WinForms.
- Formulários têm validação local, estado de envio e mensagens de sucesso/erro.
- Nenhum dado fictício: todas as telas consomem endpoints reais.

## Decisões de escopo

- Autenticação/autorização foram implementadas porque a API fica exposta na rede.
- Exportação PDF/Excel e edição de foto de usuário ficaram fora do primeiro ciclo.
- Não foi criada uma camada `Repository` genérica: o EF Core já é o repositório e
  interfaces adicionais não teriam uso concreto.
- Não foi usado AutoMapper: os mapeamentos são explícitos e pequenos.
