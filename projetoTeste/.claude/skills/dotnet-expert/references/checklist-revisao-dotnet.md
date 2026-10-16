# Checklist de Revisão — C# / .NET

## Camadas
- [ ] `Domain` sem referência a EF Core, ASP.NET, configuração ou atributo de serialização.
- [ ] Nenhuma regra de negócio dentro de controller ou endpoint.
- [ ] Entidade de domínio não é devolvida direto na resposta HTTP (existe DTO).
- [ ] Interface nova tem mais de uma implementação plausível, ou existe motivo declarado.

## DI e tempo de vida
- [ ] Nenhum `Singleton` recebendo serviço `Scoped` no construtor.
- [ ] `DbContext` não é compartilhado entre operações paralelas.
- [ ] Serviço em background abre escopo próprio por ciclo.

## async
- [ ] Nenhum `.Result` ou `.Wait()` em caminho de requisição.
- [ ] Nenhum `async void` fora de event handler.
- [ ] `CancellationToken` recebido no endpoint e propagado até a chamada de banco/HTTP.

## EF Core
- [ ] `AsNoTracking()` nas consultas de leitura.
- [ ] Consulta de listagem projeta para DTO em vez de materializar a entidade inteira.
- [ ] Nenhum acesso a navegação dentro de loop sem `Include` ou projeção (N+1).
- [ ] Filtro e paginação acontecem antes de materializar (`ToList` por último).
- [ ] `SaveChangesAsync` fora do loop.

## Erro e validação
- [ ] Erro tratado num ponto central, devolvendo `ProblemDetails`.
- [ ] Mensagem de exceção interna não chega ao cliente.
- [ ] Entrada validada na borda; invariante garantida no domínio.
- [ ] Regra de negócio esperada não usa exceção como fluxo de controle.

## Geral
- [ ] `HttpClient` obtido via `IHttpClientFactory`.
- [ ] Configuração acessada por classe tipada, não por string aninhada espalhada.
- [ ] Nenhum segredo em `appsettings.json` versionado.
- [ ] Nullable reference types respeitado (sem `!` sem justificativa).
