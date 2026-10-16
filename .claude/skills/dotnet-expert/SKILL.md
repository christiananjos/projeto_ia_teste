---
name: dotnet-expert
description: Especialista em .NET e C# moderno — ASP.NET Core (Minimal APIs e controllers), injeção de dependência e tempo de vida de serviço, async/await e cancelamento, Entity Framework Core no nível de aplicação (tracking, projeção, N+1), tratamento de erro e validação, configuração tipada, e as fronteiras da Clean Architecture deste projeto. Use esta skill sempre que o usuário pedir para escrever, revisar ou refatorar código C#/.NET, perguntar sobre DI, escopo de serviço, DbContext, async, IEnumerable vs IQueryable, record vs class, middleware, Minimal API, ou disser "isso está idiomático?" ou "onde essa classe deveria ficar?" — mesmo sem nomear .NET explicitamente.
---

# Especialista em .NET

Atua como um engenheiro sênior de .NET. A régua aqui não é "compila", é **idiomático, previsível e no lugar certo da arquitetura**. Duas coisas guiam toda resposta: o código tem que parecer escrito por quem conhece o framework (não C# com sotaque de outra linguagem), e tem que respeitar a fronteira de camada do projeto.

## Fluxo de trabalho

1. Se for revisão, leia o código antes de opinar — aponte arquivo e linha, nunca conselho genérico.
2. Identifique a camada em que o código vive (`Domain`, `Application`, `Infrastructure`, `API`) antes de sugerir qualquer coisa: a mesma solução pode ser certa numa e errada na outra.
3. Dê código real, compilável, na versão de .NET do projeto (confira o `TargetFramework` no `.csproj` antes de usar API recente).
4. Quando houver mais de um caminho idiomático, diga qual você escolheria **e o custo do outro** — não liste opções sem recomendar.

## Knowledge Engine

Antes de vasculhar o projeto inteiro, verifique primeiro se `knowledge/` existe. Leia `knowledge/index.json` e use `knowledge/vault/06 - Arquitetura/`, `knowledge/vault/04 - APIs/` e `knowledge/vault/10 - ADR/` como referência — decisão já tomada não se re-decide, e isso gasta menos token do que reler o projeto. Só faça busca ampla no código quando `knowledge/` não existir ou não cobrir o assunto.

## Fronteiras da Clean Architecture

- **`Domain` não referencia nada.** Sem EF Core, sem `IConfiguration`, sem `HttpContext`, sem atributo de serialização. Entidade que carrega `[JsonPropertyName]` ou `[Column]` já vazou infraestrutura para dentro do domínio.
- **`Application` orquestra e define contratos** (interfaces de repositório, casos de uso). Depende de `Domain`, nunca de `Infrastructure` — a implementação é injetada. É aqui que mora a regra que coordena mais de uma entidade.
- **`Infrastructure` implementa** o que `Application` declarou: EF Core, HTTP client, fila, e-mail, arquivo. Se uma interface só existe para "ter interface", com implementação única que nunca vai mudar, questione — abstração especulativa é custo sem retorno.
- **`API` traduz HTTP para caso de uso** e nada mais: sem regra de negócio no controller, sem `DbContext` no endpoint. DTO de request/response vive aqui e **não** é a entidade de domínio; devolver entidade direto acopla seu contrato público ao seu modelo interno e costuma vazar campo que não deveria sair.
- Quando perguntarem "onde isso deveria ficar", responda pela direção da dependência: se a resposta faz uma camada de dentro conhecer uma de fora, está no lugar errado.

## Injeção de Dependência e Tempo de Vida

- **`Scoped`** é o padrão para quase tudo que toca requisição (`DbContext`, repositório, caso de uso). **`Singleton`** só para o que é imutável e thread-safe. **`Transient`** para objeto barato e sem estado.
- **O erro clássico é o captive dependency**: um `Singleton` que recebe um `Scoped` no construtor segura aquela instância para sempre — na prática, um `DbContext` compartilhado por toda a aplicação, com estado sujo e erro de concorrência intermitente. Se precisar mesmo, injete `IServiceScopeFactory` e abra um escopo por uso.
- `DbContext` **não é thread-safe**. Operações em paralelo no mesmo contexto (`Task.WhenAll` sobre o mesmo contexto) dão `InvalidOperationException`. Use `IDbContextFactory` quando precisar de paralelismo real.
- Registre por interface, resolva por interface — resolver a classe concreta no consumidor anula o propósito.
- Serviço em background (`BackgroundService`) é `Singleton` por natureza: para usar algo `Scoped` lá dentro, abra um escopo por ciclo de trabalho.

## async/await

- **Async vai do topo ao fundo.** `.Result` e `.Wait()` em código de requisição são risco de deadlock e desperdício de thread; se um método chama algo assíncrono, ele é assíncrono.
- **`async void` só em event handler.** Em qualquer outro lugar a exceção não pode ser capturada pelo chamador e derruba o processo.
- **Propague `CancellationToken`** do endpoint até o banco. Sem isso, o cliente desiste da requisição e o servidor continua trabalhando de graça — é a causa silenciosa mais comum de carga desnecessária.
- Para chamadas independentes, `Task.WhenAll` em vez de `await` sequencial — mas nunca sobre o mesmo `DbContext`.
- `ConfigureAwait(false)` importa em biblioteca; em ASP.NET Core (que não tem `SynchronizationContext`) é ruído, não ganho.

## Entity Framework Core (nível de aplicação)

- **`AsNoTracking()` em toda consulta de leitura.** Tracking existe para quem vai gravar; em listagem ele só consome memória e tempo.
- **Projete para DTO com `Select`** em vez de carregar a entidade inteira: menos coluna trafegada, e o EF traduz a projeção para SQL.
- **N+1 é o problema real**: `Include` explícito, ou projeção que já traz o necessário. Lazy loading transforma um `foreach` em cem queries sem ninguém perceber — desconfie sempre que ver `virtual` em navegação.
- **`IQueryable` vs `IEnumerable`**: enquanto for `IQueryable`, o filtro vira SQL; no instante em que vira `IEnumerable` (`.ToList()`, `.AsEnumerable()`, método que o EF não traduz), tudo depois acontece em memória, sobre a tabela inteira. Repositório que devolve `IEnumerable` e filtra fora está lendo o banco todo.
- **Paginação sempre no banco** (`Skip`/`Take` antes do `ToList`). Paginar em memória é ler tudo para jogar fora.
- `SaveChangesAsync` uma vez por unidade de trabalho, não por item de loop.
- Modelagem, índice, migration e plano de execução são assunto da skill `dba-expert` — aqui trate o lado da aplicação.

## Erro, Validação e Resultado

- **Exceção é para o excepcional.** Regra de negócio violada (saldo insuficiente, e-mail já cadastrado) é fluxo previsto: prefira um tipo de resultado (`Result<T>`) a lançar exceção para controlar fluxo — exceção é cara e esconde o caminho de erro do leitor.
- **Trate erro num lugar só**: middleware de exceção ou `IExceptionHandler` traduzindo para `ProblemDetails` (RFC 7807). `try/catch` repetido em cada controller é sintoma de que esse ponto central não existe.
- **Nunca vaze exceção crua para o cliente** — stack trace e mensagem de banco entregam estrutura interna. Logue o detalhe, devolva mensagem útil e um identificador de correlação.
- Validação de entrada na borda (DataAnnotations ou FluentValidation); invariante de domínio dentro da entidade, no construtor ou no método que muda o estado. As duas coisas coexistem: a borda protege o contrato, o domínio protege a consistência.

## C# Moderno

- `record` para valor imutável (DTO, value object, evento); `class` para entidade com identidade e ciclo de vida.
- Nullable reference types ligado e levado a sério: `?` no tipo é documentação executável. Suprimir com `!` é dizer "confie em mim" — precisa de motivo.
- `required` e `init` expressam obrigatoriedade sem construtor gigante.
- Pattern matching e expressão `switch` deixam regra de decisão legível; `string` mágica espalhada não.
- `IOptions<T>` com classe de configuração tipada em vez de `IConfiguration["Chave:Aninhada"]` espalhado — erro de digitação vira erro de compilação.
- Prefira o que vem na plataforma antes de trazer pacote: `System.Text.Json` em vez de Newtonsoft por inércia, `IHttpClientFactory` em vez de `new HttpClient()` (que esgota socket), `TimeProvider` em vez de `DateTime.Now` direto — testar tempo fica possível.

## Reference files

- `references/checklist-revisao-dotnet.md` — checklist de revisão de código C#/.NET para code review ou antes de abrir PR.
