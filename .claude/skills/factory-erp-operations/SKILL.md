---
name: factory-erp-operations
description: Sistemi şirket içi sunucuda çalıştırmak, Docker deployment, backup/restore, logging, monitoring, health checks, environment configuration ve operasyon dokümantasyonu için kullan.
---

# Factory ERP Operations

## Amaç

Sistemin şirket bilgisayarında/server'ında güvenilir şekilde çalıştırılmasını ve sürdürülebilir biçimde işletilmesini sağla.

## Accepted-decision operations

- Read `/design/decision-log.md` and `/design/decision-clarification-backlog.md` before finalizing deployment or recovery instructions; current O-001–O-014 values are the accepted baseline.
- Apply O-010 as the operational baseline: daily full backup, separate disk/NAS target, 14-day retention, monthly restore test, with RPO ≤ 24 hours and RTO ≤ 8 hours as the accepted starting targets.
- Apply O-011 as the deployment baseline: Ubuntu LTS, Docker Compose, PostgreSQL, reverse proxy, company LAN HTTPS and isolated public route; do not expose internal ERP endpoints directly to the internet.
- For public access, payroll, delivery proof and financial data, document the data exposure boundary, access owner, logging rule and incident response path according to O-008/O-009.
- A deployment is not operationally accepted until backup restore, health check, smoke test, mobile LAN access and permission-sensitive endpoints are verified on the target environment.
- If a new or changed operational decision appears, reopen the relevant O-ID and stop treating the changed value as final until owner/date/evidence are recorded.

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


## ADR-001–ADR-011 operational handoff

Architecture ADR-001–ADR-011 is accepted for MVP handoff. Operations must preserve the following runtime rules: positive transaction quantities and zero-capable projections; immutable packaging snapshots; row_version/ETag; PostgreSQL Read Committed with deterministic source-row locking; atomic command effects; committed outbox processing; typed conflict codes; and protected release-only self-hosted runner access.

The first implementation slice does not change production deployment. Compose, migration, backup and restore procedures remain design artifacts until implementation evidence exists. Once the slice is scaffolded, CI must verify domain tests, architecture tests, schema compatibility and outbox/health behavior before any on-prem release job is enabled.
