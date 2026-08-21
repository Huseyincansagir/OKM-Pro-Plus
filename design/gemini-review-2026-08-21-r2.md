# Gemini Pull Review — İkinci İnceleme

**Tarih:** 21 Ağustos 2026  
**Repository:** `Huseyincansagir/OKM-Pro-Plus`  
**İnceleme türü:** Pull sonrası kod, contract, iş kuralı, authorization, test ve production-readiness review

## 1. Pull sonucu

Önceki review commit’i `b65311b` sonrasındaki remote değişiklikler fast-forward olarak pull edildi. Pull öncesi ve sonrası:

```text
BEFORE: b65311b docs: add Gemini pull review
AFTER:  bf8dba5 fix(shipping,finance): harden load verification, invoice pricing, and route execution rules
```

Gelen iki commit:

| Commit | Kapsam |
|---|---|
| `74560b6` | P-007 payment collection and ledger UI |
| `bf8dba5` | Load verification, invoice pricing ve route execution hardening |

Pull sonrası çalışma ağacı temizdir. Bu ikinci inceleme sırasında repository kaynaklarına değişiklik yapılmamıştır.

## 2. Önceki review’deki maddelerden hangileri düzeldi?

Gemini’nin ikinci commit’i önceki review’de belirtilen bazı sorunları doğru şekilde ele almış:

| Önceki bulgu | Yeni durum | Karar |
|---|---|---|
| Load verification butonu `shipment.load-plan`/`shipment.dispatch` ile görünüyordu | UI artık `shipment.load-verify` permission’ını kontrol ediyor | **Düzeldi** |
| Route tamamla açık stop varken tıklanabiliyordu | UI artık `Pending`/`Arrived` stop varsa butonu disabled gösteriyor ve bekleyen durak sayısını yazıyor | **Düzeldi** |
| Invoice mapper eksik finansal alanları `0` yapıyordu | `subtotal`, `taxTotal`, `grandTotal`, `unitPrice`, `lineTotal` nullable oldu; eksik değer `null` map ediliyor | **Düzeldi** |
| Invoice quantity packaging metadata’sı eksikti | `enteredPackagingId` ve `viewMode` DeliveryNote client modeline taşındı | **Büyük ölçüde düzeldi** |
| Fatura birim fiyatı boşken otomatik `0` gönderiliyordu | UI’da her kalem için `> 0` fiyat zorunlu hale getirildi | **Sadece UI seviyesinde düzeldi** |
| Fatura currency’si sabit TRY idi | UI’da TRY/USD/EUR seçimi eklendi | **UI seviyesinde düzeldi; backend currency integrity eksik** |

## 3. Kalan kritik bulgular

### 3.1 Load verification hâlâ gerçek barkod doğrulaması yapmıyor — YÜKSEK

Yeni UI’da barkod alanı, paket listesi ve “Okut” düğmesi eklenmiş. Ancak okutma işlemi backend’e hemen `POST /scans` göndermiyor; yalnızca local `scannedBarcodes` state’ine paket ID’si ekliyor:

```text
Enter/Okut → local scannedBarcodes state
```

Daha sonra “Yüklemeyi onayla” düğmesine basıldığında component şu işlemi yapıyor:

```text
start session
for every active package:
    packageCode veya packageId ile scan endpoint çağır
complete session
```

Kaynak: `apps/web/src/components/shipping/shipment-detail.tsx:665-681`.

Önemli sorun, bu loop’un `scannedBarcodes` listesini kullanmamasıdır. Kullanıcı hiç barkod okutmasa bile bütün aktif paketler otomatik olarak backend’e scan edilmiş olur. “Tümünü Doğrula” düğmesi de yalnızca local state’i bütün paketlerle doldurur; gerçek scan davranışından farklı bir shortcut olarak çalışır.

Bu nedenle önceki review’deki ana production riski devam ediyor: UI barkod taraması görüntüsü veriyor, fakat `Loaded` transition’ı fiili tarama kanıtına bağlı değil.

**Öneri:** Her “Okut” işleminde aktif session açıldıktan sonra doğrudan backend scan endpoint’i çağrılmalı. Complete yalnızca backend tarafından `Accepted` kabul edilen paketlerin tamamı beklenen paket kümesini karşılıyorsa yapılmalı. “Tümünü Doğrula” production UI’dan kaldırılmalı veya yalnızca açıkça development/test modu olarak işaretlenmeli.

### 3.2 Payment listesi permission gate’i backend ile hâlâ uyumsuz — YÜKSEK

Finance board şu koşulu kullanıyor:

```ts
const canReadPayments =
  permissions.includes("payment.read") || permissions.includes("current-account.read");
```

