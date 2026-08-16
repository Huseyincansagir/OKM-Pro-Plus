# G5 — Sevkiyat, İrsaliye, Fatura, Ödeme ve Cari Hesap

**Durum:** Tamamlandı ve doğrulandı

**Tarih:** 2026-08-16

**Gate:** G5 — Financial/Fulfillment Acceptance

**Yazar:** Manus AI

## 1. Amaç ve kapsam

G5 dilimi, onaylanmış satış siparişindeki rezerve miktarın irsaliye üzerinden fiziksel stoktan düşülmesini, issued irsaliye kalemlerinin kısmi veya tam olarak faturalanmasını, issued faturanın cari hesaba borç kaydı oluşturmasını ve tahsilatın fatura/cari hesaba alacak olarak uygulanmasını kapsar. Akış, O-002 ve O-003 kararlarında kabul edilen miktar sözleşmesine göre aşağıdaki sırayı korur:

> `SalesOrder → StockReservation → DeliveryNote → Invoice → CurrentAccount → Payment`

Bu dilimde fatura issue stok hareketi üretmez. Stok hareketi yalnızca irsaliye issue sırasında üretilir; fatura issue ise irsaliye allocation kalanını günceller ve cari borç hareketi oluşturur.

## 2. Uygulanan backend bileşenleri

| Katman | Bileşen | Sorumluluk |
|---|---|---|
| Application | [`ShippingFinanceContracts.cs`](../src/FactoryErp.Application/Shipping/ShippingFinanceContracts.cs) | Delivery note, invoice, payment ve current-account request/response sözleşmeleri |
| Infrastructure | [`DeliveryInvoiceFinanceEntities.cs`](../src/FactoryErp.Infrastructure/Persistence/Entities/DeliveryInvoiceFinanceEntities.cs) | DeliveryNote, allocation, invoice, tax, current-account, payment entity’leri |
| Infrastructure | [`DeliveryInvoiceFinanceConfigurations.cs`](../src/FactoryErp.Infrastructure/Persistence/Configurations/DeliveryInvoiceFinanceConfigurations.cs) | EF Core mapping, index ve database check constraint’leri |
| Infrastructure | [`DeliveryInvoiceFinanceService.cs`](../src/FactoryErp.Infrastructure/Shipping/DeliveryInvoiceFinanceService.cs) | Transactional command service, quantity preview, allocation, stock ledger ve cari ledger işlemleri |
| API | [`DeliveryNotesController.cs`](../src/FactoryErp.Api/Controllers/DeliveryNotesController.cs) | İrsaliye oluşturma, detay ve issue endpoint’leri |
| API | [`InvoicesController.cs`](../src/FactoryErp.Api/Controllers/InvoicesController.cs) | Fatura oluşturma, detay ve issue endpoint’leri |
| API | [`PaymentsController.cs`](../src/FactoryErp.Api/Controllers/PaymentsController.cs) | Payment apply endpoint’i |
| API | [`CurrentAccountsController.cs`](../src/FactoryErp.Api/Controllers/CurrentAccountsController.cs) | Müşteri bazlı cari hesap özeti |
| API | [`Program.cs`](../src/FactoryErp.Api/Program.cs) | G5 permission policy kayıtları |
| Infrastructure | [`IdentitySeeder.cs`](../src/FactoryErp.Infrastructure/Authentication/IdentitySeeder.cs) | System-admin rolüne G5 permission seed’i |
| Infrastructure | [`FinanceSeeder.cs`](../src/FactoryErp.Infrastructure/Shipping/FinanceSeeder.cs) | VAT ve payment method referans seed’i |
| Persistence | [`20260816114953_AddDeliveryInvoiceAndCurrentAccount.cs`](../src/FactoryErp.Infrastructure/Persistence/Migrations/20260816114953_AddDeliveryInvoiceAndCurrentAccount.cs) | G5 PostgreSQL migration’ı |

## 3. API sözleşmesi

Tüm mutation endpoint’leri `Idempotency-Key` header’ı ile çağrılır. Kimlik doğrulama JWT ile, erişim ise endpoint’e özel permission claim’i ile yapılır. Aktör ve korelasyon bilgisi transaction içindeki audit kaydına aktarılır.

