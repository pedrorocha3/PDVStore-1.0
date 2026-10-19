# API — endpoints e exemplos

Base local: `http://localhost:5080`. Swagger: `/swagger`.
Autenticação: `Authorization: Bearer <jwt>` (exceto login e `/health`).
Erros: `application/problem+json`.

## Autenticação

### POST `/api/auth/login`

```json
{ "nome": "Admin", "senha": "admin123" }
```

`200 OK`

```json
{
  "token": "<jwt>",
  "expiraEm": "2026-10-20T04:24:52Z",
  "usuario": {
    "id": 1,
    "nome": "Admin",
    "permissao": "Administrador",
    "permissaoDescricao": "Administrador",
    "ativo": true,
    "createdAt": "2026-07-03T00:00:00Z",
    "fotoPath": null
  }
}
```

`401` para usuário/senha inválidos (mesma mensagem nos dois casos). `429` após 5
tentativas por minuto por IP.

### GET `/api/auth/me`

Retorna o `UsuarioDto` do token.

## Produtos (`/api/produtos`)

| Método | Rota | Perfil | Descrição |
|---|---|---|---|
| GET | `/` | autenticado | `busca`, `categoria`, `ativo`, `page`, `pageSize`, `ordenarPor` (nome/codigo/preco/estoque), `descendente` |
| GET | `/{id}` | autenticado | detalhe |
| GET | `/codigo-barras/{codigo}` | autenticado | somente ativos |
| POST | `/` | Admin, Estoquista | cadastro (estoque inicial permitido) |
| PUT | `/{id}` | Admin, Estoquista | cadastro **sem alterar estoque** |
| DELETE | `/{id}` | Admin, Estoquista | desativação lógica |

Exemplo de cadastro:

```json
{
  "codigoBarras": "7891000000011",
  "nome": "Café Teste",
  "preco": 12.50,
  "precoCusto": 8.00,
  "estoque": 10,
  "estoqueMinimo": 2,
  "categoria": "Bebidas",
  "descricao": "Pacote 500g"
}
```

`201 Created` → `Location: /api/produtos/{id}`

```json
{
  "id": 1, "nome": "Café Teste", "codigoBarras": "7891000000011",
  "preco": 12.5, "precoCusto": 8.0, "estoque": 10, "estoqueMinimo": 2,
  "categoria": "Bebidas", "descricao": "Pacote 500g", "ativo": true
}
```

Validações: código, nome, categoria e descrição obrigatórios; preço, custo,
estoque e mínimo ≥ 0. Erro → `400` com `detail` em português.

## Estoque (`/api/estoque`) — Admin

| Método | Rota | Descrição |
|---|---|---|
| POST | `/entradas` | `{produtoId, quantidade, motivo}` aumenta saldo |
| POST | `/saidas` | mesma entrada; reduz e bloqueia saldo negativo |
| GET | `/movimentacoes` | `inicio`, `fim`, `produtoId`, `tipo` (Entrada/Saída), paginação |
| GET | `/alertas` | ativos com `estoque <= estoqueMinimo` |

Exemplo de saída sem saldo: `409`

```json
{
  "title": "Conflito de regra de negócio",
  "status": 409,
  "detail": "Estoque insuficiente para o produto 'Cafe Teste'.",
  "instance": "/api/estoque/saidas",
  "traceId": "0HN..."
}
```

## Clientes (`/api/clientes`)

| Método | Rota | Perfil | Descrição |
|---|---|---|---|
| GET | `/` | Admin, Operador | lista (PDV precisa para fiado) |
| GET | `/{id}` | Admin, Operador | detalhe |
| POST/PUT | `/` | Admin | cadastro/edição |
| PATCH | `/{id}/status` | Admin | `{ "ativo": false }` |
| POST | `/{id}/receber-fiado` | Admin | `{ "valor": 50.00 }` |

Campos: `nome` obrigatório; `cpfCnpj`, `telefone`, `email`, `endereco`,
`limiteCredito`. `saldoDevedor` só muda por venda fiada/cancelamento/baixa.

## Fornecedores (`/api/fornecedores`) — Admin

`GET /` (busca/ativo/paginação), `GET /{id}`, `POST /`, `PUT /{id}` e
`PATCH /{id}/status`. Campos: `nome` obrigatório, `cnpj`, `telefone`, `email`.

## Caixa (`/api/caixa`)