Bu koşul `current-account.read` tek başına olan kullanıcı için `GET /payments` çağrısını başlatıyor. Ancak backend `PaymentsController` içinde hem `GET /payments` hem de `GET /payments/methods` açıkça `PermissionPolicies.PaymentRead`, yani `permission:payment.read`, istiyor:

```text
src/FactoryErp.Api/Controllers/PaymentsController.cs:24-32
```

Sonuç olarak `current-account.read` olup `payment.read` olmayan kullanıcı UI’da ödeme tablosunu görmeye çalışır, backend 403 döner ve finance board genel hata/permission state’ine düşebilir.

Dokümanda da bu uyumsuzluk yanlış şekilde `payment.read / current-account.read` olarak yazılmış:

```text
design/implementation-web-mobile-slice.md:870-871
```

**Öneri:** Listeleme için UI yalnızca `payment.read` kontrol etmeli. Cari hareketler için `current-account.read` ayrı tutulmalı. Bu boundary için gerçek authorization integration test eklenmeli.

### 3.3 Payment işlemlerinde currency hâlâ backend’de sabit TRY — YÜKSEK

`ApplyPaymentRequest` içinde `CurrencyCode` alanı yok. Backend `ApplyPaymentAsync` içinde:

```csharp
var account = await LockOrCreateCurrentAccountAsync(request.CustomerId, "TRY", cancellationToken);
...
CurrencyCode = "TRY";
```

Kaynak: `src/FactoryErp.Infrastructure/Shipping/DeliveryInvoiceFinanceService.cs:513-520` ve `540-550`.

Frontend payment modalında da tutar alanı `Tutar (TRY)` olarak sabitlenmiş, payment listesi de her ödemeyi `formatMoney(row.amount, "TRY")` ile gösteriyor. Payment DTO’nun kendisi de currency code taşımıyor.

Invoice create ekranına USD/EUR seçimi eklenmiş olsa bile ilgili invoice’a USD/EUR ödeme uygulanınca backend TRY cari hesap ve TRY CurrentTransaction oluşturabilir. Bu, çoklu para birimli cari muhasebe için doğrudan finansal bütünlük riskidir.

**Öneri:** `ApplyPaymentRequest`, `PaymentDto` ve payment allocation contract’larına `CurrencyCode` eklenmeli. Invoice currency varsa ödeme onunla eşleşmeli; serbest tahsilat için açık currency seçimi olmalı. `CurrentAccount` kilidi müşteri + currency bağlamında alınmalı ve currency mismatch backend’de reddedilmeli.

### 3.4 Invoice currency seçimi source currency ile doğrulanmıyor — ORTA/YÜKSEK

Fatura dialog’unda kullanıcı TRY/USD/EUR seçebiliyor:

```tsx
currencyCode: invoiceCurrency || "TRY"
```

Ancak DeliveryNote contract’ında source sales order/invoice currency’si taşınmıyor ve backend `CreateInvoiceAsync` içinde seçilen currency’nin source order veya delivery note currency’siyle uyumu kontrol edilmiyor. Kullanıcı TRY irsaliyeden USD fatura oluşturabilir; backend bunu kabul edebiliyor.

**Öneri:** DeliveryNote veya SalesOrder currency snapshot’ı response’a eklenmeli. Fatura currency’si source currency’den türetilmeli veya backend açık bir conversion/exchange-rate contract’ı olmadan farklı currency’yi reddetmeli.

### 3.5 Invoice fiyatı hâlâ frontend’den canonical finansal değer olarak gönderiliyor — ORTA/YÜKSEK

Yeni UI, fiyatı artık `0` varsayılanıyla göndermiyor; bu olumlu. Ancak fiyat hâlâ kullanıcı inputundan alınıp doğrudan `CreateInvoiceRequest.UnitPrice` olarak backend’e gönderiliyor. Backend yalnızca `unit_price >= 0` database constraint’ine ve front-end guard’a dayanıyor; müşteri fiyat listesi, satış order price snapshot’ı veya yetkili fiyat kaynağıyla zorunlu eşleştirme görünmüyor.

Kullanıcının ana kuralı frontend’in canonical financial/quantity değerleri hesaplamaması ve business logic’i frontend’e taşımamasıdır. Kullanıcı tarafından gerçekten fiyat girilmesi bir “price override” senaryosu olabilir; ancak bunun yetkisi, fiyat kaynağı, audit ve limit kontrolü backend’de tanımlanmamış.

**Öneri:** Backend invoice create sırasında source order/price snapshot veya yetkili customer price resolver kullanmalı. UI yalnızca “fiyat override talebi” gönderiyorsa bunun ayrı permission ve audit alanları olmalı. Backend sıfır fiyatı ve yetkisiz fiyat farkını reddetmeli.

