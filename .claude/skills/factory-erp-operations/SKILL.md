---
name: factory-erp-operations
description: Sistemi şirket içi sunucuda çalıştırmak, Docker deployment, backup/restore, logging, monitoring, health checks, environment configuration ve operasyon dokümantasyonu için kullan.
---

# Factory ERP Operations

## Amaç

Sistemin şirket bilgisayarında/server'ında güvenilir şekilde çalıştırılmasını ve sürdürülebilir biçimde işletilmesini sağla.

## Deployment

Docker Compose temel dağıtım modeli olsun.

Servisler:

- `web`
- `api`
- `postgres`
- `reverse-proxy`

Gerekirse:

- worker/background job
- monitoring

## Environment

Secret veya parola repository'ye yazma.

Örnek:

```text
.env.example
.env.local
.env.production
```

Production secrets secret management veya güvenli environment injection ile sağlanmalı.

## Local/company network

Destekle:

- LAN IP erişimi
- local DNS hostname
- HTTPS
- CORS yapılandırması
- mobile device access

## Backup

Database için:

- scheduled backup
- retention policy
- manual backup
- restore script
- restore verification

Backup çalışıyor kabul edilmez; düzenli restore testi yapılmalı.

## Monitoring

Health endpoints:

`/health`

En az:

- application health
- database connectivity
- disk usage
- backup status

izlenmeli.

## Logging

Structured logs:

- timestamp
- level
- correlation/request id
- user id
- action
- error details where safe

Passwords/token/secrets loglama.

## Upgrade strategy

Schema migration'ları versioned olsun.

Deployment sırasında:

1. backup
2. migration
3. application deployment
4. health check
5. smoke test

sıralamasını uygula.

## Disaster recovery

Dokümante et:

- server kaybı
- database corruption
- backup restore
- storage loss
- accidental user deletion
- credential compromise

## Documentation outputs

`/docs/operations` altında:

```text
DEPLOYMENT.md
BACKUP.md
RESTORE.md
MONITORING.md
INCIDENT-RESPONSE.md
ENVIRONMENT.md
```
