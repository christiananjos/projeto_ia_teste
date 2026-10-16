---
paths:
  - "src/**/*.cs"
---

# Convenções .NET — Clean Architecture

- Domain não depende de nada; Application depende só de Domain; Infrastructure e API dependem de Application.
- **SOLID** em todo o código.
- **Repository Pattern** para acesso a dados — interfaces no Domain, implementação na Infrastructure.
- **DTOs** — nunca exponha entidades de domínio diretamente na API.
- Nomenclatura em português para domínio de negócio, em inglês para termos técnicos (padrão deste projeto).
- Código pronto para produção, sem placeholders ou `TODO`.
