# Publicação em repositório separado

## Situação

O repositório **não foi criado/publicado** nesta execução porque a credencial do
ambiente (`cline-cloud[bot]`) não possui a permissão `createRepository`:

```text
GraphQL: Resource not accessible by integration (createRepository)
```

A solução está completa e versionada na branch `cline/d5nttjzk` do repositório
original, dentro da pasta `PDVStore.Web/`. Nenhum arquivo do app Windows foi alterado.

## Gerar uma história isolada (somente a solução web)

O comando abaixo cria a branch `pdvstore-web` contendo apenas o conteúdo de
`PDVStore.Web/` na raiz (sem o projeto WinForms):

```bash
git subtree split --prefix=PDVStore.Web -b pdvstore-web
```

Verifique:

```bash
git ls-tree -r --name-only pdvstore-web | head
git log --oneline pdvstore-web
```

## Criar e publicar (com uma conta autorizada)

1. Crie o repositório **PDVStore.Web** no GitHub (vazio, sem README).
2. Envie a branch isolada como `main`:

```bash
git push https://github.com/<sua-conta>/PDVStore.Web.git pdvstore-web:main
```

3. Confirme que o repositório contém apenas `backend/`, `frontend/`, `tests/`,
   `docs/`, `.github/`, `README.md`, `PDVStore.Web.sln`, `.gitignore`,
   `.editorconfig` e `global.json`.

Com GitHub CLI e permissão de criação:

```bash
gh repo create <sua-conta>/PDVStore.Web --private --source=. --remote=web
# ou, a partir da branch isolada:
gh repo create <sua-conta>/PDVStore.Web --private --push --source=.
```

## Checklist pós-publicação

- [ ] CI verde (backend .NET 8 e front-end)
- [ ] Nenhum `.env`, `.db`, log, `bin/`, `obj/` ou `node_modules/` versionado
- [ ] `Jwt__Key` e connection string configuradas como secrets/variáveis do ambiente
- [ ] Branch `main` protegida e PR obrigatório
- [ ] Tag `v1.0.0` após homologação contra o banco SQL Server do PDVStore
