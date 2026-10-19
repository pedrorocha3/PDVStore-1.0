# Banco de dados

## Estratégia

A API compartilha **o mesmo banco e o mesmo schema** do PDVStore desktop
(SQL Server, `PDV_StoreDB`). Não há banco paralelo em produção.

- `Database:Provider = SqlServer` é o padrão.
- Nenhuma migration é executada pela API.
- `EnsureCreated` só é permitido quando o provider é SQLite **e** o ambiente é
  Development (`Database:EnsureCreatedForDevelopment=true`). Com SQL Server a API
  lança exceção se essa flag for ativada.
- O `DbContext` usa Fluent API equivalente ao `OnModelCreating` original e
  `ToTable` explícito para os 10 nomes existentes.

## Mapeamento

| Tabela | Entidade | Colunas principais |
|---|---|---|
| `Produtos` | `Produto` | Nome, CodigoBarras, Preco, PrecoCusto, Estoque, EstoqueMinimo, Categoria, Descricao, Ativo |
| `Clientes` | `Cliente` | Nome, CpfCnpj, Telefone, Email, Endereco, CadastradoEm, Ativo, LimiteCredito, SaldoDevedor |
| `Fornecedores` | `Fornecedor` | Nome, Cnpj, Telefone, Email, Ativo |
| `Usuarios` | `UsuarioCaixa` | Nome, SenhaHash, CreatedAt, FotoPath, Ativo, Permissao |
| `Caixas` | `Caixa` | UsuarioCaixaId, Abertura, Fechamento, ValorInicial, ValorFinal, Sangria, Status |
| `Vendas` | `Venda` | UsuarioCaixaId, DataVenda, ValorTotal, Desconto, FormaPagamento, PixTxId, Status, CaixaId, ClienteId |
| `ItensVendas` | `ItemVenda` | VendaId, ProdutoId, Quantidade, PrecoUnitario |
| `Compras` | `Compra` | FornecedorId, UsuarioCaixaId, DataCompra, NumeroNota, ValorTotal, Status |
| `ItensCompras` | `ItemCompra` | CompraId, ProdutoId, Quantidade, PrecoCusto |
| `MovimentacoesEstoque` | `MovimentacaoEstoque` | ProdutoId, Tipo, Quantidade, PrecoUnitario, DataMovimentacao, UsuarioId, Motivo, ReferenciaVendaId |

Precisão: `decimal(18,2)` em todos os valores monetários, idêntica ao snapshot de
migrations do projeto original. `Subtotal` e `NomeProduto` são `[NotMapped]`.

## Relacionamentos e delete

- `ItensVendas`/`ItensCompras` → pai em **Cascade**.
- `Venda.ClienteId` → **SetNull**.
- Demais FKs → **Restrict** (preserva histórico).
- Seeds de modelo: caixa 1 fechado e usuário Admin 1 (hash BCrypt original).

## Concorrência

O schema não possui `rowversion`. Para não alterá-lo:

1. operações críticas abrem transação `Serializable`;
2. o estoque é validado dentro da transação antes da baixa;
3. cancelamentos usam piso 0 (`Math.Max`).

Limitação: sob altíssima concorrência, transações serializáveis podem gerar
deadlock/timeout; nesse caso o cliente recebe erro e pode repetir. A solução
definitiva seria `rowversion` + tratamento de conflito, com migration autorizada
no projeto Windows.

## Checklist antes de apontar para produção

1. Fazer backup do banco do PDVStore.
2. Confirmar que o schema está atualizado pelas migrations do desktop.
3. Configurar `ConnectionStrings__DefaultConnection` com usuário de menor privilégio
   (SELECT/INSERT/UPDATE nas tabelas; sem DDL).
4. Iniciar a API com `Database__Provider=SqlServer` e conferir `/health`.
5. Executar login, abrir caixa, uma venda de teste e cancelamento em ambiente de
   homologação; verificar `MovimentacoesEstoque`.
6. Monitorar deadlocks e tempo das transações.

## Modo local (SQLite)

`appsettings.Development.json` aponta para `Data Source=pdvstore-dev.db` e
`EnsureCreatedForDevelopment=true`. O arquivo é ignorado pelo Git. Esse modo serve
para demonstrar a aplicação em Linux/macOS e para os testes; **não** é uma segunda
estratégia de produção. O schema é gerado a partir do mesmo modelo.

## O que não foi feito

- Nenhuma migration foi criada na API.
- Nenhuma coluna, índice ou tabela do PDVStore foi alterada.
- Não foi usado outro banco em produção “por conveniência”.
