# IMPLEMENTATION SESSION 003 — P-003 Sales Document Chain Closure

**Tarih:** 20 Ağustos 2026  
**Kapsam:** `Issued Quote → Draft SalesOrder` dönüşümü  
**Durum:** **PASS**  
**Sonraki slice:** **P-001 — Load-plan create/lock wizard**

## 1. Amaç ve mevcut durum

P-003 öncesinde `IssueQuoteAsync` yalnızca `Draft → Issued` geçişi yapıyor, stok rezervasyonu oluşturmuyor ve sipariş açmıyordu. Quote → SalesOrder dönüşümü için backend command, API endpoint, permission policy, idempotency scope, persistence ilişkisi, frontend client ve kullanıcı aksiyonu bulunmuyordu. Sales order response’unda kaynak teklif bilgisi de taşınmıyordu.

Bu session’da yalnızca mevcut satış belge zincirinin eksik closure adımı uygulandı. Issue davranışı değiştirilmedi; teklif kesinleştirme hâlâ stok rezervasyonu veya sipariş üretmiyor. Dönüşüm ayrı ve açık bir kullanıcı aksiyonu olarak tasarlandı.

## 2. Uygulanan iş kuralı

> Yalnızca `Quote.Status == "Issued"` olan teklif, atomik ve idempotent biçimde `SalesOrder.Status == "Draft"` siparişine dönüştürülebilir. Dönüşüm sırasında stok rezervasyonu yapılmaz; rezervasyon sipariş onayında gerçekleşir.

Dönüşüm transaction’ı önce quote satırını PostgreSQL `FOR UPDATE` ile kilitler. Quote durumu doğrulandıktan sonra aynı quote’a bağlı mevcut order aranır. Daha önce order oluşmuşsa aynı order response’u döndürülür; böylece farklı idempotency key ile yapılan ikinci çağrı da yeni belge üretmez. İlk dönüşümde `SourceQuoteId` set edilir, quote kalemlerindeki `QuantityBase`, `PackagingSnapshot`, `UnitPrice`, `TaxCode` ve fiyat snapshot’ı order kalemlerine server-side olarak aktarılır. `TotalNet`, `TotalTax` ve `TotalGross` backend tarafından hesaplanır.

Her başarılı yeni dönüşüm için `QuoteConvertedToOrder` audit event’i yazılır. Dönüşümde `StockReservationRecord` oluşturulmadığı ayrıca integration test ile doğrulanmıştır. Quote state’i değiştirilmez; `Issued` olarak kalır.

## 3. Değiştirilen dosyalar