### 3.6 Concurrent payment allocation’da invoice row lock yok — YÜKSEK

`ApplyPaymentAsync` invoice’ı normal `SingleOrDefaultAsync` ile yüklüyor:

```csharp
invoice = await dbContext.Invoices
    .SingleOrDefaultAsync(...);
```

Sonrasında mevcut payment allocation toplamını okuyup `allocated + request.Amount > invoice.GrandTotal` kontrolü yapıyor. Invoice veya ilgili payment allocations `FOR UPDATE` ile kilitlenmediği için iki farklı payment request aynı anda aynı kalan tutarı okuyup ikisi de başarılı olabilir. Cari account kilidi, invoice allocation yarışını tek başına çözmez.

Bu risk idempotency ile çözülmez; farklı idempotency key’leriyle gelen iki meşru concurrent tahsilat isteği aynı invoice üzerinde yarışabilir.

**Öneri:** Invoice row `FOR UPDATE` ile kilitlenmeli, allocation total lock altında hesaplanmalı ve invoice status aynı transaction’da güncellenmeli. Ayrı bir backend integration test ile iki concurrent payment’in toplamının invoice grand total’ı aşmadığı doğrulanmalı.

### 3.7 Payment backend için dedicated integration test yok — ORTA/YÜKSEK

Yeni web testleri payment modal ve finance board happy-path davranışını mock’larla test ediyor. Repository’de `ApplyPaymentAsync`, `PaymentApplied` ve `OVER_PAYMENT` kelimeleriyle bulunan tek mevcut backend test bağlantısı `CurrentAccountConcurrencyIntegrationTests` içindedir; yeni P-007 için gerçek `/auth/login` üzerinden payment apply, permission, currency, over-payment veya concurrent payment integration testleri eklenmemiştir.

Bu nedenle P-007’nın web wiring’i test edilmiş olsa da financial transaction gate’i kanıtlanmış değildir.

**Öneri:** En az şu integration testler eklenmeli:

| Senaryo | Beklenen sonuç |
|---|---|
| `payment.apply` olmadan POST | 403 |
| pasif müşteri | `CUSTOMER_NOT_ACTIVE` |
| pasif payment method | `PAYMENT_METHOD_NOT_FOUND` |
| sıfır/negatif amount | `PAYMENT_AMOUNT_INVALID` |
| invoice toplamını aşan payment | `OVER_PAYMENT` |
| aynı idempotency key replay | tek payment ve aynı response |
| farklı key ile concurrent payment | toplamın grand total’ı aşmaması |
| currency mismatch | açık domain error |

## 4. P-001/P-002 tarafındaki kalan konular

### 4.1 Route plan seçimi hâlâ `routePlans[0]` varsayımına bağlı — ORTA

`activeRoutePlan` seçimi status önceliğiyle yapılmış olsa da LoadPlan wizard ve bazı route action’ları hâlâ `routePlans[0]` kullanıyor. Birden çok route plan olduğunda wizard ile execution farklı plana bağlanabilir.

**Öneri:** Tek bir `activeRoutePlan` değişkeni tüm wizard ve route action’larında kullanılmalı; seçilen planın ID’si kullanıcıya gösterilmeli.

### 4.2 Payment ve current account listeleri `Take(100)` ile sessiz kesiliyor — ORTA

P-007 listeleri backend’de `Take(100)` kullanıyor. Proje backlog’unda da S-004 “Staff listeler: gerçek pagination; `Take(100)` sessiz kesim yasak” maddesi bulunuyor. Aynı risk payment, current account ve current transaction ekranlarına da uygulanıyor. Küçük MVP için geçici olabilir; ancak şirket içi ERP’de finansal kayıt listeleri sessiz kesilmemeli.

**Öneri:** Cursor/page pagination ve toplam/kalan kayıt metadata’sı eklenmeli.

## 5. Güncel verification sonuçları

Pull sonrası şu kontroller gerçek repository üzerinde tekrar çalıştırıldı:

| Kontrol | Sonuç |
|---|---:|
| `pnpm --dir apps/web typecheck` | PASS |
| `pnpm --dir apps/web lint` | PASS |
| `pnpm --dir apps/web test` | **PASS — 74 dosya / 242 test** |
| `pnpm --dir apps/web build` | PASS — Next.js 15.5.23, 28 route |
| `dotnet build FactoryErp.sln --configuration Release` | PASS |
| Domain tests | **PASS — 130/130** |
| Architecture tests | **PASS — 5/5** |
| Infrastructure tests | **90/92 PASS** |
| `git diff --check` | PASS |
| Pull sonrası çalışma ağacı | CLEAN |

