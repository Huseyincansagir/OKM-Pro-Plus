---
name: factory-erp-operations
description: Sistemi şirket içi sunucuda çalıştırmak, Docker deployment, backup/restore, logging, monitoring, health checks, environment configuration ve operasyon dokümantasyonu için kullan.
---

# Factory ERP Operations

## Amaç

Sistemin şirket bilgisayarında/server'ında güvenilir şekilde çalıştırılmasını ve sürdürülebilir biçimde işletilmesini sağla.

## Decision-dependent operations

- Read `/design/decision-log.md` and `/design/decision-clarification-backlog.md` before finalizing deployment or recovery instructions.
- Treat O-010 (RPO/RTO) and O-011 (server/LAN/HTTPS topology) as required operational decisions. Do not present retention, network exposure, certificate, or remote-access values as final until the owner and target are recorded.
- For public access, payroll, delivery proof and financial data, document the data exposure boundary, access owner, logging rule and incident response path.
- A deployment is not operationally accepted until backup restore, health check, smoke test, mobile LAN access and permission-sensitive endpoints are verified on the target environment.

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

Backup çalışıyor kabul edilmez; düzenli restore testi yapılmalı. Restore kabul kaydı; test tarihi, backup source, restored database version, veri bütünlüğü kontrolü, RPO/RTO sonucu ve sorumlu kullanıcıyı içermeli. O-010 kapanmadan retention veya RPO/RTO değerleri yalnızca öneri olarak etiketlenmeli.

## Monitoring

Health endpoints:

`/health`

En az:

- application health
- database connectivity
- disk usage
- backup status
- active shipment/route health
- vehicle and delivery exception counts
- package scan/failed-delivery queue

izlenmeli.

## Logging

Structured logs:

- timestamp
- level
- correlation/request id
- user id
- action
- error details where safe
- shipment id, route plan id, vehicle id, route stop id, package barcode correlation fields where safe
- load-plan validation result and delivery exception reason

Passwords/token/secrets loglama; teslim kanıtı dosya içeriğini veya kişisel veriyi loglama.

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
- delivery-proof file loss or unauthorized exposure
- route/load-plan state corruption
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
