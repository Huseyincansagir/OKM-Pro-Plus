# G6.1 Üretimden Stoğa ve Login Security Integration Evidence

**Tarih:** 2026-08-16
**Durum:** PASS — source slice restored and authenticated API security verified

## 1. Amaç ve kaynak durumu

G5 hardening commit’leri sonrasında G6.1 source slice’ının çalışma ağacında bulunmadığı tespit edildi. `HEAD` `de4b16c` üzerinde kaldı; production Domain, Application, Infrastructure ve API dosyaları commit edilmemiş önceki çalışma ağacından korunmamıştı. Kullanıcı onayıyla G6.1 source slice mevcut production/stock persistence tabloları ve canonical üretim–depo kararları kullanılarak yeniden oluşturuldu.

Bu çalışma yeni bir production schema tasarlamak yerine mevcut `production_orders`, `production_records`, `stocks` ve `stock_movements` persistence modelini yeniden kullandı. Mevcut migration geçmişi değiştirilmedi.

## 2. Restored G6.1 scope

Slice şu bounded flow’u kapsar:

```text
ProductionOrder create
→ Release
→ Start
→ ProductionRecord create
→ Complete
→ StockMovement ProductionIn
→ finished-good stock increase
```

Domain state machine `Planned → Released → InProgress → Completed` akışını; `Paused → InProgress` ve `Planned/Released → Cancelled` sınırlarını korur. Planned ve production quantities positive olmalıdır. Completed quantity planı aşamaz. Production record yalnızca `InProgress` iş emrine eklenebilir. Completion, en az bir production record olmadan yapılamaz.

Üretim kaydında packaging seçilmişse `quantityBase` server-side `IProductCatalogService.PreviewQuantityAsync` ile yeniden hesaplanır. Test fixture’ında `1 Koli → 2.000 adet` dönüşümü gerçek seeded packaging üzerinden doğrulanmıştır.

## 3. Login security integration test

Yeni test dosyası:

```text
tests/FactoryErp.Infrastructure.UnitTests/Production/ProductionSecurityIntegrationTests.cs
```

Test `WebApplicationFactory<Program>` ile gerçek API middleware pipeline’ını başlatır ve aşağıdaki akışı `/api/v1/auth/login` üzerinden doğrular:

| Senaryo | Beklenen/gerçek sonuç |
|---|---|
| Anonymous `GET /api/v1/production/orders/{id}` | `401 Unauthorized` |
| Full production user login | `200 OK`, access/refresh token üretildi |
| Login token permission claim’leri | `production.create/read/start/record/complete` mevcut |
| Full user production create | `201 Created` |
| Full user production read | `200 OK` |
| Read-only user login | `200 OK`, yalnızca `production.read` claim’i |
| Read-only user production read | `200 OK` |
| Read-only user production create | `403 Forbidden` |

Test fixture’ı gerçek `PasswordHasher`, `AuthenticationService`, JWT bearer validation, authorization policy, `IdempotencyKeyMiddleware`, exception middleware, EF Core `FactoryErpDbContext` ve PostgreSQL kullanır. Full test user system-admin role’üne; read-only test user yalnızca `production.read` permission’ına bağlanır. Fixture kullanıcı, rol, role-permission, refresh token, production order ve idempotency kayıtlarını test sonunda temizler.

Bu test sırasında production permission seed’lerinin source kodda eksik olduğu da tespit edilerek `IdentitySeeder` içine aşağıdaki idempotent definitions eklendi:

```text
production.create
production.read
production.start
production.record
production.complete
```

API policy registry ve `Program.cs` authorization registration aynı permission set ile güncellendi.

## 4. Değişen dosyalar

| Dosya | Değişiklik |
|---|---|
| `src/FactoryErp.Domain/Production/ProductionOrder.cs` | Production aggregate, status transitions ve quantity guards |
| `tests/FactoryErp.Domain.UnitTests/Production/ProductionOrderTests.cs` | Positive/zero/negative, exact boundary, over-allocation, invalid transition ve completion invariant testleri |
| `src/FactoryErp.Application/Production/ProductionContracts.cs` | Production requests, DTO’lar ve command service contract’ı |
| `src/FactoryErp.Infrastructure/Production/ProductionCommandService.cs` | Transactional production command implementation |
| `src/FactoryErp.Infrastructure/DependencyInjection.cs` | Production service registration |
| `src/FactoryErp.Api/Controllers/ProductionController.cs` | Authenticated production routes |
| `src/FactoryErp.Api/Authorization/PermissionPolicies.cs` | Production policy constants |
| `src/FactoryErp.Api/Program.cs` | Production policy registrations |
| `src/FactoryErp.Infrastructure/Authentication/IdentitySeeder.cs` | Production permission seed definitions |
| `tests/FactoryErp.Infrastructure.UnitTests/Production/ProductionModelTests.cs` | EF production constraint/row-version assertions |
| `tests/FactoryErp.Infrastructure.UnitTests/Production/ProductionIntegrationTests.cs` | PostgreSQL record/completion/stock/replay test |
| `tests/FactoryErp.Infrastructure.UnitTests/Production/ProductionSecurityIntegrationTests.cs` | Real `/auth/login` authorization integration test |
| `tests/FactoryErp.Infrastructure.UnitTests/FactoryErp.Infrastructure.UnitTests.csproj` | `Microsoft.AspNetCore.Mvc.Testing` package ve API project reference |

## 5. Migration decision

G6.1 source restoration mevcut schema ile çalışır; yeni tablo veya kolon eklenmedi. Bu nedenle yeni EF Core migration üretilmedi. Controlled Migrator, mevcut migration’ları değiştirmeden production permission seed’lerini idempotent biçimde uyguladı.

## 6. Verification

| Kontrol | Sonuç |
|---|---|
| `dotnet restore FactoryErp.sln` | PASS |
| Release `dotnet build` | PASS — 0 warning, 0 error |
| Domain unit tests | PASS — 41/41 |
| Infrastructure tests | PASS — 29/29 |
| Architecture dependency tests | PASS — 5/5 |
| Full solution tests | PASS — 75/75 |
| `git diff --check` | PASS |
| Controlled Migrator + production permission seed | PASS |
| Real `/auth/login` security integration | PASS — 1/1 |

## 7. Kalan riskler

G6.1 source restoration ve login security testi başarıyla tamamlandı; ancak dosyalar henüz yeni bir commit’e alınmadı. Bu nedenle ilk operasyonel adım source slice ve security testlerini ayrı bir commit olarak kaydetmektir.

Production-specific two-connection completion race testi henüz yoktur. G5 current-account concurrency testi mevcut olsa da stock row create/lock ve aynı production order’ın eşzamanlı completion davranışı ayrıca kanıtlanmalıdır. Production API controller’da `If-Match`/ETag guard’ı da bu slice’a dahil edilmemiştir.

Login test gerçek authentication pipeline’ını kullanmıştır; ancak test user fixture’ı doğrudan PostgreSQL’e seed edildiği için gerçek bootstrap admin password rotasyonu ve refresh-token rotation ayrı authentication regression testlerinde ayrıca korunmalıdır.

Fire, machine assignment/downtime, personnel time, lot/serial, BOM/raw-material consumption, warehouse count/transfer ve web/mobile UI bu slice’ın dışındadır.

## 8. Sonraki aşama

Bu security doğrulamasından sonraki kontrollü alt-slice, G6.1 source ve testlerinin commit edilmesi; ardından production-specific concurrency race ve ETag/If-Match hardening testlerinin eklenmesidir. Kullanıcı onayı olmadan G6.2 machine/downtime implementation’ına geçilmeyecektir.
