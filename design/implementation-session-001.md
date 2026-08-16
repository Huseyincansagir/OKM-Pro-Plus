# IMPLEMENTATION SLICE 001 — Domain Scaffold

**Tarih:** 2026-08-16

**Kapsam:** `FactoryErp.Domain` common/value objects, quantity/allocation invariants, Domain unit tests ve architecture dependency tests

**Durum:** PASS

## 1. Mevcut durum

Repository, `AGENTS.md`, `design/README.md`, `design/decision-log.md`, `design/implementation-readiness.md` ve `design/implementation-ready.md` üzerinden yeniden incelendi. Runtime `.claude/skills/factory-erp-implementation/SKILL.md` yolu mevcut repository’de bulunmadı; repository’nin `.claude/README.md` dosyası runtime skill paketinin bu klasörde bulunması gerektiğini belirtiyor. Aynı skill’in arşivlenmiş kopyası workspace içindeki `okm-remote-review` altında okunarak kurallar uygulandı.

`design/implementation-ready.md` mevcut gate için source of truth kabul edildi. Bu dosya implementation kapsamını açıkça şu sırayla sınırlar:

> `FactoryErp.Domain common/value objects → allocation invariants → Domain unit tests → Architecture dependency tests`

Mevcut repository’de G1–G5 kapsamındaki Application, Infrastructure, API ve migration kodları daha önceki commit’lerden zaten bulunmaktadır. Bu çalışma sırasında bu kapsam dışı kodlar yeniden yazılmadı, silinmedi ve yeni API, EF migration, PostgreSQL schema, web, mobile, worker veya production feature eklenmedi.

## 2. Karşılaştırma sonucu

Canonical Domain tasarımı ADR-001 ve ADR-002 doğrultusunda pozitif işlem miktarı için `PositiveQuantity`, sıfıra izin veren projection alanları için `NonNegativeQuantity`, immutable packaging/quantity snapshot’ları ve server-side temel birim dönüşümünü gerektiriyor. Mevcut implementation bu yapıların tamamını içeriyor.

Allocation tasarımı aktif allocation toplamının planned/source kalanını aşmamasını, reversal’ın negatif kayıt yerine pozitif miktarlı ayrı kayıt olarak tutulmasını ve geçersiz state transition’ların reddedilmesini gerektiriyor. Mevcut `DeliveryNoteItem` ve `DeliveryNoteItemAllocation` bu kuralları zaten uyguluyordu. Bu slice içinde eksik kalan iki açık test/guard tamamlandı: exact allocation boundary kabulü ve aynı allocation entity’sinin aynı delivery-note item’a tekrar eklenmesinin `DUPLICATE_ALLOCATION` hatasıyla reddedilmesi.

## 3. Değiştirilen dosyalar

| Dosya | Değişiklik | Kapsam |
|---|---|---|
| `src/FactoryErp.Domain/Shipping/DeliveryNoteItem.cs` | `DUPLICATE_ALLOCATION` guard’ı eklendi | Aynı allocation ID’sinin aynı aggregate child’a tekrar eklenmesini engeller |
| `tests/FactoryErp.Domain.UnitTests/Shipping/DeliveryNoteItemTests.cs` | Exact-boundary ve duplicate-allocation testleri eklendi | Allocation sınır ve duplicate davranışı |
| `tests/FactoryErp.ArchitectureTests/DomainDependencyTests.cs` | Yasak dependency listesi genişletildi | `FactoryErp.Application`, `FactoryErp.Infrastructure`, `FactoryErp.Api`, `Npgsql.EntityFrameworkCore.PostgreSQL` ve `Microsoft.Data` bağımlılıkları da açıkça engellenir |
| `design/implementation-session-001.md` | Bu evidence dokümanı eklendi | Slice sonucu ve completion gate kanıtı |

Önceki çalışma ağacında bulunan `design/database-technical-architecture.md`, `design/grok-session-review.md` ve `design/ui-mockup-review.md` değişikliklerine bu slice kapsamında dokunulmadı; bunlar önceki oturumdan kalan whitespace değişiklikleridir.

## 4. Uygulanan Domain davranışları

`PositiveQuantity` sıfır, negatif ve precision dışı miktarları reddeder. `NonNegativeQuantity` sıfır projection değerlerine izin verir ancak negatif sonuçları reddeder. `PackagingSnapshot`, `AllowPartial` kuralını ve temel birim dönüşümünü korur; kapalı ambalajda fractional quantity reddedilir. `QuantitySnapshot` girilen miktar, packaging, view mode ve base quantity snapshot’ını immutable olarak taşır.

`SalesOrder` ve `SalesOrderItem` draft, approval, preparing, partial shipment, fulfilled ve cancelled state kurallarını korur. Reservation, shipment, cancellation ve remaining quantity hesapları planned quantity sınırlarını aşamaz. Geçersiz transition’lar typed `DomainException` ile reddedilir.

`DeliveryNoteItem` aktif allocation toplamını planned quantity ve source remaining quantity ile sınırlar. Exact boundary, yani allocation miktarının hem source remaining hem de planned quantity’ye eşit olduğu durum kabul edilir. Boundary aşımı `OVER_ALLOCATION` ile reddedilir. Aynı allocation ID’sinin yeniden eklenmesi `DUPLICATE_ALLOCATION` ile reddedilir. Allocation reversal pozitif miktarlı yeni record ve `ReversedFromId` referansı ile modellenir; aktif olmayan allocation ikinci kez terslenemez.

