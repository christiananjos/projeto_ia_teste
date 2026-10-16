# Workflow de testes da API de Catálogo

> Gerado pelo 05-test-engineer em 2026-10-16 a partir de `docs/SPEC.md`, da spec técnica (§6) e do código em
> `src/API`. Os status, os códigos (`code`) e as mensagens abaixo são os mesmos verificados pelos testes HTTP em
> `src/Tests/Catalogo.IntegrationTests/Api/` (pipeline real, com a persistência substituída). Os cenários que
> dependem do banco (unicidade sem diferenciar maiúsculas, reuso de SKU, ADR-001) estão cobertos por
> `src/Tests/Catalogo.IntegrationTests/Persistencia/`, que exige Docker.
>
> Importável no Postman/Insomnia: cada bloco `curl` pode ser colado em *Import → Raw text*.

## Preparação

Base usada nos exemplos: `https://localhost:7080` (perfil `https` do `launchSettings.json`; `http://localhost:5080`
também funciona em Development).

```bash
export BASE=https://localhost:7080
```

### Configuração mínima (user-secrets da API)

```bash
cd src/API
dotnet user-secrets set "ConnectionStrings:Catalogo" "Server=localhost,1433;Database=Catalogo;User Id=sa;Password=<senha>;TrustServerCertificate=True"
dotnet user-secrets set "Autenticacao:Jwt:Issuer" "https://auth.catalogo.local"
dotnet user-secrets set "Autenticacao:Jwt:ChaveAssinatura" "<chave com pelo menos 32 bytes>"
dotnet ef database update --project ../Infrastructure --startup-project .
dotnet run --launch-profile https
```

### Tokens de teste

Ainda não há provedor de identidade (ver `knowledge/vault/14 - Planejamento/Provedor de identidade.md`). Para
testar localmente, gere um JWT HS256 com a mesma chave, issuer e audience configurados. A claim de papel é
`role` e o nome vem de `sub`:

```bash
# uso: node gerar-token.cjs <role|-> ; "-" gera token sem role (só leitura)
cat > gerar-token.cjs <<'EOF'
const crypto = require('crypto');
const b64 = o => Buffer.from(JSON.stringify(o)).toString('base64url');
const agora = Math.floor(Date.now() / 1000);
const role = process.argv[2];
const payload = { sub: 'usuario-local', iss: process.env.JWT_ISSUER, aud: 'catalogo-api', iat: agora, nbf: agora, exp: agora + 3600 };
if (role && role !== '-') payload.role = role;
const corpo = `${b64({ alg: 'HS256', typ: 'JWT' })}.${b64(payload)}`;
const assinatura = crypto.createHmac('sha256', process.env.JWT_CHAVE).update(corpo).digest('base64url');
console.log(`${corpo}.${assinatura}`);
EOF
export JWT_ISSUER="https://auth.catalogo.local" JWT_CHAVE="<a mesma chave do user-secrets>"
export TOKEN_ADMIN=$(node gerar-token.cjs Admin)
export TOKEN_LEITOR=$(node gerar-token.cjs -)
```

### Regras gerais verificadas

- `GET` exige token válido (qualquer usuário). `POST`, `PUT`, `PATCH` e `DELETE` exigem a role `Admin`.
- Sem token, com token expirado, com outro issuer ou com outra chave: `401 Unauthorized`, `code: "nao_autenticado"`.
- Token válido sem `Admin` em escrita: `403 Forbidden`, `code: "acesso_negado"`.
- Mais de 100 requisições por minuto vindas do mesmo IP: `429 Too Many Requests`, header `Retry-After`,
  `code: "limite_requisicoes"`. O limite vale também para requisições sem token.
- Id que não é GUID (`/api/v1/produtos/abc`): `404 Not Found`, `code: "nao_encontrado"`, porque a rota não casa.
- Tipo inválido na query string (`page=abc`, `ativo=talvez`, `categoriaId=x`) ou JSON malformado:
  `400 Bad Request`, `code: "requisicao_invalida"`, sem `errors`.
- Todo erro sai como `application/problem+json` com `type`, `title`, `status`, `detail`, `instance`, `code` e
  `traceId`. Erros de validação trazem também `errors` com chaves em camelCase.

