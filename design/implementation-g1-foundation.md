# Factory ERP-Lite — G1 Backend Foundation Evidence

**Durum:** G1 tamamlandı — G2’ye geçişe hazır
**Tarih:** 2026-08-16
**Başlangıç:** `d487f1d` UI reference set
**Bu slice commit’i:** G1 commit’i ile birlikte yayınlanacak

## 1. Tamamlanan kapsam

G1 kapsamında solution’a aşağıdaki projeler eklendi:

| Proje | Sorumluluk |
|---|---|
| `FactoryErp.Application` | EF/HTTP bağımsız application abstraction (`IUnitOfWork`) |
| `FactoryErp.Infrastructure` | EF Core DbContext, Npgsql mapping, health check, migration assembly |
| `FactoryErp.Api` | ASP.NET Core composition root, safe exception handling, health endpoints |
| `FactoryErp.Migrator` | Kontrollü one-shot migration runner |
| `FactoryErp.Infrastructure.UnitTests` | EF model mapping ve persistence boundary testleri |

Infrastructure persistence foundation; users, roles, permissions, user_roles, role_permissions, refresh_tokens, audit_logs, idempotency_records, system_settings, document_sequences ve outbox_messages tablolarını içerir. Explicit `snake_case` mapping, restricted ledger-style relations, composite keys, unique idempotency scope/key index’ı, `jsonb` audit/outbox alanları ve `row_version bigint` concurrency token mapping’i eklenmiştir.

İki migration uygulanmıştır. İlki generated `InitialIdentityAndAudit`; ikincisi trigger ve idempotent foundation seed’i sağlayan `AddFoundationTriggerAndSeed` migration’ıdır. Seed; system admin, viewer, sales, warehouse, production, accounting ve hr rollerini; temel system/audit/product/warehouse/sales permission’larını; timezone ve foundation schema setting’lerini yükler.

## 2. Çalıştırılan kanıtlar

| Kontrol | Sonuç |
|---|---|
| `dotnet build FactoryErp.sln --configuration Release` | 0 warning / 0 error |
| Domain unit tests | 28 passed |
| Architecture tests | 5 passed |
| Infrastructure persistence model tests | 4 passed |
| EF migration apply | 2 migration başarılı |
| Migration idempotency | İkinci çalıştırmada `No migrations were applied` |
| Seed counts | 7 role, 6 permission, 2 setting, 6 role-permission |
| API `/health/live` | 200 / `status=live` |
| API `/health/ready` | Healthy |
| API `/health/startup` | Healthy |
| API root | `FactoryErp.Api`, `version=g1` |

Test çalıştırmalarında FluentAssertions lisans bilgilendirme mesajı görünmektedir; test başarısızlığı değildir. Ticari kullanıma geçmeden önce paket lisansı ayrıca değerlendirilmelidir.

## 3. Deployment notu

Repository’ye `deploy/compose.dev.yaml` ve `.env.example` eklendi. Compose profili PostgreSQL 16.4 kullanır, health check içerir, named volume kullanır ve geliştirme ortamında PostgreSQL’i yalnızca loopback’e bind eder.

Bu sandbox içinde Docker executable bulunmadığı için Compose container smoke test’i çalıştırılmadı. Bunun yerine sandbox’ın mevcut PostgreSQL 16.14 servisi üzerinde izole `factory_erp_g1` database’i oluşturuldu; migration apply, API health ve idempotency testleri bu database üzerinde başarılı oldu. Şirket server’ında gerçek Compose acceptance G8 production gate’inde çalıştırılacaktır.

## 4. Bilinçli olarak yapılmayanlar

G1’de business aggregate persistence’i, authentication token üretimi, API command/query handler’ları, ürün/stok/satış tabloları, web/mobile uygulama project’leri ve background worker business logic’i henüz eklenmemiştir. Bunlar G2–G6 vertical slice’larının konusudur. G1’in amacı dependency direction, migration boundary, health ve kontrollü database foundation’ı güvenilir hale getirmektir.

## 5. G2 handoff

Bir sonraki slice identity ve ortak API davranışıdır: user/role/permission application services, JWT/refresh token contract, permission policies, correlation/audit context, idempotency behavior, typed ProblemDetails ve API integration/security testleri. G2, G1 migration ve `IUnitOfWork` contract’ını temel alacak; Domain’e yeni framework dependency eklemeyecektir.
