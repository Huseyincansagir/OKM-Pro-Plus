# Factory ERP — Skill System Review

## 1. Keşif sonucu

Bootstrap promptunda root `.claude/skills/` beklenmesine rağmen mevcut repository’de skill seti `docs/06-process-skill/.claude/skills/` altında bulunmaktadır. Buna ek olarak legacy/kompakt skill dosyaları `docs/06-process-skill/factory-erp-design-workflow/` altında bulunur. Uygulama boyunca canonical agent skill kaynağı olarak `.claude/skills` paketindeki dosyalar kabul edilmelidir; legacy dosyalar yalnızca karşılaştırma referansıdır.

## 2. Skill matrisi

| Skill | Amaç | Tetikleyici | Girdiler | Çıktılar | Workflow konumu |
|---|---|---|---|---|---|
| `factory-erp-design-workflow` | Kodlama öncesi domain, UX, screen inventory, workflow ve Design Gate üretmek | Yeni ERP, fabrika, üretim/depo, public katalog, mobil operasyon veya kodlama öncesi tasarım talebi | Repository, mevcut docs, iş gereksinimi, design artefact'ları | `docs/01-design` ve `docs/02-architecture` dosyaları, decision log, readiness | DISCOVER → DESIGN → DESIGN GATE |
| `factory-erp-architecture` | Tasarımı PostgreSQL, API, transaction, RBAC ve deployment mimarisine dönüştürmek | Design Gate başarılı ve schema/API/infra kararı gerektiğinde | `docs/01-design`, `docs/02-architecture`, açık kararların çözümü | Domain boundary, schema, migration planı, API contract, deployment planı | ARCHITECTURE |
| `factory-erp-implementation` | Onaylanmış tasarımı gerçek backend/web/mobile koduna çevirmek | `implementation-ready.md` mevcut ve READY olduğunda | Design + architecture artefact'ları, repository | Database migration, domain/application, API, auth, UI/mobile, tests, docs | IMPLEMENTATION |
| `factory-erp-qa-security` | İşlev, bütünlük, yetki, güvenlik, performans ve release hazırlığını doğrulamak | Feature veya release implementation sonrası | Kod, migration, API, test planı, role matrix | Unit/integration/E2E, security review, `release-readiness.md` | TEST → SECURITY REVIEW → RELEASE GATE |
| `factory-erp-operations` | Şirket içi server deployment, backup, restore, monitoring ve işletim | Deployment, server, LAN, backup, restore veya operasyon talebi | Docker Compose, environment, server ve backup politikası | `/docs/operations` deployment, backup, restore, monitoring ve incident docs | OPERATIONS / DEPLOYMENT |

## 3. Skill ilişkisi

```text
factory-erp-design-workflow
        ↓
factory-erp-architecture
        ↓
factory-erp-implementation
        ↓
factory-erp-qa-security
        ↓
factory-erp-operations
        ↓
RELEASE GATE
```

Design skill'inin `implementation-ready.md` dosyası üretmemesi veya dosyayı `NOT READY` bırakması durumunda implementation skill'inin yeni business feature üretmemesi gerekir. Architecture skill'i design kararlarını değiştirmek zorunda kalırsa `decision-log.md` ve ilgili design artefact'ları güncellemelidir.

## 4. Mevcut aşamada uygulanan skill'ler

Bu bootstrap talimatı için yalnızca `factory-erp-design-workflow` aktif iş akışıdır. Architecture, implementation, QA/security ve operations skill'leri okunmuş; ancak henüz tetiklenmemiştir. Bu ayrım, design tamamlanmadan production code üretilmesini engeller.

## 5. Tutarlılık kontrolü

- Design skill'i UI, workflow, source of truth ve Design Gate artefact'larını ister.
- Architecture skill'i aynı domainleri PostgreSQL, API, RBAC ve transaction sınırlarına çevirir.
- Implementation skill'i açıkça `implementation-ready.md` yoksa büyük feature başlatmayı yasaklar.
- QA/security skill'i planın unit, integration, E2E, role matrix ve bütünlük testleriyle doğrulanmasını ister.
- Operations skill'i deployment, backup/restore, health endpoint, structured logs ve upgrade sırasını ister.

Bu nedenle mevcut design status `BLOCKED` olarak bırakılmış ve sonraki recommended skill `factory-erp-architecture` olarak raporlanmıştır.