Exemplo de corpo de erro de validação:

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Requisição inválida",
  "status": 400,
  "detail": "Um ou mais campos são inválidos.",
  "instance": "/api/v1/produtos",
  "errors": { "sku": ["O SKU deve conter apenas letras, números e hífen."] },
  "code": "validacao",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```

---

## Endpoint: POST /api/v1/categorias

### Cenário de Sucesso

```bash
curl -i -X POST "$BASE/api/v1/categorias" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "nome": "Periféricos", "descricao": "Teclados, mouses e headsets" }'
```

**Resposta esperada:** `201 Created`, header `Location: /api/v1/categorias/{id}`

```json
{
  "id": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01",
  "nome": "Periféricos",
  "descricao": "Teclados, mouses e headsets",
  "ativa": true,
  "dataCriacao": "2026-10-16T12:00:00Z",
  "dataAtualizacao": "2026-10-16T12:00:00Z"
}
```

Datas enviadas no corpo são ignoradas: o servidor sempre define `dataCriacao`/`dataAtualizacao` em UTC (BR-007).

### Cenário de Erro — Nome com espaço nas pontas

```bash
curl -i -X POST "$BASE/api/v1/categorias" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "nome": " Periféricos" }'
```

**Resposta esperada:** `400 Bad Request`, `code: "validacao"`,
`errors.nome: ["O nome não pode começar nem terminar com espaços."]`

### Cenário de Erro — Nome duplicado (sem diferenciar maiúsculas)

```bash
curl -i -X POST "$BASE/api/v1/categorias" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "nome": "PERIFÉRICOS" }'
```

**Resposta esperada:** `409 Conflict`, `code: "categoria.nome_duplicado"`, `detail: "Já existe uma categoria com este nome."`

### Cenário de Erro — Sem a role Admin

```bash
curl -i -X POST "$BASE/api/v1/categorias" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_LEITOR" \
  -d '{ "nome": "Acessórios" }'
```

**Resposta esperada:** `403 Forbidden`, `code: "acesso_negado"`

### Cenário de Erro — JSON malformado

```bash
curl -i -X POST "$BASE/api/v1/categorias" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "nome": '
```

**Resposta esperada:** `400 Bad Request`, `code: "requisicao_invalida"`

---

## Endpoint: GET /api/v1/categorias

### Cenário de Sucesso

```bash
curl -i "$BASE/api/v1/categorias?page=1&pageSize=20&nome=peri&ativa=true" \
  -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `200 OK`, ordenado por `nome asc` (desempate `id asc`)

```json
{
  "itens": [
    {
      "id": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01",
      "nome": "Periféricos",
      "descricao": "Teclados, mouses e headsets",
      "ativa": true,
      "dataCriacao": "2026-10-16T12:00:00Z",
      "dataAtualizacao": "2026-10-16T12:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItens": 1,
  "totalPaginas": 1
}
```

Sem parâmetros: `page=1`, `pageSize=20`, todas as categorias (ativas e inativas). Página além da última: `200`
com `itens: []` e os totais reais.

### Cenário de Erro — pageSize acima de 100

```bash
curl -i "$BASE/api/v1/categorias?pageSize=101" -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `400 Bad Request`, `code: "validacao"`,
`errors.pageSize: ["O tamanho da página deve estar entre 1 e 100."]`

### Cenário de Erro — Tipo inválido na query string

```bash
curl -i "$BASE/api/v1/categorias?ativa=talvez" -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `400 Bad Request`, `code: "requisicao_invalida"`

### Cenário de Erro — Sem autenticação

```bash
curl -i "$BASE/api/v1/categorias"
```

**Resposta esperada:** `401 Unauthorized`, `code: "nao_autenticado"`

---

## Endpoint: GET /api/v1/categorias/{id}

### Cenário de Sucesso

```bash
curl -i "$BASE/api/v1/categorias/0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01" \
  -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `200 OK` com o `CategoriaResponse` (mesmo formato do POST).

### Cenário de Erro — Categoria não encontrada

```bash
curl -i "$BASE/api/v1/categorias/0199f0a1-0000-7000-8000-000000000000" \
  -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `404 Not Found`, `code: "categoria.nao_encontrada"`, `detail: "Categoria não encontrada."`

### Cenário de Erro — Id que não é GUID

