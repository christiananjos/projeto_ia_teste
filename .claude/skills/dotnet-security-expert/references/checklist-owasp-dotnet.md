# Checklist de Revisão de Segurança — OWASP Top 10 aplicado a .NET

Use este checklist ao fazer uma revisão de segurança completa de um projeto ou PR .NET. Para cada item, marque como OK, N/A, ou aponte o achado com severidade.

## 1. Broken Access Control
- [ ] Todo endpoint que recebe um ID de recurso (`{id}` na rota) valida que o recurso pertence ao usuário/tenant autenticado, não só que o usuário está autenticado.
- [ ] Nenhum endpoint administrativo depende só de "o frontend não mostra o botão" — a checagem de role/policy está no backend.
- [ ] CORS não usa `AllowAnyOrigin()` combinado com `AllowCredentials()` (combinação inválida e insegura).
- [ ] Rotas de arquivo estático não expõem diretórios sensíveis (ex: `wwwroot` não contém configs ou secrets).

## 2. Cryptographic Failures
- [ ] Senhas nunca são armazenadas em texto puro ou com hash sem salt (checar uso de `PasswordHasher<T>` ou Identity).
- [ ] Dados sensíveis em trânsito usam HTTPS obrigatório (`UseHttpsRedirection` + HSTS em produção).
- [ ] Criptografia simétrica usa modo autenticado (AES-GCM) em vez de CBC sem HMAC.
- [ ] Nenhum uso de algoritmos obsoletos (`MD5`, `SHA1` para hash de senha, `DES`).

## 3. Injection
- [ ] Nenhum `FromSqlRaw`/`ExecuteSqlRaw` com concatenação de string — só parâmetros.
- [ ] Comandos de shell (`Process.Start`) não recebem input do usuário sem sanitização/allowlist.
- [ ] Queries LDAP, XPath ou NoSQL (se houver) também usam parametrização, não concatenação.

## 4. Insecure Design
- [ ] Fluxos sensíveis (reset de senha, mudança de e-mail, exclusão de conta) exigem reautenticação ou confirmação, não só uma chamada de API autenticada.
- [ ] Rate limiting está presente em endpoints de login/reset de senha (prevenção de brute force).

## 5. Security Misconfiguration
- [ ] `UseDeveloperExceptionPage()` só roda quando `IsDevelopment()` é true.
- [ ] Headers de segurança presentes: `X-Content-Type-Options: nosniff`, `X-Frame-Options` ou `Content-Security-Policy` com `frame-ancestors`, `Referrer-Policy`.
- [ ] Swagger/OpenAPI não está exposto publicamente em produção sem autenticação.
- [ ] Mensagens de erro em produção não vazam stack trace ou detalhes de infraestrutura.

## 6. Vulnerable and Outdated Components
- [ ] `dotnet list package --vulnerable --include-transitive` rodado e sem findings críticos/altos.
- [ ] `dotnet list package --deprecated` sem pacote abandonado em uso crítico.
- [ ] Versão do .NET runtime ainda dentro do período de suporte (LTS ou STS ativo).

## 7. Identification and Authentication Failures
- [ ] JWT valida `issuer`, `audience` e `lifetime`.
- [ ] Refresh tokens são armazenados com hash e rotacionados a cada uso.
- [ ] Sessão/token tem expiração razoável (não infinita).
- [ ] MFA disponível para contas com privilégio elevado, quando aplicável ao contexto do produto.

## 8. Software and Data Integrity Failures
- [ ] Nenhum uso de `BinaryFormatter` para (de)serialização.
- [ ] Se usa `Newtonsoft.Json`, `TypeNameHandling` não está em `All` sem uma allowlist de tipos.
- [ ] Pipeline de CI/CD assina ou verifica integridade de artefatos antes do deploy (quando aplicável ao nível de maturidade do time).

## 9. Security Logging and Monitoring Failures
- [ ] Eventos de autenticação (login falho, mudança de senha, mudança de permissão) são logados.
- [ ] Logs não contêm dados sensíveis (senha, token, número de cartão) em texto puro.
- [ ] Existe alerta ou monitoramento para padrões anômalos (múltiplas falhas de login, por exemplo).

## 10. Server-Side Request Forgery (SSRF)
- [ ] Toda chamada HTTP server-side com URL vinda do usuário valida contra allowlist de hosts/esquemas.
- [ ] Serviços internos (metadata endpoints de cloud, bancos internos) não são alcançáveis a partir de uma URL arbitrária fornecida pelo usuário.
