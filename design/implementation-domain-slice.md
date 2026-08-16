# Factory ERP — İlk Domain Implementation Slice

**Tarih:** 2026-08-16
**Aşama:** IMPLEMENTATION — DOMAIN SLICE COMPLETE
**Kapsam:** Yalnızca `FactoryErp.Domain`, Domain unit testleri ve Architecture dependency testleri

## 1. Amaç ve sınır

Bu slice, peçete fabrikası ERP’sinin sonraki Application, Infrastructure ve API katmanlarına temel oluşturacak **miktar, snapshot, sipariş state’i ve allocation invariant’larını** gerçek C# kodu ile kanıtlar. Tasarım kararları `ADR-001`–`ADR-011` ve `/design/` klasöründeki canonical belgelerden uygulanmıştır.

Bu aşamada ASP.NET Core API, EF Core `DbContext`, PostgreSQL migration, Docker Compose, Worker, web, mobil, public katalog veya dış e-belge adapter’ı eklenmemiştir. Bu sınır bilinçlidir; ilk slice’ın amacı transaction ve persistence davranışını değil, saf Domain kurallarını deterministik biçimde doğrulamaktır.

## 2. Gerçekleştirilen source tree

| Alan | Gerçek dosyalar | Sorumluluk |
|---|---|---|
| Common | `Common/Entity.cs`, `AggregateRoot.cs`, `DomainEvent.cs`, `DomainError.cs`, `DomainException.cs`, `DomainGuard.cs` | Entity kimliği, zaman alanları, row version placeholder’ı, event collection ve typed error |
| Shared | `Shared/QuantityTypes.cs` | `PositiveQuantity`, `NonNegativeQuantity`, `UomCode`, packaging ve quantity snapshot |
| Sales | `Sales/SalesOrder.cs`, `Sales/SalesOrderItem.cs`, `Sales/SalesOrderStatus.cs` | Draft → pending approval → approved → preparing → partial/fulfilled akışı; reservation ve shipment limitleri |
| Shipping | `Shipping/DeliveryNoteItem.cs` | Faturalanabilir miktar, allocation toplamı, source kalan miktarı ve pozitif reversal |
| Unit tests | `tests/FactoryErp.Domain.UnitTests/` | Quantity, snapshot, state, allocation ve reversal senaryoları |
| Architecture tests | `tests/FactoryErp.ArchitectureTests/` | Domain’in framework/infrastructure paketlerine bağımlı olmaması |

## 3. Kabul edilen invariant sonuçları

`PositiveQuantity` sıfır ve negatif işlem miktarlarını reddeder. `NonNegativeQuantity` projection alanlarında sıfıra izin verir fakat negatif sonucu reddeder. UOM scale 0–6 aralığında tutulur ve miktarın precision sınırını aşması typed domain error üretir.

`PackagingSnapshot` efektif katsayıyı ve ambalaj davranışını snapshot olarak taşır. Kapalı ambalajda fractional giriş reddedilir; girilen koli/paket/palet miktarı temel UOM’a çevrilirken sonuç `PositiveQuantity` olur. Snapshot’ın public setter’ı yoktur ve geçmiş miktar master ambalaj katsayısı değiştiğinde yeniden yorumlanmaz.

`SalesOrderItem`, rezervasyon ve sevkiyat miktarlarının kalan sipariş miktarını aşmasını engeller. Partial delivery kapalıysa kalan miktardan küçük sevk reddedilir; son sevkte durum `Fulfilled`, ara sevkte `PartiallyShipped` olur. `SalesOrder` yalnızca kalem içeren taslağı approval’a gönderebilir ve yalnızca approval bekleyen siparişi onaylayabilir.

`DeliveryNoteItem`, aktif allocation toplamını kendi planlanan miktarı ve source kalan miktarı ile karşılaştırır. Aşım durumunda `OVER_ALLOCATION` üretilir. Reversal, negatif miktar yazmak yerine pozitif miktarlı yeni allocation kaydı ve `ReversedFromId` referansı ile modellenir; orijinal kayıt ayrıca `Reversed` durumuna alınabilir.

## 4. Test evidence

| Gate | Sonuç | Kanıt |
|---|---:|---|
| Release build | PASS | `dotnet build FactoryErp.sln --configuration Release` |
| Domain unit tests | PASS | 28/28 test |
| Architecture tests | PASS | 2/2 test |
| Domain framework leakage | PASS | ASP.NET Core, EF Core, Npgsql, Dapper ve `System.Data` bağımlılığı bulunmadı |
| Database/API integration | BEKLENEN | Bu slice kapsamı dışında; Infrastructure slice’ında yapılacak |

Test grupları şu kabul senaryolarını kapsar: `QTY-001`–`QTY-006`, snapshot immutability, `ALLOC-001`, `ALLOC-002`, `ALLOC-003`, partial shipment, fulfilled state, invalid order transition, typed reversal error ve domain event collection.

> FluentAssertions paketinin ticari kullanım lisans uyarısı test çalıştırma sırasında görünmektedir. İlk slice için testler başarılıdır; repository’nin ticari kullanım politikasında lisans değerlendirmesi Infrastructure/API geliştirmesine geçmeden önce yapılmalıdır.

## 5. Bilinçli olarak sonraki slice’a bırakılan konular

Row lock, `SELECT FOR UPDATE`, EF private backing-field mapping, PostgreSQL numeric/JSONB/deferred constraint, `row_version` trigger, idempotency key + payload hash, concurrency conflict mapping, transaction rollback, outbox insert/publish ve ProblemDetails mapping bu Domain koduna taşınmamıştır. Bunlar `FactoryErp.Infrastructure` ve `FactoryErp.Application` slice’ında gerçek PostgreSQL üzerinde kanıtlanacaktır.

Özellikle Domain’in `RowVersion` alanı persistence concurrency mekanizmasının yerine geçmez. `row_version bigint` trigger’ı, ETag/If-Match ve typed concurrency response’ları Infrastructure/API katmanında uygulanacaktır. Aynı şekilde Domain allocation upper-bound kontrolü, database transaction içindeki lock/re-read/deferred guard mekanizmasının yerine geçmez.

## 6. Sonraki uygulama sırası

Bir sonraki slice `FactoryErp.Infrastructure`, `FactoryErp.Application` ve `FactoryErp.Migrator` project’lerinin eklenmesiyle başlayacaktır. Öncelik sırası; EF Core mapping ve private collection access, PostgreSQL migration 0001–0008, row version trigger, allocation constraint, `IssueDeliveryNote` CQRS transaction akışı ve gerçek PostgreSQL integration testleridir. Bu adım tamamlanmadan web, mobil ve public katalog yüzeylerinin production API’ye bağlanması başlatılmayacaktır.