```bash
curl -i "$BASE/api/v1/categorias/abc" -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `404 Not Found`, `code: "nao_encontrado"` (a rota `{id:guid}` não casa)

---

## Endpoint: PUT /api/v1/categorias/{id}

### Cenário de Sucesso — Renomear e inativar

```bash
curl -i -X PUT "$BASE/api/v1/categorias/0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "nome": "Periféricos Gamer", "descricao": "Linha gamer", "ativa": false }'
```

**Resposta esperada:** `200 OK` com `nome: "Periféricos Gamer"`, `ativa: false` e `dataAtualizacao` nova.
Manter o próprio nome não conflita. É substituição completa: `descricao` omitida limpa o campo.

### Cenário de Erro — Sem o campo `ativa`

```bash
curl -i -X PUT "$BASE/api/v1/categorias/0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "nome": "Periféricos" }'
```

**Resposta esperada:** `400 Bad Request`, `code: "validacao"`, `errors.ativa: ["O campo ativa é obrigatório."]`

### Cenário de Erro — Nome de outra categoria

```bash
curl -i -X PUT "$BASE/api/v1/categorias/0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "nome": "Acessórios", "ativa": true }'
```

**Resposta esperada:** `409 Conflict`, `code: "categoria.nome_duplicado"`

### Cenário de Erro — Categoria não encontrada

```bash
curl -i -X PUT "$BASE/api/v1/categorias/0199f0a1-0000-7000-8000-000000000000" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "nome": "Periféricos", "ativa": true }'
```

**Resposta esperada:** `404 Not Found`, `code: "categoria.nao_encontrada"`

---

## Endpoint: DELETE /api/v1/categorias/{id}

### Cenário de Sucesso — Sem produtos (ou só com produtos removidos)

```bash
curl -i -X DELETE "$BASE/api/v1/categorias/0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01" \
  -H "Authorization: Bearer $TOKEN_ADMIN"
```

**Resposta esperada:** `204 No Content`. A categoria é apagada fisicamente. Se ela tinha apenas produtos
removidos logicamente, eles são apagados na mesma transação (ADR-001).

### Cenário de Erro — Categoria com produto ativo ou inativo (não removido)

```bash
curl -i -X DELETE "$BASE/api/v1/categorias/0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01" \
  -H "Authorization: Bearer $TOKEN_ADMIN"
```

**Resposta esperada:** `409 Conflict`, `code: "categoria.possui_produtos"`,
`detail: "A categoria possui produtos vinculados e não pode ser removida."`

### Cenário de Erro — Categoria não encontrada

```bash
curl -i -X DELETE "$BASE/api/v1/categorias/0199f0a1-0000-7000-8000-000000000000" \
  -H "Authorization: Bearer $TOKEN_ADMIN"
```

**Resposta esperada:** `404 Not Found`, `code: "categoria.nao_encontrada"`

---

## Endpoint: POST /api/v1/produtos

### Cenário de Sucesso

```bash
curl -i -X POST "$BASE/api/v1/produtos" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "categoriaId": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01", "nome": "Teclado Mecânico", "sku": "TEC-001", "descricao": "ABNT2", "preco": 349.90 }'
```

**Resposta esperada:** `201 Created`, header `Location: /api/v1/produtos/{id}`

```json
{
  "id": "0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07",
  "categoriaId": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01",
  "categoriaNome": "Periféricos",
  "nome": "Teclado Mecânico",
  "sku": "TEC-001",
  "descricao": "ABNT2",
  "preco": 349.90,
  "ativo": true,
  "dataCriacao": "2026-10-16T12:05:00Z",
  "dataAtualizacao": "2026-10-16T12:05:00Z"
}
```

`dataRemocao` nunca aparece na resposta. O SKU é guardado como foi enviado (sem mudar maiúsculas).

### Cenário de Erro — SKU com quebra de linha no final (regressão)

```bash
curl -i -X POST "$BASE/api/v1/produtos" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "categoriaId": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01", "nome": "Teclado Mecânico", "sku": "TEC-001\n", "preco": 349.90 }'
```

**Resposta esperada:** `400 Bad Request`, `code: "validacao"`,
`errors.sku: ["O SKU deve conter apenas letras, números e hífen."]`. O mesmo vale para `"AB\n"`.

### Cenário de Erro — Vários campos inválidos

```bash
curl -i -X POST "$BASE/api/v1/produtos" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "nome": "ab", "sku": "", "preco": 0.001 }'
```

**Resposta esperada:** `400 Bad Request`, `code: "validacao"`, com `errors.categoriaId`, `errors.nome`,
`errors.sku` e `errors.preco` ("O preço deve ter no máximo 2 casas decimais.").

### Cenário de Erro — Categoria inexistente ou inativa

```bash
curl -i -X POST "$BASE/api/v1/produtos" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "categoriaId": "0199f0a1-0000-7000-8000-000000000000", "nome": "Teclado Mecânico", "sku": "TEC-009", "preco": 349.90 }'
```

**Resposta esperada:** `422 Unprocessable Entity`, `code: "produto.categoria_invalida"`,
`detail: "A categoria informada não existe ou está inativa."`

### Cenário de Erro — SKU já usado (sem diferenciar maiúsculas, em qualquer categoria)

```bash
curl -i -X POST "$BASE/api/v1/produtos" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "categoriaId": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01", "nome": "Outro Teclado", "sku": "tec-001", "preco": 199.90 }'
```

**Resposta esperada:** `409 Conflict`, `code: "produto.sku_duplicado"`

### Cenário de Erro — Nome já usado na mesma categoria

```bash
curl -i -X POST "$BASE/api/v1/produtos" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "categoriaId": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01", "nome": "TECLADO MECÂNICO", "sku": "TEC-002", "preco": 299.90 }'
```

**Resposta esperada:** `409 Conflict`, `code: "produto.nome_duplicado"`. Ordem das checagens:
400 → 422 → 409 SKU → 409 nome.

---

## Endpoint: GET /api/v1/produtos

### Cenário de Sucesso — Filtros e ordenação

```bash
curl -i "$BASE/api/v1/produtos?page=1&pageSize=20&categoriaId=0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01&nome=teclado&ordenarPor=preco&direcao=desc" \
  -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `200 OK`