## 5. Eklenen ve doğrulanan testler

Domain test paketi aşağıdaki zorunlu durumları kapsar:

| Test alanı | Sonuç |
|---|---:|
| Valid positive quantity | PASS |
| Zero quantity rejection | PASS |
| Negative quantity rejection | PASS |
| Precision/scale violation | PASS |
| Packaging base conversion | PASS |
| Closed-packaging fractional rejection | PASS |
| Exact allocation boundary | PASS |
| Over-allocation | PASS |
| Duplicate allocation | PASS |
| Invoice/waiver remaining boundary | PASS |
| Invalid state | PASS |
| Invalid transition | PASS |
| Reversal and repeated reversal | PASS |
| Domain event collection | PASS |

## 6. Verification sonuçları

İstenen komutlar gerçekten çalıştırıldı:

```text
dotnet restore
dotnet build FactoryErp.sln --configuration Release --no-restore
dotnet test FactoryErp.sln --configuration Release --no-restore
git diff --check
```

| Verification | Sonuç | Kanıt |
|---|---:|---|
| Domain implementation | PASS | Common/value object, sales ve shipping Domain implementation mevcut |
| Domain invariants | PASS | Quantity, shipment, allocation, invoiceable remainder ve reversal guard’ları geçiyor |
| Domain unit tests | PASS | 30/30 geçti |
| Architecture tests | PASS | 5/5 geçti |
| `dotnet restore` | PASS | Tüm projeler restore edildi/güncel |
| `dotnet build` | PASS | Release build, 0 warning, 0 error |
| `dotnet test` | PASS | Domain 30, Infrastructure 21, Architecture 5; toplam 56 test, 0 failure |
| Design consistency | PASS | O-001–O-014, ADR-001–ADR-011 ve implementation-ready gate değiştirilmedi |
| `git diff --check` | PASS | Whitespace ihlali yok |

FluentAssertions çalıştırılırken ticari lisans uyarısı gösterildi; bu uyarı test sonucunu başarısız yapmadı ve kurumsal kullanım politikası sonraki süreçte ayrıca değerlendirilmelidir.

## 7. Completion gate

```text
IMPLEMENTATION SLICE 001

STATUS: PASS

Domain: PASS
Invariants: PASS
Unit Tests: PASS
Architecture Tests: PASS
Build: PASS
Test: PASS
Design Consistency: PASS
```

## 8. Kalan riskler

Bu slice yalnızca Domain ve test sınırını kanıtlar. Domain invariant’larının EF Core mapping, PostgreSQL check constraint, transaction, row lock, concurrency, idempotency, audit ve outbox davranışlarıyla birlikte kanıtlanması sonraki persistence/application slice’ının sorumluluğudur.

Repository’de runtime `.claude/skills/factory-erp-implementation/SKILL.md` dosyasının bulunmaması, skill paketinin repository’ye eksik senkronize edilmiş olabileceğini gösterir. Bu durum mevcut Domain test gate’ini bloklamadı; ancak sonraki implementation slice’ına geçmeden skill runtime klasörü ile `docs/06-process-skill` arşiv kopyasının senkronizasyonu kontrol edilmelidir.

Ayrıca aynı aggregate içinde aynı allocation entity ID’siyle yapılan duplicate ekleme artık Domain seviyesinde engellenmektedir. Farklı ID’lerle gelen aynı payload’ın idempotency kontrolü ise command/application ve persistence katmanında yapılmalıdır; bu slice’ın bilinçli sınırı dışında bırakılmıştır.

## 9. Detaylı risk ve migration analizi

Bu rapordaki kalan riskler, migration baseline reconciliation, 0001–0018 sorumluluk sırası, allocation granularity tutarsızlığı, EF Core/PostgreSQL mapping gereksinimleri, concurrency/idempotency, rollback, seed ve persistence acceptance gate’leri [`implementation-session-001-risk-and-migration.md`](./implementation-session-001-risk-and-migration.md) içinde ayrıntılandırılmıştır.

## 10. Sonraki implementation slice

Bu slice başarıyla tamamlanmıştır. Kullanıcı talimatı gereği sonraki implementation slice otomatik başlatılmamıştır.

Design gate’e göre önerilen sonraki sınır `FactoryErp.Infrastructure` ve `FactoryErp.Application` persistence foundation’ıdır. Bu sınırda EF Core mapping, private collection access, PostgreSQL migration, quantity/allocation database constraints, row-version/concurrency ve gerçek PostgreSQL integration testleri ele alınmalıdır.

## References

[1]: ./implementation-ready.md "Factory ERP Implementation Ready Gate"
[2]: ./implementation-readiness.md "Factory ERP Implementation Readiness"
[3]: ./factoryerp-domain-code-design.md "FactoryErp.Domain Kod Tasarımı"
[4]: ./allocation-cqrs-unit-test-code-design.md "Allocation ve CQRS Unit Test Kod Tasarımı"
[5]: ../AGENTS.md "Factory ERP Agent Instructions"
