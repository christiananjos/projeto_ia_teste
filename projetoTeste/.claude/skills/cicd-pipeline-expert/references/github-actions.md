# Referência GitHub Actions

## Workflow .NET básico (.github/workflows/build.yml)

```yaml
name: build-and-test

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.x'

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/packages.lock.json') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage"

      - name: Publish
        run: dotnet publish --configuration Release --output ./publish

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: drop
          path: ./publish
```

## Job de deploy com proteção de environment

```yaml
  deploy-staging:
    needs: build
    runs-on: ubuntu-latest
    environment: staging
    steps:
      - uses: actions/download-artifact@v4
        with:
          name: drop
          path: ./publish

      - name: Deploy to Azure Web App
        uses: azure/webapps-deploy@v3
        with:
          app-name: '<nome-do-app>-staging'
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE_STAGING }}
          package: ./publish
```

## Environments
- `Settings > Environments` permite exigir revisores antes de um job daquele ambiente rodar — mesmo conceito dos checks de environment do Azure DevOps.
- Guarde secrets escopados por ambiente em cada environment, não no nível do repo inteiro, pra manter credenciais de staging/prod separadas.

## Workflows reutilizáveis vs composite actions
- **Workflow reutilizável** (trigger `workflow_call`): melhor quando você quer compartilhar um grafo de jobs inteiro (ex: a mesma sequência de build+test+deploy) entre vários repos.
- **Composite action**: melhor pra compartilhar um punhado de steps (ex: "configurar o runtime + restore + cache") pra inserir em jobs/workflows diferentes.

## Actions comuns pra .NET
- `actions/setup-dotnet@v4` — instala o SDK
- `actions/cache@v4` — cacheia pacotes NuGet
- `actions/upload-artifact@v4` / `actions/download-artifact@v4` — passa output de build entre jobs
- `azure/webapps-deploy@v3` — deploy pro Azure App Service
- `azure/login@v2` — login OIDC no Azure (preferível a publish profiles/secrets de longa duração pra produção)

## Proteção de branch
`Settings > Branches > Branch protection rules` pra `main`:
- Exigir pull request antes do merge, exigir aprovações
- Exigir que os status checks passem (selecione o workflow de build) antes do merge
- Exigir que branches estejam atualizadas antes do merge
- Opcionalmente exigir histórico linear pra um log mais limpo
