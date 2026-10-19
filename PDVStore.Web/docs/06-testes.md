# Testes e validação

## Como executar

```bash
cd tests/PDVStore.Api.Tests
dotnet test
```

Ambiente usado nesta entrega: .NET SDK 8.0.426, Linux, SQLite in-memory e
`WebApplicationFactory`. Nenhum teste acessa o SQL Server do PDVStore.

## Resultado

```text
Passed! - Failed: 0, Passed: 74, Skipped: 0, Total: 74
```

Front-end:

```text
npm run build
✓ tsc -b
✓ 69 modules transformed
✓ built in 803ms
```

## Cobertura por arquivo

| Arquivo | Cenários principais |
|---|---|
| `ProdutoServiceTests` | criação válida, campos obrigatórios, preço negativo, edição **sem alterar estoque**, soft delete, busca/categoria/paginação, código só ativo |
| `EstoqueServiceTests` | entrada/saída, saldo insuficiente sem gravar, quantidade inválida, produto inexistente, histórico por tipo/período, alertas |
| `VendaServiceTests` | sem item, sem forma, sem caixa, fiado sem/inativo/acima do limite, limite 0 permite, estoque insuficiente, total negativo, venda completa, PIX TxId, saldo devedor, linhas duplicadas, falha de pagamento com rollback, cancelamento e estorno, duplo cancelamento |
| `CompraServiceTests` | total, estoque, custo, movimentação, validações, fornecedor inativo, produto inexistente, cancelamento com piso 0 |
| `CaixaServiceTests` | único aberto, conflito, sangria acumulada, valor inválido, fechamento com vendas concluídas e canceladas ignoradas, listagem |
| `ClienteServiceTests` | nome obrigatório, data UTC, edição preserva saldo/status, ativar/desativar, baixa de fiado sem negativo, filtros |
| `UsuarioServiceTests` | login seed, senha errada, inativo, hash BCrypt, nome duplicado, senha curta, edição sem senha, troca exige senha atual, soft delete |
| `ApiIntegrationTests` | health anônimo, Swagger, 401 sem token, login inválido, criação/listagem, 403 do Operador, 400 de validação, fluxo venda+estoque+fechamento pela API |
| `ModeloSqlServerTests` | 10 tabelas, `decimal(18,2)`, colunas-chave, ausência de `__EFMigrationsHistory` e de nomes antigos |

## Cenários de negócio verificados

- Venda não grava nada quando falta caixa, estoque, cliente ou pagamento.
- Falha do pagamento faz rollback de venda, itens, estoque e movimentação.
- Cancelamento devolve estoque e saldo devedor com piso 0.
- Fechamento de caixa ignora vendas canceladas.
- Edição de produto não modifica estoque.
- Operador não gerencia produtos/estoque, mas consulta catálogo e clientes.
- Senhas nunca são persistidas em texto puro.
- DDL SQL Server mantém os nomes e a precisão do schema original.

## O que NÃO foi possível verificar

- Execução contra uma instância real de SQL Server/LocalDB (indisponível no ambiente).
  Mitigado por `ModeloSqlServerTests` + testes SQLite; o checklist manual está em
  `docs/03-banco-de-dados.md`.
- Teste E2E automatizado de navegador (sem Playwright/Chromium no ambiente).
  O front-end foi validado por `tsc`, build de produção, servidor Vite e proxy `/health`.
- Concorrência real com múltiplas conexões SQL Server; as transações foram
  exercitadas funcionalmente, mas não sob carga.
