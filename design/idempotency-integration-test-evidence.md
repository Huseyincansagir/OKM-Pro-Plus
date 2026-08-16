# Merkezi Idempotency Replay ve Mismatch Integration Test Kanıtı

**Tarih:** 2026-08-16

## Kapsam

Bu test dilimi, merkezi `IIdempotencyStore`/`EfIdempotencyStore` davranışını gerçek PostgreSQL veritabanı üzerinde, `DeliveryInvoiceFinanceService` command sınırından doğrular. Testler yalnızca aynı `scope + Idempotency-Key` kaydını kullanır; test anahtarları her çalışmada benzersiz üretildiği için mevcut fixture veya smoke verileriyle çakışmaz. Her test sonunda kendi idempotency kaydını siler.

## Test senaryoları

| Test | Hazırlık | Beklenen sonuç | Durum |
|---|---|---|---|
| `Central_store_replays_same_payload_and_response_through_delivery_command` | Merkezi EF store’a aynı scope/key/payload hash ile serialized `DeliveryNoteDto` kaydedilir | Aynı command yeniden çağrıldığında service database mutation yapmadan stored response’u deserialize edip döndürür; kayıt sayısı bir kalır | PASS |
| `Central_store_rejects_same_key_with_different_payload_through_delivery_command` | Aynı scope/key için ilk payload hash kaydedilir, ikinci request quantity değerini değiştirir | Service `IDEMPOTENCY_PAYLOAD_MISMATCH` DomainException üretir; stored kayıt değişmeden bir adet kalır | PASS |

Replay testi, command’ın replay kontrolünü order lookup ve transaction mutation’ından önce yaptığını da doğrular. Mismatch testi ise payload hash’in yalnızca key’e değil, request payload’a bağlı olduğunu kanıtlar.

## Teknik uygulama

Test dosyası:

```text
tests/FactoryErp.Infrastructure.UnitTests/Idempotency/IdempotencyIntegrationTests.cs
```

Testlerde doğrudan `FactoryErpDbContext` + Npgsql kullanılır. `EfIdempotencyStore` gerçek `idempotency_records` tablosuna yazıp okur. Finance command’ın audit ve product catalog bağımlılıkları yalnızca replay/mismatch noktasına ulaşılabilen no-op test doubles ile sağlanır; replay/mismatch gerçekleştiğinde product catalog, order lookup veya audit command yolu çalıştırılmaz.

Test bağlantısı aşağıdaki non-production connection string üzerinden sağlanmıştır:

```text
Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me
```

Production secret kullanılmamış ve repository’ye secret eklenmemiştir. `FactoryErpTestConnectionString` veya `ConnectionStrings__FactoryErp` environment variable’ları ile bağlantı override edilebilir.

## Çalıştırılan doğrulamalar

```text
Focused idempotency integration tests: 2/2 PASS
Domain unit tests: 31/31 PASS
Infrastructure unit tests: 23/23 PASS
Architecture tests: 5/5 PASS
Release build: PASS — 0 warning, 0 error
dotnet restore: PASS
dotnet test FactoryErp.sln: PASS — 59/59
Whitespace validation: PASS — git diff --check
```

Focused test komutu:

```bash
export ConnectionStrings__FactoryErp='Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me'
dotnet test tests/FactoryErp.Infrastructure.UnitTests/FactoryErp.Infrastructure.UnitTests.csproj \
  --configuration Release \
  --filter FullyQualifiedName~IdempotencyIntegrationTests
```

## Kalan riskler

Bu testler aynı transaction’ın iki concurrent writer tarafından yarışmasını henüz doğrulamaz. Merkezi idempotency tablosundaki `(scope, key)` unique constraint’i için ayrı bir two-connection race testi eklenmelidir. Ayrıca production seviyesinde response body’nin versioned contract olarak saklanması, expired kayıtların temizlenmesi, failed mutation sonrası idempotency kaydının ne zaman yazılacağı ve distributed deployment senaryosunda transaction boundary davranışı ayrıca kanıtlanmalıdır.

## Sonuç

Merkezi idempotency replay ve payload mismatch davranışı gerçek PostgreSQL üzerinde başarıyla doğrulanmıştır. Aynı scope/key ve aynı payload için response replay edilir; aynı key farklı payload ile tekrar kullanıldığında `IDEMPOTENCY_PAYLOAD_MISMATCH` döner ve mevcut idempotency kaydı değiştirilmez.
