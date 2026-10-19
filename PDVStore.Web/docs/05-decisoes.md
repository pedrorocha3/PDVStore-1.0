# Decisões arquiteturais (ADR resumido)

## ADR-01 — Não alterar o projeto Windows

**Decisão:** toda a solução vive em `PDVStore.Web/`; nenhum arquivo do WinForms,
`.csproj`, `.slnx` ou migration original foi modificado.

**Motivo:** requisito principal e garantia de que o desktop continua funcionando.

**Consequência:** as regras foram reimplementadas na API, com testes de equivalência.

## ADR-02 — Compartilhar o banco, não o código

**Decisão:** a API referencia o mesmo SQL Server/`PDV_StoreDB`, mas não referencia o
projeto WinForms (que é `net8.0-windows` e não roda no servidor Linux).

**Motivo:** evitar acoplamento, dependência de WinForms e risco de quebrar o desktop.

## ADR-03 — Nenhuma migration na API

**Decisão:** a API nunca chama `Migrate`/`EnsureCreated` no SQL Server. A flag de
criação automática só funciona com provider SQLite em Development.

**Motivo:** o schema é propriedade do PDVStore; alterações exigem autorização.

## ADR-04 — SQLite apenas para desenvolvimento e testes

**Decisão:** `appsettings.Development.json` usa SQLite; produção usa SQL Server.

**Motivo:** permitir executar/validar a solução em ambiente sem SQL Server.

**Consequência:** comportamento específico de SQL Server (collation, deadlocks)
precisa do checklist manual documentado.

## ADR-05 — DTOs e mapeamento explícito

**Decisão:** nenhuma entidade é retornada diretamente; contratos ficam em
`Contracts` e o mapeamento é manual em `Mappers`/serviços.

**Motivo:** evitar ciclos (`Venda -> Itens -> Venda`), vazar `SenhaHash` e permitir
evolução independente do schema. AutoMapper seria complexidade sem ganho.

## ADR-06 — Preço da venda vem do cadastro

**Decisão:** `POST /api/vendas` aceita produto e quantidade; o preço unitário é lido
do banco.

**Motivo:** impedir manipulação de preço pelo cliente HTTP. O PDV desktop também
usa o preço carregado do cadastro.

## ADR-07 — Edição de produto não altera estoque

**Decisão:** `AtualizarProdutoRequest` não possui `Estoque`.

**Motivo:** o briefing proíbe que uma edição simples de produto altere estoque.
O desktop permitia editar o campo na tela; a API restringe a mudança a
movimentações, vendas e compras. Essa divergência é intencional e testada.

## ADR-08 — JWT lendo a mesma tabela de usuários

**Decisão:** login valida BCrypt na tabela `Usuarios` e emite JWT com `sub`, `name`
e `role`. O `Session.CurrentUser` estático não é usado no servidor.

**Motivo:** `Session` é estado global e inseguro em aplicações web concorrentes.

**Consequência:** o desktop continua com login próprio; ambos compartilham os
mesmos usuários/senhas.

## ADR-09 — Transações serializáveis

**Decisão:** venda, compra, cancelamentos, ajustes e abertura de caixa usam
`IsolationLevel.Serializable`.

**Motivo:** evitar lost update/estoque negativo sem adicionar `rowversion` ao schema.

**Limitação:** pode gerar deadlock sob carga; repetir a operação é seguro.

## ADR-10 — Soft delete

**Decisão:** produtos, clientes, fornecedores e usuários são desativados, nunca
apagados fisicamente.

**Motivo:** preservar vendas, compras e movimentações históricas, como no desktop.

## ADR-11 — React + TypeScript

**Decisão:** usar TypeScript, embora o briefing cite “React + JavaScript”.

**Motivo:** o item específico de front-end pede TypeScript; os contratos da API
ficam verificados em tempo de compilação.

## ADR-12 — Endurecimentos de segurança

Sem alterar conceitos de negócio, foram adicionados:

- senha mínima de 6 caracteres (o desktop não validava tamanho);
- nome de usuário único na API (o desktop assume unicidade por convenção);
- fornecedor inativo não pode ser usado em nova compra;
- buscas case-insensitive para reproduzir a collation padrão do SQL Server também
  no SQLite;
- rate limit no login e limite global por IP;
- CORS restrito às origens configuradas.

## ADR-13 — Sem camada Repository genérica

**Decisão:** serviços usam `PdvStoreDbContext` diretamente.

**Motivo:** não há necessidade concreta de troca de ORM; uma interface genérica
adicionaria indireção sem benefício neste porte.

## Fora de escopo (primeiro ciclo)

- Exportação PDF/Excel do desktop.
- Gateway de pagamento real (mock preservado).
- Upload/gestão de foto de usuário (campo `FotoPath` preservado).
- Testes E2E de navegador.