| Dosya | Değişiklik | Sonuç |
|---|---|---|
| `src/FactoryErp.Application/Sales/SalesContracts.cs` | `SalesOrderDto` içine `SourceQuoteId` ve `SourceQuoteNumber`; `ISalesCommandService` içine `ConvertQuoteToSalesOrderAsync` | Application contract tamamlandı |
| `src/FactoryErp.Infrastructure/Persistence/Entities/SalesFoundationEntities.cs` | `SalesOrderRecord.SourceQuoteId` ve navigation | Quote → order persistence ilişkisi eklendi |
| `src/FactoryErp.Infrastructure/Persistence/Configurations/SalesFoundationConfigurations.cs` | Nullable FK ve filtered unique index | Bir quote’tan en fazla bir order |
| `src/FactoryErp.Infrastructure/Persistence/Migrations/20260820093028_AddSourceQuoteToSalesOrder.cs` | `source_quote_id`, FK ve unique partial index | Database schema güncellendi |
| `src/FactoryErp.Infrastructure/Persistence/Migrations/20260820093028_AddSourceQuoteToSalesOrder.Designer.cs` | Güncel EF model snapshot metadata’sı | Migration metadata tamamlandı |
| `src/FactoryErp.Infrastructure/Persistence/Migrations/FactoryErpDbContextModelSnapshot.cs` | Güncel model snapshot | EF model/persistence uyumu |
| `src/FactoryErp.Infrastructure/Persistence/Migrations/20260819140000_AddCustomerPricingDirectory.cs` | Eksik EF `DbContext`/`Migration` attribute’ları | Mevcut pricing migration’ı EF tarafından keşfedilebilir hale geldi |
| `src/FactoryErp.Infrastructure/Sales/SalesCommandService.cs` | Transaction, row lock, duplicate lookup, idempotency replay, order mapping ve audit | Gerçek conversion command uygulandı |
| `src/FactoryErp.Api/Authorization/PermissionPolicies.cs` | `QuoteConvert` policy | `permission:quote.convert` tanımlandı |
| `src/FactoryErp.Api/Program.cs` | Quote conversion policy registration | ASP.NET authorization wiring tamamlandı |
| `src/FactoryErp.Api/Controllers/QuotesController.cs` | `POST /api/v1/quotes/{quoteId}/convert` | API endpoint eklendi |
| `src/FactoryErp.Infrastructure/Authentication/IdentitySeeder.cs` | Permission ID `78` — `quote.convert` | `system_admin` seed tamamlandı |
| `apps/web/src/lib/sales/quotes.ts` | `canConvertToOrder`, `convertQuoteToOrder` | Web API client tamamlandı |
| `apps/web/src/lib/sales/orders.ts` | Source quote alanları ve mapper | Order response izlenebilirliği tamamlandı |
| `apps/web/src/components/sales/quote-detail.tsx` | Permission-gated button, confirmation dialog, loading/error, redirect | Türkçe conversion UX tamamlandı |
| `apps/web/src/components/sales/order-detail.tsx` | Kaynak teklif linki | Quote → order izlenebilirliği görünür oldu |
| `tests/FactoryErp.Infrastructure.UnitTests/Sales/QuoteConversionIntegrationTests.cs` | Valid, duplicate, idempotency, concurrent, invalid state, no-reservation, audit testleri | Backend conversion davranışı doğrulandı |
| `tests/FactoryErp.Infrastructure.UnitTests/Sales/QuoteConversionSecurityIntegrationTests.cs` | Gerçek `/api/v1/auth/login` sonrası permission denied testi | `quote.convert` sınırı doğrulandı |
| `tests/FactoryErp.Infrastructure.UnitTests/Persistence/FactoryErpDbContextModelTests.cs` | Source quote FK ve filtered unique index testi | EF model invariant’ı doğrulandı |
| `apps/web/src/components/sales/quote-detail.test.tsx` | Visibility, confirmation summary, conversion call ve order redirect | Quote UI doğrulandı |
| `apps/web/src/components/sales/order-detail.test.tsx` | Source quote link testi | Order UI doğrulandı |
| `apps/web/src/components/sales/order-list.test.tsx` | Yeni nullable mapper alanları | Fixture contract güncellendi |
| `apps/web/src/lib/sales/orders.test.ts` | Source quote mapper assertion’ları | API client regression coverage |
| `design/implementation-backlog.md` | P-003 `[x]`, sıradaki madde P-001 | Backlog güncellendi |

## 4. API ve authorization sözleşmesi

Yeni endpoint aşağıdaki sözleşmeyle çalışır:

```http
POST /api/v1/quotes/{quoteId}/convert
Authorization: Bearer <access-token>
Idempotency-Key: <required-key>
X-Correlation-Id: <optional-correlation-id>
```

Başarılı yeni dönüşümde HTTP `201 Created` ve `/api/v1/orders/{orderId}` Location değeri döner. Aynı quote daha önce dönüştürülmüşse mevcut order response’u tekrar edilir. Quote bulunamazsa `404`; quote `Issued` değilse domain error mapping’i üzerinden `422`; conversion permission’ı yoksa `403` döner.

`IdempotencyKeyMiddleware` içinde `/api/v1/quotes` prefix’i zaten mevcut olduğundan P-003 için middleware genişletmesi gerekmedi. `quote.convert` policy’si `Program.cs` içine bağlandı ve `IdentitySeeder` içine ID `78` ile eklendi.

## 5. Database ve migration notu

P-003 migration’ı aşağıdaki database nesnelerini ekler:

| Nesne | Kural |
|---|---|
| `sales_orders.source_quote_id` | Nullable UUID |
| `FK_sales_orders_quotes_source_quote_id` | Quote silinmesini `Restrict` eder |
| `IX_sales_orders_source_quote_id` | `UNIQUE`, yalnızca `source_quote_id IS NOT NULL` satırlarında |

Local PostgreSQL migration head’i doğrulanmıştır. Kodda mevcut olan `20260819140000_AddCustomerPricingDirectory` migration’ının EF discovery attribute’ları eksik olduğu için local migration listesinde görünmediği tespit edildi; P-003 conversion quote item pricing kolonlarına ihtiyaç duyduğu için bu mevcut migration’a yalnızca metadata attribute düzeltmesi yapıldı. Ardından pricing migration ve P-003 migration local database’e uygulandı.

## 6. Test kapsamı

P-003’e özel backend testleri gerçek PostgreSQL üzerinde çalıştırıldı:

| Senaryo | Sonuç |
|---|---:|
| Issued quote → Draft SalesOrder | PASS |
| Source quote id/number response mapping | PASS |
| `QuantityBase`, packaging ve fiyat snapshot aktarımı | PASS |
| Backend total hesapları | PASS |
| Stok rezervasyonu yapılmaması | PASS |
| `QuoteConvertedToOrder` audit event’i | PASS |
| Aynı idempotency key ile replay | PASS |
| Farklı key ile duplicate quote conversion | PASS |
| Concurrent conversion | PASS |
| Draft quote rejection | PASS |
| Gerçek `/auth/login` sonrası `quote.convert` permission denied | PASS |
| EF source quote FK/unique index modeli | PASS |

