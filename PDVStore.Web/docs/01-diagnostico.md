# Etapa 1 — Diagnóstico do PDVStore original

Fontes analisadas: `Models/*.cs`, `Data/PDVContext.cs`, `Services/*.cs`, `ViewModels/*.cs`,
`Forms/frmPDV.cs`, `frmGerenciarProdutos.cs`, `frmCaixa.cs`, `frmCompras.cs`,
`frmMenuPrincipal.cs`, `Program.cs`, `Helpers/ConnectionHelper.cs`, `Migrations/*`,
`PDVStore.Tests/*` e `README.md`.

## 1. Arquitetura identificada

Aplicação **WinForms .NET 8** (`net8.0-windows7.0`), organizada em:

| Pasta | Responsabilidade |
|---|---|
| `Models` | Entidades, enum `TipoPermissao`, `Session` estática |
| `Data` | `PDVContext` + factory de design-time |
| `Migrations` | Histórico do schema SQL Server |
| `Services` | Regras de negócio (venda, estoque, caixa, compra, cliente, fornecedor, usuário, relatório) |
| `ViewModels` | Estado de tela (PDV, Dashboard) |
| `Forms` | Interface WinForms |
| `Integrations` | Mock de pagamento |
| `Helpers` | Connection string, máscaras, prompt |

DI via `Host.CreateDefaultBuilder` + `AddTransient`; Serilog em arquivo; EF Core 9.0.17
com SQL Server; testes NUnit com SQLite in-memory.

## 2. Banco e conexão

- Provider: **SQL Server** (`Microsoft.EntityFrameworkCore.SqlServer` 9.0.17).
- Runtime: `Helpers/ConnectionHelper.GetConnectionString()` →
  `Server=(localdb)\MSSQLLocalDB;Database=PDV_StoreDB;Trusted_Connection=True;`.
- `appsettings.json` contém `Database=PDVStore`, mas **não é usado** pelo `Program.cs`
  (a string efetiva é a do `ConnectionHelper`).
- 10 tabelas: `Produtos`, `Clientes`, `Fornecedores`, `Usuarios`, `Caixas`, `Vendas`,
  `ItensVendas`, `Compras`, `ItensCompras`, `MovimentacoesEstoque`.
- Decimais `decimal(18,2)`, datas `datetime2`, chaves identity int.
- Soft delete por `Ativo` em produtos, clientes, fornecedores e usuários.

## 3. Entidades e relacionamentos

- `Produto` 1:N `ItemVenda`, `ItemCompra`, `MovimentacaoEstoque`.
- `Venda` 1:N `ItemVenda` (Cascade); N:1 `Cliente` (SetNull), `Caixa` (Restrict),
  `UsuarioCaixa` (Restrict).
- `Compra` 1:N `ItemCompra` (Cascade); N:1 `Fornecedor` e `UsuarioCaixa` (Restrict).
- `Caixa` N:1 `UsuarioCaixa` (Restrict); 1:N `Vendas`.
- `MovimentacaoEstoque` N:1 `Produto` e `Usuario` (Restrict, usuário opcional).
- Seeds: `Caixa` id 1 fechado e usuário `Admin` id 1 (hash BCrypt de `admin123`).

## 4. Funcionalidades e regras que precisam ser preservadas

### Produtos
- Código de barras, nome, categoria e descrição obrigatórios na tela.
- Preço, custo, estoque e estoque mínimo não podem ser negativos.
- Exclusão é **lógica** (`Ativo = false`); consultas padrão só mostram ativos.
- Busca por nome/código; consulta por código só retorna ativo.

### Estoque
- Ajuste manual exige quantidade > 0; produto existente; saída não pode negativar.
- Cada ajuste grava `MovimentacaoEstoque` com tipo, quantidade, data UTC, usuário e motivo.
- Histórico filtrado por período e ordenado do mais recente.
- Alerta: produto ativo com `Estoque <= EstoqueMinimo`.

### Vendas
- Exige ao menos um item, forma de pagamento e **caixa aberto**.
- Total = soma dos subtotais − desconto; total negativo é rejeitado.
- Valida estoque de todos os itens antes de gravar.
- Grava cabeçalho + itens, baixa estoque e cria movimentação `Saída` com
  `ReferenciaVendaId`, motivo `Venda #id` e custo unitário.
- Fiado exige cliente ativo e respeita `LimiteCredito` quando > 0; soma ao `SaldoDevedor`.
- Pagamento mock sempre aprova; PIX gera `PixTxId`.
- Cancelamento devolve estoque, cria movimentação `Entrada`
  (`Estorno - Venda #id`), reduz saldo devedor com piso 0 e marca `Cancelada`.

### Compras
- Exige ≥1 item, quantidade > 0, custo ≥ 0 e produtos existentes.
- Total = soma dos subtotais; aumenta estoque; atualiza `PrecoCusto` quando > 0;
  grava movimentação `Entrada` com motivo `Compra #id - nota`.
- Cancelamento reduz estoque com piso 0 e marca `Cancelada`.

### Caixa
- Só um caixa aberto por vez.
- Sangria exige valor > 0 e caixa aberto; acumula no campo `Sangria`.
- Fechamento = `ValorInicial + vendas concluídas - sangrias`.

### Clientes / fornecedores / usuários
- Nome obrigatório; clientes têm limite e saldo devedor; baixa de fiado com piso 0.
- Usuários: senha BCrypt; autenticação não revela se usuário ou senha falhou;
  edição não altera senha; exclusão é lógica.
- Perfis: Administrador (tudo), Operador (somente PDV), Estoquista (somente produtos).

## 5. Riscos técnicos e medidas adotadas na versão web

| Risco | Mitigação |
|---|---|
| Alterar o app Windows | Nenhum arquivo existente foi modificado; solução em `PDVStore.Web/` |
| Schema divergente | Mesmos nomes de tabela/coluna, `decimal(18,2)` e FKs; teste de DDL SQL Server |
| API rodar migrations no banco compartilhado | Nenhuma migration/EnsureCreated no SQL Server; flag só para SQLite dev |
| Perda de atualização de estoque | Transações `Serializable` em venda/compra/ajuste/cancelamento |
| `Session` estática no servidor | `ICurrentUser` por requisição a partir do JWT |
| Autenticação desktop reutilizada | JWT novo, lendo a mesma tabela `Usuarios` e validando BCrypt |
| Preço manipulado no HTTP | Preço unitário da venda vem do cadastro do produto |
| Edição de produto mexer no estoque | DTO de atualização não possui campo de estoque |
| Segredos no repositório | Apenas `.env.example`; falha explícita sem `Jwt__Key` |
| SQL Server indisponível no ambiente | Testes com SQLite + DDL SQL Server; checklist manual documentado |