Kalan iki infrastructure failure önceki baseline sorunlarıdır:

- `PhysicalLogisticsIntegrationTests.Physical_master_creates_profiles_and_replays_idempotently`: physical profile tarih aralığı/test isolation overlap.
- `LogisticsSecurityIntegrationTests.Login_enforces_vehicle_driver_shipment_and_route_permission_boundary`: aynı physical profile hazırlık/setup nedeniyle 422 beklenirken alınıyor.

Bu iki failure yeni P-007 commitlerinin doğrudan sonucu görünmüyor; ancak repository full gate hâlâ kırmızı olduğu için dokümanda yalnızca Domain/Architecture gate’lerini PASS göstermek yerine full backend suite sonucu ayrıca belirtilmeli.

## 6. Doküman doğruluğu

`design/implementation-web-mobile-slice.md:861-898` P-007’yı `WEB SLICE 022 — Payment and collection ledger UI` olarak `PASS` ilan ediyor ve `payment.read / current-account.read` ile payment listesi okunabildiğini yazıyor. Bu ifade backend controller ile uyumlu değil; `/payments` için gerçek policy `payment.read`.

Ayrıca doküman ödeme fatura allocation ve over-payment guard’ını kuralsal olarak yazıyor, fakat concurrent allocation row lock ve currency contract’ı yok. Bu nedenle doküman “UI/API wiring PASS, financial production gate pending” şeklinde daraltılmalı.

## 7. Son karar

Yeni pull önceki review’e göre anlamlı iyileştirmeler içeriyor. Permission gate, route complete guard, nullable financial mapping ve invoice UI validation doğru yönde. Buna karşın production gate hâlâ kapanmış değil.

```text
P-007: UI/API WIRING PASS — FINANCIAL GATE BLOCKED
P-006: UI VALIDATION IMPROVED — CURRENCY/PRICE AUTHORITY GATE BLOCKED
P-002: ROUTE EXECUTION UI IMPROVED — LOAD VERIFICATION GATE BLOCKED
Overall: REVIEW REQUIRED
```

Öncelikli aksiyon sırası:

1. Load verification’da local “scanned” state yerine gerçek incremental backend scan akışını kurun; complete’i accepted scan set’ine bağlayın.
2. Finance board payment listesi için `payment.read` / `current-account.read` ayrımını düzeltin.
3. Payment currency contract’ını backend’e taşıyın; TRY hardcode’unu kaldırın.
4. Invoice currency’yi source currency ile doğrulayın.
5. Invoice fiyat authority/override kuralını backend’de tanımlayın.
6. Invoice row lock ve concurrent payment integration testlerini ekleyin.
7. Payment backend integration/security testleri tamamlanmadan P-007’yı production PASS ilan etmeyin.
8. `Take(100)` finansal listeler için pagination ekleyin.

## İncelenen başlıca dosyalar

- `apps/web/src/components/shipping/shipment-detail.tsx`
- `apps/web/src/components/shipping/shipment-detail.test.tsx`
- `apps/web/src/components/shipping/delivery-note-detail.tsx`
- `apps/web/src/lib/shipping/delivery-notes.ts`
- `apps/web/src/components/finance/finance-board.tsx`
- `apps/web/src/components/finance/payment-modal.tsx`
- `apps/web/src/lib/finance/payments.ts`
- `src/FactoryErp.Api/Controllers/PaymentsController.cs`
- `src/FactoryErp.Application/Shipping/ShippingFinanceContracts.cs`
- `src/FactoryErp.Infrastructure/Shipping/DeliveryInvoiceFinanceService.cs`
- `src/FactoryErp.Domain/Shipping/DispatchRun.cs`
- `design/implementation-web-mobile-slice.md`

## References

[1]: `../home/ubuntu/OKM-Pro-Plus/apps/web/src/components/shipping/shipment-detail.tsx` — Load verification UI
[2]: `../home/ubuntu/OKM-Pro-Plus/src/FactoryErp.Api/Controllers/PaymentsController.cs` — Payment authorization routes
[3]: `../home/ubuntu/OKM-Pro-Plus/src/FactoryErp.Infrastructure/Shipping/DeliveryInvoiceFinanceService.cs` — Invoice/payment transaction rules
[4]: `../home/ubuntu/OKM-Pro-Plus/apps/web/src/components/finance/finance-board.tsx` — Finance board permissions and currency rendering
[5]: `../home/ubuntu/OKM-Pro-Plus/design/implementation-web-mobile-slice.md` — Slice status documentation
