# L4-B6 Deployment Smoke Test Evidence

**Tarih:** 2026-08-17
**Commit:** `8e1bed6`
**Amaç:** B6 migration, seed, API startup, health endpoint ve authentication boundary’lerini temiz Compose PostgreSQL ortamında doğrulamak.

## 1. Test kapsamı ve ortam

Smoke test, repository’deki mevcut `deploy/compose.dev.yaml` dosyasıyla izole bir PostgreSQL 16.4 container’ı üzerinde çalıştırıldı. Docker Engine `29.1.3` ve Docker Compose `2.40.3` kullanıldı. Test database’i `factory_erp_b6_smoke`, test kullanıcı adı `factory_erp_b6_smoke`, test host portu `55432` olarak ayrıldı. Test sonunda container, network ve named volume temizlendi.

Mevcut development Compose dosyası yalnızca PostgreSQL servisini tanımlar. API için geçici `mcr.microsoft.com/dotnet/aspnet:8.0` container’ı, repository’nin Release output klasörü read-only mount edilerek aynı internal Docker network üzerinde çalıştırıldı. Bu yöntem API’nin gerçek container runtime davranışını doğrular; ancak production image/Dockerfile veya tam reverse-proxy/web/worker Compose kabulü değildir.

## 2. Migration ve seed sonuçları

| Kontrol | Sonuç | Kanıt |
|---|---|---|
| PostgreSQL Compose container start | PASS | PostgreSQL 16.4 container healthy |
| PostgreSQL healthcheck | PASS | Compose health status `healthy` |
| Temiz database migration | PASS | `__EFMigrationsHistory` içinde 18 migration |
| B6 tables | PASS | `dispatch_runs` ve `route_execution_events` mevcut |
| B6 permission seed | PASS | `shipment.dispatch`, `shipment.depart`, `shipment.route-execute`, `shipment.route-exception` toplam 4 kayıt |
| Integration delivery note seed | PASS | `DN` seed kaydı `Issued` status ile mevcut |
| Forward-fix migration | PASS | `ck_dispatch_runs_departed_pair` Dispatched ara state’ini kabul ediyor |
| İkinci migrator çalıştırması | PASS | Permission/DeliveryNote sayıları `4:1 → 4:1`, duplicate oluşmadı |

Migrator temiz veritabanında başarılı şekilde tamamlandı ve şu mesajı verdi:

> Factory ERP database migration and optional foundation/catalog/sales/finance seed completed.

B6 ana migration’ı ile forward-fix migration’ı birlikte uygulandı. Migration zincirinin temiz database’te çalıştığı ve seed’in ikinci çalıştırmada idempotent olduğu doğrulandı.

## 3. API container ve health sonuçları

Geçici API container’ı `postgres` service adı üzerinden Compose internal network’e bağlandı. `ConnectionStrings__FactoryErp`, JWT issuer/audience/signing key ve `ASPNETCORE_URLS` environment değişkenleriyle başlatıldı.

| Endpoint | HTTP | Response |
|---|---:|---|
| `/health/live` | 200 | `{"status":"live"}` |
| `/health/ready` | 200 | `Healthy` |
| `/health/startup` | 200 | `Healthy` |
| `/` | 200 | `FactoryErp.Api` running response |

Health response’larında password, connection string veya stack trace gözlenmedi. Health endpoint’leri anonymous olarak erişilebilir olduğu için container readiness kontrolü authentication gerektirmeden yapılabildi.

## 4. Authentication ve B6 authorization sonuçları

Migrator ile seed edilmiş admin hesabı gerçek `/api/v1/auth/login` endpoint’i üzerinden test edildi.

| Senaryo | HTTP | Beklenen | Sonuç |
|---|---:|---:|---|
| Admin login | 200 | Access token alınmalı | PASS |
| Anonymous B6 prepare request | 401 | Authentication zorunlu | PASS |
| Admin B6 prepare request | 422 | Policy geçilmeli; sahte kaynaklar domain/precondition katmanında reddedilmeli | PASS |

Admin’in B6 dispatch request’i `401` veya `403` ile reddedilmedi; `422` response, authorization boundary’sinin geçildiğini ve request’in gerçek handler/precondition katmanına ulaştığını gösterdi. Request sahte UUID’lerle gönderildiği için başarılı DispatchRun oluşturulması beklenmedi.

## 5. Sandbox sınırlaması

Compose resolved config içinde `127.0.0.1:55432 → container:5432` port publish’i mevcut olmasına rağmen sandbox Docker host networking katmanında hosttan `55432` bağlantısı kurulamadı. Bu nedenle migration job’ı ve API smoke çağrıları aynı Docker internal network üzerinden yürütüldü. Şirket Ubuntu LTS sunucusunda loopback port publish davranışı ayrıca doğrulanmalıdır; bu sonuç ürün migration veya API health failure’ı değildir.

## 6. Deployment acceptance durumu

| Acceptance alanı | Durum |
|---|---|
| Temiz PostgreSQL Compose start/health | PASS |
| Migration zinciri ve B6 schema | PASS |
| Seed idempotency | PASS |
| API runtime container | PASS — geçici ASP.NET runtime container |
| Health/readiness endpoints | PASS |
| Real login ve B6 auth boundary | PASS |
| Production API Dockerfile/image | BLOCKED — repository’de Dockerfile yok |
| Full production Compose (`api`, `web`, `reverse-proxy`, `worker`, `backup`) | BLOCKED — mevcut Compose yalnızca PostgreSQL içeriyor |
| HTTPS/reverse proxy smoke test | BLOCKED — reverse proxy implementation yok |
| Backup/restore smoke test | BLOCKED — backup service/script implementation yok |
| Web/mobile LAN smoke test | BLOCKED — web/mobile implementation bu repository slice’ında yok |

Sonuç olarak **B6 migration + database + API runtime smoke testi PASS**, fakat deployment architecture belgesindeki tam production Compose acceptance henüz tamamlanmış değildir. Bu durum bir B6 kod hatası değil, deployment implementation kapsamının henüz üretilmemiş olmasıdır.

## 7. Önerilen sonraki deployment slice

Tam deployment kabulüne geçmeden önce ayrı bir deployment implementation slice açılmalıdır. Bu slice içinde API için multi-stage Dockerfile, production Compose servisleri, internal/public network ayrımı, reverse proxy, health dependency ordering, kontrollü migration job, secret injection, backup/restore job ve web/mobile LAN route’ları birlikte tasarlanmalıdır. Production’da API startup’ında otomatik migration çalıştırılmamalı; migrator kontrollü release adımı olarak çalıştırılmalıdır.

## References

[1]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/8e1bed6/deploy/compose.dev.yaml "FactoryErp development Compose PostgreSQL definition"

[2]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/8e1bed6/src/FactoryErp.Migrator/Program.cs "FactoryErp migration and seed entrypoint"

[3]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/8e1bed6/src/FactoryErp.Api/Program.cs "FactoryErp API health and authentication configuration"

[4]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/8e1bed6/design/implementation-g6.2-l4b6-cqrs-ef-skeleton.md "L4-B6 production implementation report"
