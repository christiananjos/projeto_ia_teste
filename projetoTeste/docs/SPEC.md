# API de Produtos (CRUD) - Especificação

## 📋 Visão Geral

API REST para gerenciamento de produtos e suas categorias: cadastro, consulta, atualização e remoção.
Serve como CRUD de referência do projeto, seguindo Clean Architecture.

**Stack:** .NET 10 (somente backend)

**Escopo:** Produto e Categoria. Fora de escopo: estoque, preços por período, imagens, carrinho/pedidos.

---

## 🎯 Requisitos Funcionais

### REQ-001: Gerenciar categorias
- Criar, listar, obter, atualizar e remover categorias
- Nome único (sem diferenciar maiúsculas/minúsculas)
- Categoria com produtos vinculados não pode ser removida

### REQ-002: Gerenciar produtos
- Criar produto vinculado a uma categoria existente e ativa
- Obter produto por id (incluindo nome da categoria)
- Atualizar todos os campos editáveis do produto
- Remover produto (exclusão lógica)

### REQ-003: Listar produtos
- Paginação (`page`, `pageSize`) com padrão 1 e 20, máximo 100
- Filtros opcionais: `categoriaId`, `nome` (contém), `ativo`
- Ordenação por `nome` ou `preco` (`asc`/`desc`), padrão `nome asc`

### REQ-004: Ativar e inativar produto
- Produto inativo não aparece na listagem por padrão (`ativo=true` implícito)
- Inativar/reativar via endpoint dedicado

---

## 🏗️ Regras de Negócio

### BR-001: Nome do produto
Obrigatório, de 3 a 150 caracteres, sem espaços nas pontas. Único dentro da mesma categoria
(comparação sem diferenciar maiúsculas/minúsculas) entre produtos não removidos.

### BR-002: Preço
Obrigatório, decimal maior que zero, com no máximo 2 casas decimais.

### BR-003: SKU
Obrigatório, de 3 a 40 caracteres alfanuméricos e hífen, único globalmente. Gerado pelo cliente e
imutável depois da criação (o `PUT` não o altera).

### BR-004: Categoria válida
Produto só pode ser criado ou movido para categoria que exista e esteja ativa; caso contrário, 422.

### BR-005: Exclusão lógica
`DELETE` de produto marca `DataRemocao`; o registro some de todas as consultas e libera nome e SKU
para reuso. Remover um produto já removido retorna 404.

### BR-006: Remoção de categoria
Categoria com ao menos um produto não removido não pode ser excluída (409). Categoria sem produtos é
removida fisicamente.

### BR-007: Datas
`DataCriacao` e `DataAtualizacao` são definidas pelo servidor em UTC; o cliente não as envia.

---

## 🗄️ Modelo de Dados

### Categoria
- Id (UUID, PK)
- Nome (string, required, 3–100, único)
- Descricao (string, nullable, até 500)
- Ativa (bool, padrão true)
- DataCriacao (DateTime UTC)
- DataAtualizacao (DateTime UTC)

### Produto
- Id (UUID, PK)
- CategoriaId (UUID, FK → Categoria, required)
- Nome (string, required, 3–150)
- Sku (string, required, 3–40, único)
- Descricao (string, nullable, até 1000)
- Preco (decimal(18,2), required, > 0)
- Ativo (bool, padrão true)
- DataCriacao (DateTime UTC)
- DataAtualizacao (DateTime UTC)
- DataRemocao (DateTime UTC, nullable — exclusão lógica)

**Índices:** `Categoria(Nome)` único; `Produto(Sku)` único filtrado por `DataRemocao IS NULL`;
`Produto(CategoriaId, Nome)` único filtrado por `DataRemocao IS NULL`.

**Persistência:** Entity Framework Core com SQL Server (connection string via configuração/secrets).

---

## 📡 Endpoints Principais

Base: `/api/v1`. Erros no formato `ProblemDetails` (RFC 9457).

### Categorias
- `GET /api/v1/categorias` - Listar (paginado)
- `GET /api/v1/categorias/{id}` - Obter um (200 / 404)
- `POST /api/v1/categorias` - Criar (201 + `Location` / 400 / 409 nome duplicado)
- `PUT /api/v1/categorias/{id}` - Atualizar (200 / 400 / 404 / 409)
- `DELETE /api/v1/categorias/{id}` - Remover (204 / 404 / 409 com produtos vinculados)

### Produtos
- `GET /api/v1/produtos` - Listar com paginação, filtros e ordenação (200)
- `GET /api/v1/produtos/{id}` - Obter um (200 / 404)
- `POST /api/v1/produtos` - Criar (201 + `Location` / 400 / 409 SKU ou nome duplicado / 422 categoria inválida)
- `PUT /api/v1/produtos/{id}` - Atualizar (200 / 400 / 404 / 409 / 422)
- `PATCH /api/v1/produtos/{id}/status` - Ativar/inativar, corpo `{ "ativo": bool }` (204 / 404)
- `DELETE /api/v1/produtos/{id}` - Remover logicamente (204 / 404)

### Contratos (exemplo)

`POST /api/v1/produtos`
```json
{ "categoriaId": "uuid", "nome": "Teclado Mecânico", "sku": "TEC-001", "descricao": "ABNT2", "preco": 349.90 }
```

Resposta de listagem:
```json
{ "itens": [ ], "page": 1, "pageSize": 20, "totalItens": 0, "totalPaginas": 0 }
```

---

## 🧪 Testes

Cobertura mínima: 80% da Application Layer

- [ ] Testes unitários (validators, handlers/services, regras BR-001 a BR-007)
- [ ] Testes de integração (endpoints com banco em memória/Testcontainers: status codes, paginação, filtros, conflitos)

---

## 🔒 Segurança

- [ ] Autenticação JWT; leitura (`GET`) exige usuário autenticado, escrita exige role `Admin`
- [ ] Validação de entrada (FluentValidation) em todos os comandos; 400 com detalhes por campo
- [ ] Rate limiting (ex.: 100 req/min por IP)
- [ ] HTTPS em produção
- [ ] Nenhum segredo no código (connection string e chave JWT via configuração/secrets)
- [ ] Consultas parametrizadas (EF Core), sem SQL concatenado

---

## ✅ Critérios de Aceitar

- [ ] Todos os endpoints funcionando com os status codes descritos
- [ ] Regras BR-001 a BR-007 cobertas por testes
- [ ] Paginação, filtros e ordenação funcionando na listagem de produtos
- [ ] Testes com 80%+ cobertura da Application Layer
- [ ] Código segue SOLID e Clean Architecture (Domain, Application, Infrastructure, Api)
- [ ] Sem vulnerabilidades críticas

---

**Pronto para orquestração!** 🚀

Edite este arquivo e chame:
```
/inicia-orquestracao
```
