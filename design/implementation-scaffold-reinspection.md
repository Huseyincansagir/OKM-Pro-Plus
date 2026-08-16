# İlk Implementation Scaffold Slice — Yeniden Keşif Raporu

**Tarih:** 2026-08-16

**Kapsam:** Yalnızca `FactoryErp.Domain`, allocation/quantity invariant’leri, Domain unit testleri ve architecture dependency testleri

**Sonuç:** İlk scaffold slice mevcut repository baseline’ında zaten tamamlanmış ve yeniden doğrulanmıştır.

## 1. Okunan canonical kaynaklar

Repository-level [`AGENTS.md`](../AGENTS.md), [`.claude/README.md`](../.claude/README.md), [`design/README.md`](./README.md), [`design/decision-log.md`](./decision-log.md), [`design/implementation-readiness.md`](./implementation-readiness.md) ve [`design/implementation-ready.md`](./implementation-ready.md) okundu. Repository içinde `.claude/skills/*` altında ayrıca okunabilir bir skill dosyası bulunmadı; `.claude/README.md`, bu klasörün canonical skill paketi olması gerektiğini, dokümantasyon kopyasının `docs/06-process-skill/` altında tutulduğunu belirtmektedir.

Design baseline’ı ilk slice’ı şu sınırlarla tanımlar:

> `FactoryErp.Domain common/value objects → allocation invariants → Domain unit tests → Architecture dependency tests`

Bu sınır içinde API, EF Core migration, PostgreSQL persistence, web, mobile, worker veya external adapter geliştirilmemelidir. Mevcut repository daha sonraki Infrastructure, Application ve API slice’larını da içeriyor; bu yeniden keşif sırasında söz konusu feature’lara yeni kod eklenmemiş ve mevcut architecture baseline değiştirilmemiştir.

## 2. Mevcut repository bulguları

| İncelenen alan | Durum | Kanıt |
|---|---|---|
| Domain common layer | Mevcut | `src/FactoryErp.Domain/Common/` altında Entity, AggregateRoot, DomainEvent, DomainError, DomainException ve DomainGuard |
| Quantity/value objects | Mevcut | `src/FactoryErp.Domain/Shared/QuantityTypes.cs` altında PositiveQuantity, NonNegativeQuantity, UomCode, PackagingSnapshot ve QuantitySnapshot |
| Sales order invariants | Mevcut | `SalesOrder`, `SalesOrderItem` ve status transition’ları |
| Allocation invariants | Mevcut | `Shipping/DeliveryNoteItem.cs` altında allocation upper-bound, source remaining, invoiceable remainder ve reversal semantiği |
| Domain tests | Mevcut | Quantity, snapshot, order state, reservation/shipment, allocation, invoicing ve reversal testleri |
| Architecture tests | Mevcut | Domain framework leakage testleri ve Application/Infrastructure/API layer dependency testleri |
| Domain project references | Uygun | `FactoryErp.Domain.csproj` içinde project/package reference yok |
| İstenen kapsam dışı feature’lar | Mevcut baseline’da var | G1–G5 commit’leriyle Infrastructure/Application/API/migration kodu daha önce eklenmiş; bu yeniden keşif slice’ında bunlara dokunulmadı |

## 3. Invariant coverage

`PositiveQuantity` sıfır/negatif miktarı ve precision/scale ihlalini reddeder. `NonNegativeQuantity` projection değerlerinde sıfıra izin verir, ancak negatif değer ve precision ihlalini reddeder. `PackagingSnapshot`, packaging conversion katsayısını ve partial davranışını immutable snapshot olarak taşır; kapalı ambalajda fractional giriş reddedilir ve temel birim miktarı server-side domain dönüşümüyle hesaplanır.

`SalesOrderItem`, reservation ve shipment miktarlarını kalan sipariş miktarıyla sınırlar. Partial delivery kapalıysa eksik miktarlı sevkiyat reddedilir; son sevkiyat `Fulfilled`, ara sevkiyat `PartiallyShipped` durumunu üretir. `DeliveryNoteItem`, aktif allocation toplamını hem planlanan delivery miktarı hem de kaynak kalan miktarıyla sınırlar. Invoice allocation ve waiver işlemleri `RemainingToInvoice` üst sınırını korur. Reversal negatif miktar yazmak yerine pozitif miktarlı yeni kayıt ve reversal referansı kullanır.