Web tarafında conversion button visibility, permission gate, confirmation summary, conversion çağrısı, loading akışı, error state, order redirect ve source quote linki test edildi.

## 7. Verification sonuçları

### P-003 completion gate

| Gate | Sonuç | Kanıt |
|---|---:|---|
| Backend persistence | PASS | EF migration ve local PostgreSQL schema doğrulandı |
| Backend conversion command | PASS | `QuoteConversionIntegrationTests`: 4/4 |
| Idempotency/concurrency | PASS | Replay, farklı key duplicate ve concurrent testleri geçti |
| Authorization | PASS | Gerçek login security testi: 1/1 |
| Audit/no reservation | PASS | Audit ve `StockReservations` assertions geçti |
| Frontend typecheck | PASS | `pnpm --dir apps/web typecheck` |
| Frontend lint | PASS | `pnpm --dir apps/web lint` |
| Frontend tests | PASS | **69 dosya / 209 test** |
| Frontend production build | PASS | `pnpm --dir apps/web build` |
| `dotnet restore` | PASS | Solution restore tamamlandı |
| `dotnet build` | PASS | Release solution build, 0 warning / 0 error |
| Domain unit tests | PASS | **129/129** |
| Architecture tests | PASS | **5/5** |
| `git diff --check` | PASS | Whitespace hatası yok |
| Backlog/docs | PASS | P-003 `[x]`, bu rapor oluşturuldu |

### Full repository test durumu

Full `dotnet test FactoryErp.sln --configuration Release --no-build` çalıştırmasında P-003 testleri başarılıdır. Domain ve architecture projeleri tamamen geçmiştir. Infrastructure test projesinde **88/90** test geçmiştir; iki failure P-003 öncesi baseline’da bulunan ve P-003 koduna bağlı olmayan testlerdir:

| Pre-existing failure | Durum |
|---|---|
| `PhysicalLogisticsIntegrationTests.Physical_master_creates_profiles_and_replays_idempotently` | Ürün fiziksel profil tarih aralığı çakışması; test isolation/profile fixture sorunu |
| `LogisticsSecurityIntegrationTests.Login_enforces_vehicle_driver_shipment_and_route_permission_boundary` | Mevcut physical profile hazırlık adımı `422` döndürüyor; P-003 route’u ile ilişkili değil |

Bu iki baseline failure’a P-003 kapsamında davranış değişikliği uygulanmadı. P-003’e özel backend, security, frontend, build, domain ve architecture gate’lerinin tamamı geçmiştir.

## 8. Kalan riskler ve sınırlar

İlk olarak, repository’nin iki pre-existing infrastructure failure’ı hâlâ açık durumdadır. Bunlar bu slice’ın iş kuralıyla ilişkili değildir ancak global CI’ın tamamen yeşil raporlanmasını engeller; ayrı bir test isolation/security hardening çalışmasında ele alınmalıdır.

İkinci olarak, P-003 API success path’i service integration testleriyle ve endpoint authorization boundary’si gerçek login testiyle doğrulanmıştır; gerçek browser üzerinden canlı API çağrısı yapılmamıştır. UI tarafında API çağrısı mocked component testleriyle doğrulanmıştır. Production smoke testinde issued quote oluşturma, convert endpoint’i ve order detail source linki birlikte kontrol edilmelidir.

Üçüncü olarak, quote expiration için yeni bir P-003 kuralı eklenmemiştir. Conversion gate’i mevcut karar doğrultusunda yalnızca `Issued` state’ine dayanır. ValidUntil üzerinden ayrıca expiration enforcement kararı alınacaksa ayrı bir design decision ve test slice’ı açılmalıdır.

## 9. Completion summary

```text
IMPLEMENTATION SLICE P-003

STATUS: PASS

Backend Persistence: PASS
Conversion Transaction: PASS
Idempotency: PASS
Concurrency: PASS
Audit: PASS
Authorization: PASS
Frontend UI: PASS
Domain Tests: PASS
Architecture Tests: PASS
Web Typecheck: PASS
Web Lint: PASS
Web Tests: PASS — 209/209
Web Build: PASS
Dotnet Restore: PASS
Dotnet Build: PASS
Dotnet Full Test: PASS WITH 2 PRE-EXISTING BASELINE FAILURES
Docs/Backlog: PASS

Next Slice: P-001 — Load-plan create/lock wizard
```

P-003 tamamlanmadan P-001 kodlamasına başlanmayacaktır; backlog sırası güncellenmiş ve bu session burada durdurulmuştur.