| Method | Endpoint | Permission | Davranış |
|---|---|---|---|
| `POST` | `/api/v1/delivery-notes` | `delivery-note.create` | Sales order kalemlerinden draft irsaliye oluşturur; `quantityBase` sunucuda hesaplanır |
| `GET` | `/api/v1/delivery-notes/{id}` | `delivery-note.read` | İrsaliye ve kalem allocation görünümünü döndürür |
| `POST` | `/api/v1/delivery-notes/{id}/issue` | `delivery-note.issue` | Reservation ve stock lock altında stok düşer, delivery allocation yazılır |
| `POST` | `/api/v1/invoices` | `invoice.create` | Issued irsaliyeden draft fatura ve fiyat/vergi snapshot’ı oluşturur |
| `GET` | `/api/v1/invoices/{id}` | `invoice.read` | Fatura ve fatura kalemlerini döndürür |
| `POST` | `/api/v1/invoices/{id}/issue` | `invoice.issue` | Invoice allocation ve cari debit hareketini aynı transaction’da commit eder |
| `POST` | `/api/v1/payments` | `payment.apply` | Ödeme kaydını, varsa invoice allocation’ı ve cari credit hareketini commit eder |
| `GET` | `/api/v1/current-accounts/{customerId}` | `current-account.read` | Müşteri cari özeti, debit, credit ve balance değerlerini döndürür |

## 4. Miktar ve allocation kuralları

İrsaliye oluştururken kullanıcı tarafından girilen paketleme seviyesi `enteredQuantity` ve `enteredPackagingId` ile taşınır; temel birim miktarı server-side quantity preview üzerinden hesaplanır. Örneğin seed edilmiş `Koli = 2.000 adet` dönüşümünde iki koli, `quantityBase = 4.000` olarak kaydedilmiştir. İstemcinin gönderdiği bir temel miktar alanı kabul edilmez.

İrsaliye issue sırasında aşağıdaki kontroller transaction içinde yeniden uygulanır: aktif stok rezervasyonunun bulunması, delivery miktarının reservation kalanını aşmaması, fiziksel stok ve rezerve stok yeterliliği, satış siparişi shipped/remaining projection değerlerinin korunması ve allocation idempotency anahtarının benzersizliği. Başarılı issue işleminde `DeliveryNoteItemAllocation`, `StockMovement(DeliveryIssue)`, reservation tüketimi ve stok düşümü birlikte commit edilir.

Fatura create yalnızca `DeliveryNote.Status = Issued` kaynaklarından çalışır. Fatura kalemi, ilgili irsaliye kaleminin `RemainingToInvoice` değerini aşamaz. Fatura issue sırasında `InvoiceItemAllocation` oluşturulur; `invoiced_qty + waived_qty <= shipped_qty` ve `remaining_to_invoice = shipped_qty - invoiced_qty - waived_qty` kuralları korunur. Aynı fatura kaleminin aynı delivery note kalemine tekrar bağlanması unique index ile engellenir.

Cari hesapta invoice issue debit, payment apply credit üretir. `CurrentTransaction` için database seviyesinde tam olarak bir tarafın pozitif olmasını zorunlu kılan `ck_current_transactions_one_side` check constraint’i bulunmaktadır. Böylece aynı hareketin hem borç hem alacak yazılması veya iki tarafın sıfır kalması engellenir.

## 5. Migration ve seed

`20260816114953_AddDeliveryInvoiceAndCurrentAccount` migration’ı aşağıdaki tabloları ve ilişkileri ekler: `delivery_notes`, `delivery_note_items`, `delivery_note_item_allocations`, `tax_codes`, `invoices`, `invoice_items`, `invoice_item_allocations`, `current_accounts`, `current_transactions`, `payment_methods`, `payments` ve `payment_allocations`.

Controlled migrator üzerinde migration başarıyla uygulanmış ve G5 reference seed’leri idempotent biçimde çalıştırılmıştır. Seed edilen veriler aşağıdaki gibidir.

| Tür | Kod | Açıklama | Değer |
|---|---|---|---:|
| Tax code | `VAT20` | `%20 KDV` | `0.20` |
| Tax code | `VAT0` | `%0 KDV` | `0.00` |
| Payment method | `BANK` | Banka Havalesi | Aktif |
| Payment method | `CASH` | Nakit | Aktif |

