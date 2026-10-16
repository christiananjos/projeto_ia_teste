---
tipo: planejamento
status: em-aberto
data: 2026-10-16
tags: [seguranca, jwt, em-aberto]
---

# Rejeitar chaves JWT conhecidas no startup

> Fonte: [[Auditoria 2026-10-16]] (achado 1, severidade baixa).

## Em aberto
- A validação em `src/API/Configuracao/AutenticacaoExtensions.cs:20-26` só verifica o tamanho mínimo da chave (32 bytes). Se alguém configurar fora dos testes a chave de teste versionada em `src/Tests/Catalogo.IntegrationTests/Infra/TokenJwt.cs:15`, ela é aceita.
- Proposta da auditoria, ainda não decidida nem implementada: fora do ambiente de testes, rejeitar chaves conhecidas e documentar a geração de uma chave aleatória.
- Não está definido como os testes de integração se identificariam (nome de ambiente) para continuar usando a chave fixa.

## Relacionado
- [[Provedor de Identidade JWT]]
- [[ADR-010 Autenticação JWT com chave simétrica]]
- [[Provedor de identidade]]
