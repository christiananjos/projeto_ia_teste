# Checklist de Revisão de Arquitetura AWS

Use este checklist antes de considerar uma arquitetura AWS pronta pra produção.

## Rede
- [ ] VPC com subnets públicas e privadas separadas.
- [ ] Banco de dados e recursos internos vivem em subnet privada, sem IP público.
- [ ] NAT Gateway configurado se recursos em subnet privada precisam de saída pra internet (ex: baixar pacotes).
- [ ] Load Balancer (ALB/NLB) na frente de qualquer serviço com múltiplas instâncias/tasks.

## Segurança
- [ ] Security Groups seguem menor privilégio (nenhuma porta aberta pra `0.0.0.0/0` além de HTTP/HTTPS no load balancer público).
- [ ] Nenhuma credencial estática (access key/secret) hardcoded em código ou variável de ambiente commitada — usar IAM Roles atribuídas ao recurso sempre que possível.
- [ ] Secrets (connection string, API keys) vêm do Secrets Manager ou Parameter Store, não de config commitada.
- [ ] Criptografia em repouso habilitada (S3, RDS, EBS) e em trânsito (TLS/HTTPS obrigatório).
- [ ] MFA habilitado pra usuários IAM com acesso ao console, especialmente contas com privilégio administrativo.

## Resiliência
- [ ] Recursos críticos (RDS, ECS) distribuídos em pelo menos 2 Availability Zones.
- [ ] Auto Scaling configurado pra computação (ECS/EC2) com métricas razoáveis (CPU, latência, tamanho de fila).
- [ ] Backup automático habilitado no RDS com retenção adequada ao RPO do negócio.
- [ ] Health checks configurados no Load Balancer/ECS pra remover automaticamente instâncias não saudáveis.

## Observabilidade
- [ ] CloudWatch Logs configurado pra aplicação (não só métricas de infraestrutura).
- [ ] Alarmes CloudWatch pra métricas críticas (CPU alta, erro 5xx, fila crescendo) com notificação (SNS) configurada.
- [ ] X-Ray ou alternativa de tracing distribuído, se a arquitetura tiver múltiplos serviços chamando uns aos outros.

## Custo
- [ ] Tags de custo (ambiente, projeto, responsável) aplicadas nos recursos pra rastreamento no Cost Explorer.
- [ ] Nenhum recurso órfão (EBS volumes não anexados, Elastic IPs não usados, snapshots antigos) acumulando custo.
- [ ] Lifecycle rules configuradas no S3 pra dados que não precisam ficar em Standard indefinidamente.
