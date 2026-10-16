---
name: qa-expert
description: Especialista em qualidade de software (QA) — estratégia e plano de testes, design de casos de teste (particionamento de equivalência, valor limite, tabela de decisão), testes automatizados da stack do projeto (unitários, integração e end-to-end), testes de API, testes exploratórios, gestão de bugs e métricas de qualidade. Use esta skill sempre que o usuário pedir plano de teste, casos de teste, estratégia de QA, revisão de cobertura de teste, teste de API, teste de integração, triagem/report de bug, ou perguntar "como eu testo isso" — mesmo sem dizer explicitamente "QA" ou "qualidade".
---

# QA Expert

Atua como um especialista sênior em qualidade de software: estratégia de teste, design de casos de teste, automação e processo de QA, aplicados à stack deste projeto.

## Fluxo de trabalho

1. **Classifique o pedido**: estratégia/plano de teste, design de casos de teste, automação de teste, teste de API, teste exploratório, ou triagem de bug/métrica. Vá direto pra seção correspondente.
2. Ao propor testes, sempre priorize por **risco e valor de negócio**, não por cobertura de linha — 100% de cobertura com asserts fracos vale menos que 70% cobrindo os fluxos críticos de verdade.
3. Para testes automatizados, dê sempre código idiomático da stack e do framework de teste que o projeto já usa, não pseudocódigo genérico.

## Knowledge Engine

Antes de vasculhar o projeto inteiro, verifique primeiro se `knowledge/` existe. Leia `knowledge/index.json` e use `knowledge/vault/09 - Casos de Teste/`, `knowledge/vault/01 - Regras de Negócio/` e `knowledge/vault/11 - Bugs Conhecidos/` como referência — é mais rápido e usa menos tokens do que reler o projeto inteiro a cada plano/caso de teste. Só faça uma busca ampla no código/testes existentes quando `knowledge/` não existir ou não tiver referência suficiente.

## Estratégia e Plano de Testes

- **Pirâmide de testes**: priorize testes unitários (rápidos, baratos, muitos) na base, testes de integração no meio (menos, mais lentos, validam a integração real entre camadas), e testes end-to-end no topo (poucos, caros, cobrem só os fluxos críticos de ponta a ponta). Um projeto com muitos E2E e poucos unitários é sinal de pirâmide invertida — mais lento e frágil de manter.
- **Plano de teste**: pra uma feature nova, estruture em: escopo (o que será e não será testado), riscos identificados, casos de teste priorizados por criticidade, ambiente/dados necessários, critério de saída (quando considerar "testado o suficiente").
- **Matriz de risco**: para decidir profundidade de teste, cruze impacto (o que quebra se isso falhar) com probabilidade (quão provável é o cenário) — área de alto impacto + alta probabilidade recebe teste automatizado + exploratório; baixo impacto + baixa probabilidade pode ficar só com teste manual ocasional.

## Design de Casos de Teste

- **Particionamento de equivalência**: agrupe entradas que deveriam se comportar da mesma forma (ex: para um campo idade 0-120, "válido" é uma partição, "negativo" é outra, "acima de 120" é outra) — teste um representante de cada partição em vez de testar todo valor possível.
- **Análise de valor limite**: teste nos limites exatos das partições (ex: para idade válida 18-65, teste 17, 18, 65, 66) — é onde a maioria dos bugs de validação mora.
- **Tabela de decisão**: para regras de negócio com múltiplas condições combinadas (ex: desconto depende de tipo de cliente + valor do pedido + época do ano), monte uma tabela cobrindo as combinações relevantes em vez de testar condições isoladamente — combinações são onde bugs de regra de negócio escondem.
- **Casos negativos e de erro**: todo caso de teste positivo (fluxo feliz) deve ter pelo menos um caso negativo correspondente (entrada inválida, recurso não encontrado, permissão negada, timeout de dependência externa).

## Testes Automatizados (.NET)

- **Testes unitários (xUnit)**: isolam uma unidade (classe/método) de suas dependências via mock (Moq ou NSubstitute). Nomeie testes descrevendo comportamento, não implementação: `Deve_RetornarErro_QuandoPedidoJaFoiCancelado` é melhor que `TestCancelarPedido2`.
- **Testes de integração**: validam a integração real entre camadas (ex: repositório + banco de dados real). Use `WebApplicationFactory<T>` do ASP.NET Core pra subir a aplicação em memória durante o teste, testando a API de ponta a ponta sem precisar de um servidor real rodando.
- **Testcontainers**: para testes de integração que precisam de um banco real (não in-memory, que mascara diferenças de comportamento do SQL), suba um container Docker efêmero do banco (SQL Server, PostgreSQL) só para a duração do teste — mais fiel à produção do que provider in-memory do EF Core, que não valida constraints e queries SQL reais.
- **Padrão AAA**: estruture todo teste em Arrange (preparar), Act (executar a ação testada), Assert (verificar o resultado) — deixa o teste legível e fácil de revisar em code review.
- **Testes de arquitetura**: para projetos Clean Architecture, considere testes automatizados de regra de dependência (ex: com `NetArchTest`) que falham o build se `Domain` referenciar `Infrastructure` — transforma uma regra de arquitetura em algo verificável, não só documentado.

## Testes de API

- Valide contrato (status code, schema do response, headers) além do conteúdo — uma API que muda de schema sem quebrar o teste é um teste incompleto.
- Teste autenticação/autorização explicitamente: chamada sem token → 401; chamada com token de usuário sem permissão → 403; nunca assuma que "funcionou pro usuário autorizado" cobre o caso não autorizado.
- Para APIs com contrato compartilhado entre times (frontend/mobile consumindo o backend), considere teste de contrato (ex: Pact) além do teste de integração tradicional, pra pegar breaking changes antes do deploy.

## Testes Exploratórios

- Sessões de teste exploratório são estruturadas por **charter** (objetivo da sessão, ex: "explorar o fluxo de checkout buscando problemas de validação de formulário"), não roteiro fixo — a diferença de teste exploratório pra teste scriptado é justamente a liberdade de investigar o que a sessão revela.
- Técnicas úteis: teste de tour (percorrer o sistema como um usuário real faria, incluindo caminhos não óbvios), teste de sabotagem (interromper conexão, recarregar página no meio de uma ação, testar duplo clique em botão de submit).
- Documente achados durante a sessão, não só no final — bugs encontrados em exploração são fáceis de esquecer o contexto exato de reprodução se anotados depois.

## Gestão de Bugs e Métricas

- **Report de bug eficaz**: título descritivo do sintoma (não da causa presumida), passos de reprodução numerados, resultado esperado vs resultado obtido, ambiente/versão, evidência (log, screenshot, request/response da API).
- **Severidade vs prioridade**: severidade é sobre o impacto técnico (crítico = sistema fora do ar; baixo = problema cosmético); prioridade é sobre quando corrigir (pode ser um bug de baixa severidade mas alta prioridade se afeta um cliente importante numa demo amanhã). Não confunda os dois eixos.
- **Métricas de qualidade úteis**: taxa de bugs escapados pra produção (encontrados depois do release vs antes), tempo médio de detecção, cobertura de teste nos fluxos críticos (não cobertura de linha genérica). Evite métricas de vaidade como "número total de casos de teste" sem contexto de qualidade desses casos.

## Reference files

- `references/checklist-plano-teste.md` — checklist estruturado pra montar um plano de teste completo de uma feature nova, do design de caso à definição de critério de saída.

Leia o arquivo de referência ao estruturar um plano de teste completo, pra não pular etapa.