| Método | Rota | Perfil | Descrição |
|---|---|---|---|
| GET | `/aberto` | Admin, Operador | `200` caixa ou `204` se nenhum |
| POST | `/abrir` | Admin | `{ "valorInicial": 100.00 }`; `409` se já aberto |
| POST | `/{id}/sangria` | Admin | `{ "valor": 10.00 }` |
| POST | `/{id}/fechar` | Admin | calcula `inicial + concluídas - sangrias` |
| GET | `/` | Admin | histórico paginado |

Exemplo de fechamento:

```json
{
  "id": 2, "usuarioCaixaId": 1, "usuarioNome": "Admin",
  "abertura": "2026-10-19T20:24:56Z", "fechamento": "2026-10-19T21:10:00Z",
  "valorInicial": 100.0, "valorFinal": 112.5, "sangria": 0.0, "status": "Fechado"
}
```

## Vendas (`/api/vendas`)

### POST `/api/vendas` — Admin, Operador

```json
{
  "itens": [ { "produtoId": 1, "quantidade": 2 } ],
  "desconto": 2.00,
  "formaPagamento": "PIX",
  "clienteId": null
}
```

`201 Created`

```json
{
  "id": 1,
  "usuarioCaixaId": 1,
  "usuarioNome": "Admin",
  "dataVenda": "2026-10-19T20:24:56Z",
  "valorTotal": 23.00,
  "desconto": 2.00,
  "formaPagamento": "PIX",
  "pixTxId": "C4EC1C305A6C4F6284314B005B1F22DA",
  "status": "Concluida",
  "caixaId": 2,
  "clienteId": null,
  "clienteNome": null,
  "itens": [ { "id": 1, "produtoId": 1, "produtoNome": "Cafe Teste",
               "quantidade": 2, "precoUnitario": 12.5, "subtotal": 25.0 } ]
}
```

Regras: caixa aberto; ≥1 item; forma obrigatória; fiado exige cliente ativo e
respeita limite (quando > 0); preço vem do cadastro; estoque validado antes;
baixa + movimentação; pagamento mock; tudo em transação.

| Método | Rota | Perfil | Descrição |
|---|---|---|---|
| GET | `/` | Admin | período/status/caixa, paginado |
| GET | `/{id}` | Admin | detalhe com itens |
| POST | `/{id}/cancelar` | Admin | `{ "motivo": "..." }` estorna estoque/fiado |
| GET | `/resumo` | Admin | quantidade, total e por forma no período |

## Compras (`/api/compras`) — Admin

### POST `/api/compras`

```json
{
  "fornecedorId": 1,
  "numeroNota": "NF-123",
  "itens": [ { "produtoId": 1, "quantidade": 5, "precoCusto": 7.50 } ]
}
```

`201 Created`; `valorTotal` calculado; estoque somado; `PrecoCusto` atualizado se
> 0; movimentação `Entrada` com motivo `Compra #id - NF-123`.

`GET /` (período/status/paginação), `GET /{id}`, `POST /{id}/cancelar`
(estoque estornado com piso 0).

## Relatórios (`/api/relatorios`) — Admin

| Rota | Descrição |
|---|---|
| GET `/dashboard?inicio&fim` | total, quantidade, ranking, alertas, formas, por dia, movimentos |
| GET `/itens-mais-vendidos?inicio&fim&topN` | ranking somente concluídas |
| GET `/estoque-minimo` | ativos no limite |

Datas sem offset são tratadas como UTC. O padrão é últimos 30 dias.

## Usuários (`/api/usuarios`) — Admin

| Método | Rota | Descrição |
|---|---|---|
| GET | `/?ativo&page&pageSize` | lista |
| POST | `/` | `{ "nome", "senha", "permissao" }` |
| PUT | `/{id}` | `{ "nome", "permissao", "ativo", "fotoPath" }` (não altera senha) |
| DELETE | `/{id}` | desativação lógica |
| POST | `/{id}/alterar-senha` | `{ "senhaAtual", "novaSenha" }` |

Perfis aceitos: `Administrador`, `Operador`, `Estoquista` (serializados como texto).
Senha nova exige ≥ 6 caracteres; hash BCrypt; nome único.

## Saúde e observabilidade

- `GET /health` (anônimo): status, serviço, provider e UTC. Não expõe connection string.
- Logs Serilog em `logs/pdvstore-web-AAAAMMDD.log`.
- `traceId` em todo ProblemDetails para correlação.