```json
{
  "itens": [
    {
      "id": "0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07",
      "categoriaId": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01",
      "categoriaNome": "Periféricos",
      "nome": "Teclado Mecânico",
      "sku": "TEC-001",
      "descricao": "ABNT2",
      "preco": 349.90,
      "ativo": true,
      "dataCriacao": "2026-10-16T12:05:00Z",
      "dataAtualizacao": "2026-10-16T12:05:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItens": 1,
  "totalPaginas": 1
}
```

Padrões: `page=1`, `pageSize=20`, `ativo=true` (inativos só com `ativo=false`), `ordenarPor=nome`,
`direcao=asc`, desempate `id asc`. `nome` é "contém", sem diferenciar maiúsculas, e `%`/`_` são tratados como
texto.

### Cenário de Sucesso — Só inativos

```bash
curl -i "$BASE/api/v1/produtos?ativo=false" -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `200 OK` só com produtos `ativo: false`.

### Cenário de Erro — Ordenação desconhecida

```bash
curl -i "$BASE/api/v1/produtos?ordenarPor=data&direcao=cima" -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `400 Bad Request`, `code: "validacao"`,
`errors.ordenarPor: ["O campo ordenarPor deve ser 'nome' ou 'preco'."]`,
`errors.direcao: ["O campo direcao deve ser 'asc' ou 'desc'."]`

### Cenário de Erro — Tipo inválido na query string

```bash
curl -i "$BASE/api/v1/produtos?categoriaId=x" -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `400 Bad Request`, `code: "requisicao_invalida"`

### Cenário de Erro — Sem autenticação

```bash
curl -i "$BASE/api/v1/produtos"
```

**Resposta esperada:** `401 Unauthorized`, `code: "nao_autenticado"`

---

## Endpoint: GET /api/v1/produtos/{id}

### Cenário de Sucesso (inclui produto inativo)

```bash
curl -i "$BASE/api/v1/produtos/0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07" \
  -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `200 OK` com o `ProdutoResponse`, incluindo `categoriaNome`. Produto inativo também é
retornado.

### Cenário de Erro — Produto inexistente ou removido

