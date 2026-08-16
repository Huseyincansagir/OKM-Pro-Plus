# Factory ERP — Implementation Ready Gate

Bu dosya, `factory-erp-design-workflow` skill'inin production implementation başlangıç işaretidir. Öneri veya agent çıktısı tek başına implementation başlangıç kanıtı değildir.

```text
DESIGN:
READY FOR ARCHITECTURE
IMPLEMENTATION:
NOT READY
NEXT SKILL:
factory-erp-architecture
```

Proje sahibi O-001–O-014 karar paketini 2026-08-16 tarihinde kabul etmiştir. Bu nedenle Design Gate karar blokajı kaldırılmış ve Architecture aşamasına geçiş açılmıştır. **Bu durum production code yazılabileceği anlamına gelmez.**

## Architecture başlamadan önceki durum

Aşağıdaki kararlar `decision-log.md` içinde `DECIDED` durumundadır ve canonical tasarım belgelerine yayılmıştır:

- O-001 vergi/e-belge sınırı.
- O-002 kısmi sevkiyat ve allocation.
- O-003 kısmi fatura ve cari etkisi.
- O-004 BOM/hammadde MVP sınırı.
- O-005 lot/seri MVP sınırı.
- O-006 public talep ve müşteri kabulü.
- O-007 risk soft block/override politikası.
- O-008 maaş/bordro MVP sınırı.
- O-009 public/KVKK ve abuse controls.
- O-010 backup, RPO/RTO ve restore politikası.
- O-011 Ubuntu/Docker/LAN HTTPS ve public route sınırı.
- O-012 fiyat listesi ve snapshot politikası.
- O-013 production marka ve asset manifest.
- O-014 kargo otomasyonu ve manuel depo onayı.

## Architecture aşamasında üretilecek artefact’lar

`factory-erp-architecture` skill’i aşağıdaki çıktıları üretmeden implementation başlamaz:

- Domain aggregate ve bounded-context sınırları.
- API endpoint, DTO, validation, error ve idempotency sözleşmeleri.
- EF Core/PostgreSQL migration planı.
- Allocation, quantity, concurrency ve ledger constraints.
- RBAC/permission policy ve state transition authorization.
- Audit event ve notification matrisi.
- Web, mobile ve public data contract’ları.
- Docker Compose, reverse proxy, HTTPS, backup/restore ve health-check ayrıntıları.
- Architecture acceptance checklist ve karar traceability matrisi.

## Implementation’a geçiş koşulları

Aşağıdaki dosyalar Architecture çıktılarıyla birlikte güncellenip doğrulanmadan `IMPLEMENTATION: READY` yapılmayacaktır:

- `decision-log.md`
- `domain-model.md`
- `business-workflows.md`
- `database-technical-architecture.md`
- `master-screen-inventory.md`
- `public-catalog-design.md`
- `mobile-design.md`
- `implementation-readiness.md`
- architecture/QA/security/operations skill-impact review’ları
- migration planı, API contract testleri, permission testleri ve restore acceptance testleri

## Mevcut sonuç

```text
Architecture: ALLOWED TO START
Production code: BLOCKED UNTIL ARCHITECTURE ACCEPTANCE
```