System-admin permission seed’i şu izinleri içerir: `delivery-note.create`, `delivery-note.read`, `delivery-note.issue`, `invoice.create`, `invoice.read`, `invoice.issue`, `payment.apply` ve `current-account.read`.

## 6. Test ve smoke kanıtı

Yeni persistence model testleri [`DeliveryInvoiceFinanceModelTests.cs`](../tests/FactoryErp.Infrastructure.UnitTests/Shipping/DeliveryInvoiceFinanceModelTests.cs) içinde beş test olarak eklendi. Testler delivery-note invoiced/remaining projection guard’larını, delivery allocation quantity ve unique idempotency key’ini, invoice allocation quantity ve unique idempotency key’ini, current transaction one-side guard’ını ve current-account unique/precision modelini doğrular.

Focused G5 model test sonucu aşağıdaki gibidir.

| Test grubu | Sonuç |
|---|---:|
| `DeliveryInvoiceFinanceModelTests` | 5 geçti, 0 başarısız |
| Release solution build | 0 warning, 0 error |
| G5 migration history | 1 kayıt, uygulandı |
| Tax code seed | 2 kayıt |
| Payment method seed | 2 kayıt |

Uçtan uca smoke akışında mevcut approved/reserved siparişten iki koli için draft irsaliye oluşturulmuş, irsaliye issue edilmiş, irsaliyedeki bir koli için draft fatura oluşturulmuş ve issue edilmiştir. Bir koli `2.000 base unit` olarak hesaplanmış; `0,11 TRY/base unit` fiyat ve `%20 KDV` ile `220,00 TRY` ara toplam, `44,00 TRY` vergi ve `264,00 TRY` genel toplam elde edilmiştir.

| Smoke adımı | Beklenen | Gerçekleşen |
|---|---|---|
| Delivery create | Draft, `quantityBase = 4.000` | Başarılı, HTTP `201` |
| Delivery issue | Stok ve reservation düşümü | Başarılı, HTTP `200` |
| Invoice create | Draft, `quantityBase = 2.000` | Başarılı, HTTP `201` |
| Invoice issue | Issued, cari debit `264,00` | Başarılı, HTTP `200` |
| Payment apply | Applied, cari credit `264,00` | Başarılı, HTTP `200` |
| Current account read | Balance `0,00` | Başarılı, HTTP `200` |

Database readback kanıtı şöyledir: fatura durumu `Paid`, fatura toplamı `264,00`, payment allocation toplamı `264,00`; current transactions sırasıyla `InvoiceIssued debit = 264,00` ve `PaymentApplied credit = 264,00`; irsaliye kaleminde `shipped_qty = 4.000`, `invoiced_qty = 2.000`, `remaining_to_invoice = 2.000`; smoke sonrasında stok `on_hand = 14.000`, `reserved = 6.000` olarak okunmuştur. Bu değerler kısmi sevkiyat ve kısmi fatura sözleşmesinin korunduğunu gösterir.

## 7. Kalite kapısı

G5 kapanışı öncesinde solution Release build’i, odaklı G5 persistence testleri ve PostgreSQL migration/smoke doğrulaması çalıştırılmıştır. Final gate için tüm solution testleri ve `git diff --check` ayrıca çalıştırılacaktır. G5 commit’ine yalnızca kaynak kodu, migration, test ve bu evidence dokümanı alınmalı; çalışma ağacında G5 öncesinden kalan tasarım dokümanı düzenlemeleri ayrı gözden geçirilmelidir.

## References

[1]: ../design/partial-shipment-invoicing-workflow.md "O-002/O-003 kısmi sevkiyat ve faturalama iş akışı"
[2]: ../design/architecture-api-contracts.md "ASP.NET Core endpoint sözleşmeleri"
[3]: ../design/mvp-coding-plan.md "MVP coding roadmap ve G5 gate tanımı"
[4]: ../src/FactoryErp.Infrastructure/Shipping/DeliveryInvoiceFinanceService.cs "G5 transactional shipping-finance service"
[5]: ../src/FactoryErp.Infrastructure/Persistence/Configurations/DeliveryInvoiceFinanceConfigurations.cs "G5 EF Core constraints and mappings"