```bash
curl -i "$BASE/api/v1/produtos/0199f0a2-0000-7000-8000-000000000000" \
  -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `404 Not Found`, `code: "produto.nao_encontrado"`, `detail: "Produto não encontrado."`

---

## Endpoint: PUT /api/v1/produtos/{id}

### Cenário de Sucesso

```bash
curl -i -X PUT "$BASE/api/v1/produtos/0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "categoriaId": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01", "nome": "Teclado Mecânico RGB", "descricao": "ABNT2 com iluminação", "preco": 399.90, "sku": "OUTRO-SKU", "ativo": false }'
```

**Resposta esperada:** `200 OK` com `nome`, `descricao` e `preco` novos. `sku` continua `"TEC-001"` e `ativo`
continua `true`: esses campos não fazem parte do PUT e são ignorados (BR-003, ADR-006). Manter a categoria
atual é aceito mesmo que ela esteja inativa.

### Cenário de Erro — Mover para categoria inexistente ou inativa

```bash
curl -i -X PUT "$BASE/api/v1/produtos/0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "categoriaId": "0199f0a1-0000-7000-8000-000000000000", "nome": "Teclado Mecânico", "preco": 349.90 }'
```

**Resposta esperada:** `422 Unprocessable Entity`, `code: "produto.categoria_invalida"`

### Cenário de Erro — Sem preço

```bash
curl -i -X PUT "$BASE/api/v1/produtos/0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "categoriaId": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01", "nome": "Teclado Mecânico" }'
```

**Resposta esperada:** `400 Bad Request`, `code: "validacao"`, `errors.preco: ["O preço é obrigatório."]`

### Cenário de Erro — Nome de outro produto da categoria

```bash
curl -i -X PUT "$BASE/api/v1/produtos/0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "categoriaId": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01", "nome": "Mouse Gamer", "preco": 349.90 }'
```

**Resposta esperada:** `409 Conflict`, `code: "produto.nome_duplicado"`

### Cenário de Erro — Produto inexistente ou removido

```bash
curl -i -X PUT "$BASE/api/v1/produtos/0199f0a2-0000-7000-8000-000000000000" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "categoriaId": "0199f0a1-7c3e-7d2a-9b41-3f5e8c2d1a01", "nome": "Teclado Mecânico", "preco": 349.90 }'
```

**Resposta esperada:** `404 Not Found`, `code: "produto.nao_encontrado"`

---

## Endpoint: PATCH /api/v1/produtos/{id}/status

### Cenário de Sucesso — Inativar

```bash
curl -i -X PATCH "$BASE/api/v1/produtos/0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07/status" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "ativo": false }'
```

**Resposta esperada:** `204 No Content`. O produto sai da listagem padrão, mas continua no `GET /{id}`.
Repetir com o mesmo valor também responde `204` e não altera `dataAtualizacao` (idempotente).

### Cenário de Erro — Sem `ativo`

```bash
curl -i -X PATCH "$BASE/api/v1/produtos/0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07/status" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ }'
```

**Resposta esperada:** `400 Bad Request`, `code: "validacao"`, `errors.ativo: ["O campo ativo é obrigatório."]`

### Cenário de Erro — Produto inexistente ou removido

```bash
curl -i -X PATCH "$BASE/api/v1/produtos/0199f0a2-0000-7000-8000-000000000000/status" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN_ADMIN" \
  -d '{ "ativo": true }'
```

**Resposta esperada:** `404 Not Found`, `code: "produto.nao_encontrado"`

---

## Endpoint: DELETE /api/v1/produtos/{id}

### Cenário de Sucesso

```bash
curl -i -X DELETE "$BASE/api/v1/produtos/0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07" \
  -H "Authorization: Bearer $TOKEN_ADMIN"
```

**Resposta esperada:** `204 No Content`. Exclusão lógica: o produto some de todas as consultas e libera nome e
SKU para reuso (um novo `POST` com `"sku": "TEC-001"` passa a responder `201`).

### Cenário de Erro — Remover de novo

```bash
curl -i -X DELETE "$BASE/api/v1/produtos/0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07" \
  -H "Authorization: Bearer $TOKEN_ADMIN"
```

**Resposta esperada:** `404 Not Found`, `code: "produto.nao_encontrado"` (BR-005)

### Cenário de Erro — Sem a role Admin

```bash
curl -i -X DELETE "$BASE/api/v1/produtos/0199f0a2-1b5d-7e40-8c11-6a2f4d9e3b07" \
  -H "Authorization: Bearer $TOKEN_LEITOR"
```

**Resposta esperada:** `403 Forbidden`, `code: "acesso_negado"`

---

## Roteiro sugerido (coleção Postman/Insomnia)

1. `POST /categorias` "Periféricos" → guarde o `id` como `categoriaId`.
2. `POST /produtos` "TEC-001" → guarde o `id` como `produtoId`.
3. `POST /produtos` com `"sku": "tec-001"` → `409 produto.sku_duplicado`.
4. `GET /produtos?nome=teclado` → 1 item.
5. `PATCH /produtos/{produtoId}/status` `{ "ativo": false }` → `204`. `GET /produtos` → 0 itens; `?ativo=false` → 1.
6. `DELETE /categorias/{categoriaId}` → `409 categoria.possui_produtos` (produto inativo ainda conta).
7. `DELETE /produtos/{produtoId}` → `204`; de novo → `404`.
8. `DELETE /categorias/{categoriaId}` → `204` (o produto removido é apagado junto, ADR-001).