Bu senaryolar için testler mevcut olup ayrıca yeni invariant kodu eklenmesi gerekli görülmemiştir.

## 4. Çalıştırılan kalite kapısı

Aşağıdaki komutlar temiz bir yeniden doğrulama olarak çalıştırıldı:

```text
dotnet restore FactoryErp.sln
dotnet build FactoryErp.sln --configuration Release --no-restore
dotnet test tests/FactoryErp.Domain.UnitTests/FactoryErp.Domain.UnitTests.csproj --configuration Release --no-restore
dotnet test tests/FactoryErp.ArchitectureTests/FactoryErp.ArchitectureTests.csproj --configuration Release --no-restore
```

Sonuçlar aşağıdaki tabloda özetlenmiştir.

| Gate | Sonuç |
|---|---:|
| `dotnet restore` | PASS — tüm projeler güncel |
| Release solution build | PASS — 0 warning, 0 error |
| Domain unit tests | PASS — 28/28 |
| Architecture dependency tests | PASS — 5/5 |
| Domain → Infrastructure/API dependency ihlali | PASS — Domain framework/infrastructure leakage yok |
| Allocation/quantity invariant tests | PASS — mevcut Domain test paketinde kapsanmış |

Architecture test paketi tarihsel ilk slice kanıtındaki 2 testten daha geniş bir baseline’a ulaşmıştır. Mevcut 5 test; Domain’in ASP.NET Core, EF Core, Npgsql, Dapper ve `System.Data` bağımlılıklarını; Application’ın Infrastructure/API/framework adapter bağımlılıklarını; Infrastructure’ın API’ye ters bağımlılığını ve API’nin beklenen Application/Infrastructure referanslarını doğrulamaktadır.

## 5. Değişiklik kararı

İnceleme sonucunda Domain common/value object, allocation invariant, Domain unit test ve architecture dependency test kapsamlarında eksik veya başarısız bir madde bulunmadı. Bu nedenle minimum kod değişikliği **sıfırdır**. API, EF migration, web, mobile veya worker feature’ı geliştirilmemiştir.

Bu rapor, yeniden keşif ve doğrulama kanıtı olarak eklenmiştir. Mevcut canonical [`implementation-domain-slice.md`](./implementation-domain-slice.md) ilk slice’ın tarihsel tamamlanma kanıtı olarak korunmuştur; bu rapor ise mevcut daha geniş repository baseline’ında yapılan ikinci doğrulamayı ve 5 testlik güncel architecture kapsamını kaydeder.

## 6. Kalan riskler

İlk slice için kalite kapısı yeşildir. Ancak sonraki persistence/application slice’ına geçerken Domain testlerinde kanıtlanan invariant’ların EF Core mapping, PostgreSQL check constraint, transaction, concurrency, idempotency ve audit davranışlarıyla tekrar kanıtlanması gerekir. Bu rapor, daha sonraki G1–G5 kodunun production-ready olduğu anlamına gelmez; yalnızca istenen ilk Domain scaffold sınırının temiz olduğunu gösterir.

FluentAssertions çalıştırılırken ticari lisans uyarısı görünmektedir. Testler teknik olarak başarılıdır; ticari kullanım politikası sonraki ekip/kurum değerlendirmesinde ayrıca ele alınmalıdır.

## 7. Sonraki skill

İlk scaffold slice’ı başarıyla doğrulandığı için design gate’in önerdiği sonraki skill `factory-erp-implementation` olmalıdır. Bunun ilk alt dilimi `FactoryErp.Infrastructure` ve `FactoryErp.Application` persistence foundation’ıdır; ancak yeni slice başlatılırken mevcut `implementation-ready.md` sınırları, ADR kararları ve bu raporda belirtilen Domain invariant’ları korunmalıdır.
