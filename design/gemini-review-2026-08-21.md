# Gemini Pull İncelemesi — 21 Ağustos 2026

## Sonuç özeti

Remote `main` branch fast-forward olarak pull edildi. Çalışma ağacı temizdir; inceleme sırasında hiçbir kaynak dosyası değiştirilmemiştir. Güncel HEAD:

```text
3e977d0 feat(web): add invoice create and issue flow (P-006)
```

Pull ile önceki yerel HEAD `9d9c81d` arasına üç commit geldi:

| Commit | Kapsam | Değerlendirme |
|---|---|---|
| `6c44f7d` | P-002 sefer hazırlama ve route execution web akışı | İyi bir UI/API wiring; backend command’ları mevcut API’ye bağlıyor |
| `c368d45` | Load verification’da package code veya package ID ile güvenli lookup | Küçük ve doğru bir backend hardening |
| `3e977d0` | P-006 fatura oluşturma/kesme web akışı | Temel akış çalışıyor; finansal veri ve permission tarafında önemli açıklar var |

Genel kararım: **Kod okunabilir ve mevcut API’lere gerçek bağlantı kurulmuş; ancak Gemini’nin dokümanda P-001, P-002 ve P-006’yı doğrudan `PASS` olarak işaretlemesi production gate anlamında erken.** Web gate’leri yeşil olsa da aşağıdaki iş kuralları düzeltilmeden bu slice’ları tamamen kapanmış kabul etmemek gerekir.

## İyi yapılan kısımlar

P-002 tarafında sefer hazırlama dialog’u, kilitli rota/yük planı üzerinden araç ve şoför teyidi, route stop eşleştirmesi, optimistic concurrency için `If-Match`/rowVersion aktarımı ve mevcut dispatch endpoint’lerine bağlanma doğru yönde yapılmış. `arriveStop`, `deliverStop`, `confirmDispatch`, `departDispatch` ve `completeDispatch` client fonksiyonları gerçek API sözleşmelerini kullanıyor; frontend kendi shipment state’ini uydurmak yerine başarılı komuttan sonra yeniden yükleme yapıyor.

P-001 LoadPlan wizard’ında package physical snapshot ve server `quantityBase` kullanılması, fiziksel ölçü yoksa plan uydurulmaması, araç fit değerlendirmesinin backend’e bırakılması, hard error ile warning ayrımı ve lock öncesi confirmation akışı tasarım kararlarıyla uyumlu. Kodun “FFD optimalite iddiası yok” demesi de olumlu; frontend’in optimizasyon sonucunu yeniden hesaplamaya çalışmaması doğru.

`c368d45` commitindeki package lookup düzeltmesi de faydalı. Barkod alanı UUID formatındaysa aynı shipment içindeki `package_code` veya `id` üzerinden `FOR UPDATE` ile lookup yapılması, önceki yalnızca `package_code` bağımlılığını azaltıyor ve aynı shipment ownership sınırını koruyor.

P-006’da fatura listesine detay linki, fatura detay sayfası, `invoice.read` / `invoice.create` / `invoice.issue` görünürlük kontrolleri, issue confirmation dialog’u, loading/error durumları ve issue sonrasında cari borç kaydı hakkında kullanıcı bilgilendirmesi eklenmiş. API client’ları `idempotent: true` ile çağrılıyor ve mevcut backend endpoint’lerine bağlanıyor.

## Bulgular ve önem dereceleri

### 1. Yükleme doğrulama permission gate’i backend ile uyumsuz — YÜKSEK

`ShipmentDetailBoard` içinde yükleme doğrulama butonu şu koşulla gösteriliyor:

```ts
const canPerformLoadVerification =
  (canLoadPlan || canDispatch) && isRouteLocked && isLoadPlanLocked && row?.status === "Preparing" && packages.length > 0;
```

Kaynak: `apps/web/src/components/shipping/shipment-detail.tsx:153-154`.

Buna karşın backend’de start, scan ve complete endpoint’lerinin tamamı `PermissionPolicies.ShipmentLoadVerify`, yani `permission:shipment.load-verify`, policy’si ile korunuyor:

```text
POST /api/v1/load-plans/{id}/load-verification/sessions
POST /api/v1/load-verification/sessions/{id}/scans
POST /api/v1/load-verification/sessions/{id}/complete
```

Kaynak: `src/FactoryErp.Api/Controllers/LoadVerificationController.cs:13-72`.

Sonuç olarak yalnızca `shipment.load-plan` veya yalnızca `shipment.dispatch` yetkisi olan kullanıcı butonu görebilir; gerçek API çağrısı ise `403 Forbidden` döner. Mevcut component testi bu problemi yakalamıyor; testte kullanıcıya `shipment.load-plan` veriliyor, fakat API çağrıları mock’landığı için backend authorization çalışmıyor.

**Öneri:** UI koşuluna doğrudan `const canLoadVerify = permissions.includes("shipment.load-verify")` eklenmeli. Testler `shipment.load-plan` olup `shipment.load-verify` olmayan kullanıcının butonu görmediğini ve `shipment.load-verify` olan kullanıcının gördüğünü doğrulamalı.

### 2. “Yüklemeyi tamamla” gerçek barkod doğrulaması yapmadan bütün paketleri otomatik okutuyor — YÜKSEK

UI’daki `Yüklemeyi tamamla (Loaded)` işlemi gerçek kullanıcı barkod taraması istemiyor. Modal onaylandığında şu döngü çalışıyor:

```ts
const session = await startLoadVerification(...);
for (const pkg of packages) {
  const barcode = pkg.packageCode || pkg.id;
  await scanLoadVerificationPackage(session.id, currentSessionRowVersion, barcode);
  currentSessionRowVersion++;
}
await completeLoadVerification(session.id, currentSessionRowVersion);
```

Kaynak: `apps/web/src/components/shipping/shipment-detail.tsx:634-646`.

Bu davranış, paketlerin gerçekten araçta bulunduğunu tarama ile kanıtlamadan, ekranda listelenen her paketi sistemin kendisinin kabul etmesi anlamına gelir. Uygulamanın genel gereksinimlerinde telefon kamerası ile barkod okuma ve actual load verification bulunuyor. Bu UI akışı ise fiili saha doğrulamasını “paket listesini otomatik dolaşma” işlemine indirgemiştir.

Bu durum P-004 Flutter/kamera slice’ının ileride gerçek taramayı eklemesi planlanıyorsa geçici bir development shortcut olarak kabul edilebilir; fakat mevcut P-002 dokümanında “Yükleme doğrula / tamamla” ve `Loaded` state transition’ı production kapsamı gibi sunuluyor. Bu nedenle **production’da kullanılmamalı**.

**Öneri:** Web’de ya gerçek barkod input/camera akışı eklenmeli ya da bu buton “Demo yükleme doğrulaması” olarak açıkça sınırlandırılmalı ve `Loaded` state’ine geçiş yetkisi gerçek barkod akışına bırakılmalı. En azından kullanıcı her taramayı ayrı ayrı görmeli; beklenmeyen, duplicate veya yanlış LoadUnit sonuçları UI’da gösterilmeden session tamamlanmamalı.

### 3. Fatura kalemleri varsayılan olarak sıfır fiyatla oluşturulabiliyor — YÜKSEK

İrsaliye detayındaki fatura oluşturma dialog’unda her kalem için unit price state’i boşsa `0` gönderiliyor:

```ts
unitPrice: unitPrices[item.id] ?? 0
```

Input’ın başlangıç değeri de `0`; geçersiz veya boş giriş tekrar `0` olarak kaydediliyor:

```ts
value={unitPrices[item.id] !== undefined ? String(unitPrices[item.id]) : "0"}
...
[ item.id ]: isNaN(val) ? 0 : val
```

Kaynak: `apps/web/src/components/shipping/delivery-note-detail.tsx:93-103` ve `269-295`.

Mevcut backend ve database `unit_price >= 0` kuralına izin verdiği için bu istek başarıyla **0 TL tutarlı Draft Invoice** oluşturabilir. Backend `lineTotal = quantityBase * input.UnitPrice` hesapladığı için subtotal, grand total ve issue sırasında cari debit kaydı da sıfır olabilir. Kaynak: `src/FactoryErp.Infrastructure/Shipping/DeliveryInvoiceFinanceService.cs:328-355` ve `src/FactoryErp.Infrastructure/Persistence/Configurations/DeliveryInvoiceFinanceConfigurations.cs` içindeki `ck_invoice_items_amounts_non_negative`.

Bu yalnızca bir UI görünüm sorunu değildir; finansal belgeyi yanlış tutarla üretme riskidir. Üstelik eklenen test, `unitPrice: 0` davranışını beklenen sonuç olarak sabitliyor: `apps/web/src/components/shipping/delivery-note-detail.test.tsx:126-139`.

**Öneri:** Fiyat frontend’de hesaplanmamalı; backend müşteri fiyat listesi veya delivery-note/order price snapshot’ından canonical fiyatı belirlemeli. Geçici olarak kullanıcı girişi zorunlu tutulacaksa boş/zero değer gönderimi engellenmeli ve backend’de de finansal iş kuralına göre `PRICE_REQUIRED` veya uygun bir domain hatası eklenmeli. Test, `0` gönderildiğini doğrulamak yerine fiyat kaynağının backend olduğunu doğrulamalı.

### 4. Fatura oluşturma para birimini sabit olarak TRY gönderiyor — ORTA/YÜKSEK

İrsaliyeden fatura oluşturma çağrısı her durumda:

```ts
currencyCode: "TRY"
```

gönderiyor. Kaynak: `apps/web/src/components/shipping/delivery-note-detail.tsx:93-96`.

Projedeki satış ve fatura contract’ları currency code taşıyor ve backend fatura kaydında currency snapshot’ı ile cari account currency’si kullanılıyor. Bu nedenle çoklu para birimi desteklenirken frontend’in hardcoded TRY göndermesi yanlış cari hesap ve yanlış fiyat snapshot’ı oluşturabilir.

**Öneri:** Para birimi delivery note/order source snapshot’ından gelmeli veya kullanıcıya seçtirilmeli; backend currency compatibility kontrolü yapmalı. P-006 testleri yalnızca TRY varsayımını değil, source currency’nin korunduğunu da doğrulamalı.

### 5. Fatura mapper’ı eksik finansal alanları 0’a çeviriyor — ORTA

`apps/web/src/lib/finance/invoices.ts` içinde `asFiniteNumber` default olarak `0` döndürüyor ve `InvoiceDetail` içindeki `subtotal`, `taxTotal`, `grandTotal`, `unitPrice` ve `lineTotal` alanları nullable değil.

```ts
function asFiniteNumber(value: unknown, defaultValue = 0): number {
  return typeof value === "number" && Number.isFinite(value) ? value : defaultValue;
}
```

Kaynak: `apps/web/src/lib/finance/invoices.ts:49-50` ve `53-93`.

Bu, server response’unda alan eksik olduğunda kullanıcıya “0” gösterir. Projenin cari panosu açıklamasında eksik tutarın `₺0` yazılmaması gerektiği zaten ifade ediliyor. Bu yaklaşım canonical financial value hesaplamasa bile eksik/veri bozukluğunu gerçek sıfır gibi göstermektedir.

**Öneri:** Mapping katmanında finansal alanlar `number | null` olmalı; eksik veya geçersiz response `—` olarak gösterilmeli veya `unexpected response` hatası üretilmeli. Backend finansal response contract’ı da required alanları açıkça garanti etmeli.

### 6. Fatura miktarının view mode metadata’sı hatalı/ambiguous — ORTA

Fatura oluşturma akışında server’dan gelen `remainingToInvoice` temel miktarı, `enteredPackagingId: null` ile birlikte `viewMode: "Piece"` olarak gönderiliyor:

```ts
enteredQuantity: item.remainingToInvoice ?? item.quantityBase ?? 1,
enteredPackagingId: null,
viewMode: "Piece",
```

Backend quantity preview şu an packaging yokken BaseUnit seçtiği için sayısal dönüşüm bugün doğrudan bozulmayabilir. Ancak ürünün temel birimi kg, adet veya farklı birim olabilir; `Piece` metadata’sı gerçeği yansıtmıyor ve ileride view mode kullanan handler’larda yanlış dönüşüm riski oluşturuyor.

**Öneri:** DeliveryNoteItem response’una `baseUomCode` veya source packaging snapshot/view mode eklenmeli. Fatura create isteği, `remainingToInvoice` değerini backend’in beklediği canonical base-unit contract’ı ile açıkça göndermeli; “Piece” sabiti kaldırılmalı.

### 7. Sefer tamamla butonu backend’den önce filtrelenmiyor — ORTA

Shipment detail’de `Rotayı tamamla` butonu `dispatchRun.status === "InTransit"` olduğunda gösteriliyor. Fakat domain `CompleteRoute` açık `Pending` veya `Arrived` stop varken tamamlamayı reddediyor:

```csharp
if (_stops.Any(x => x.Status is not (Departed or Skipped or Delivered)))
    throw new DomainException("ROUTE_NOT_COMPLETE", ...);
```

Kaynak: `src/FactoryErp.Domain/Shipping/DispatchRun.cs:362-372`.

Kullanıcı tüm duraklar tamamlanmadan butona basabilir ve backend’den hata alır. Bu iş kuralının server’da bulunması doğrudur; fakat UI mevcut stop durumunu bildiği için buton disabled/gizli olmalı veya kullanıcıya açık “X durak bekliyor” mesajı verilmelidir.

### 8. LoadPlan wizard route plan seçimi `routePlans[0]` varsayımına bağlı — ORTA/DÜŞÜK

`ShipmentDetailBoard` `activeRoutePlan` hesaplıyor; ancak LoadPlan wizard’a `routePlans[0]` veriliyor:

```tsx
routePlan={routePlans[0] ?? null}
```

Ayrıca kaynak ata/planla/kilitle kontrollerinde de `routePlans[0]` kullanılıyor. Backend şu an route plan listesini `CreatedAt DESC` sıralıyor; bu yüzden çoğu durumda en yeni plan öne gelir. Ancak superseded/old plan, eşit timestamp veya gelecekte backend sıralama değişikliği olduğunda yanlış route planına LoadPlan bağlama riski var.

**Öneri:** Tek bir `activeRoutePlan` seçimi yapılmalı ve wizard ile tüm route action’ları aynı seçilmiş nesneyi kullanmalı. `status` bazlı açık seçim ve `routePlanId` bağlamı gösterilmeli.

## Test ve gate sonuçları

Pull sonrası komutlar yeniden çalıştırılmıştır:

| Kontrol | Sonuç |
|---|---:|
| `pnpm --dir apps/web typecheck` | PASS |
| `pnpm --dir apps/web lint` | PASS |
| `pnpm --dir apps/web test` | **PASS — 72 dosya / 228 test** |
| `pnpm --dir apps/web build` | PASS — Next.js 15.5.23, 28 route |
| `dotnet build FactoryErp.sln --configuration Release` | PASS |
| Domain tests | **PASS — 130/130** |
| Architecture tests | **PASS — 5/5** |
| Infrastructure tests | **90/92 PASS** |
| `git diff --check` | PASS |

Infrastructure testlerindeki iki failure pull öncesindeki baseline sorunlarıdır:

| Failure | Değerlendirme |
|---|---|
| `LogisticsSecurityIntegrationTests.Login_enforces_vehicle_driver_shipment_and_route_permission_boundary` | Physical profile tarih aralığı/setup çakışması; P-002/P-006 kaynaklı değil |
| `PhysicalLogisticsIntegrationTests.Physical_master_creates_profiles_and_replays_idempotently` | Ürün fiziksel profil aralığı overlap/test isolation sorunu; P-002/P-006 kaynaklı değil |

Gemini’nin P-002/P-006 için eklediği web testleri geçmiştir; ancak testlerin çoğu mocked API ile component davranışını doğrular. Gerçek backend authorization ve invoice create/issue transaction senaryoları bu pull’da yeni integration testlerle kapsanmamıştır.

## Dokümantasyon değerlendirmesi

`design/implementation-backlog.md` içinde P-003, P-001, P-002 ve P-006 `[x]` olarak işaretlenmiş. `design/implementation-web-mobile-slice.md` içinde WEB SLICE 019, 020 ve 021 `PASS` olarak raporlanmış. Web build ve test gate’leri gerçekten geçse de yukarıdaki yüksek önem dereceli iş kuralları nedeniyle bu işaretler **“UI/API wiring tamamlandı”** şeklinde daraltılmalı veya önce düzeltmeler yapılmalıdır.

Özellikle P-006 için gerçek finansal değer kaynağı, currency propagation ve invoice integration testleri tamamlanmadan “fatura oluştur/kes tamamlandı” demek risklidir. P-002 için otomatik paket döngüsü gerçek barcode load verification yerine geçmediğinden, production gate’e “camera/manual barcode verification pending” notu eklenmelidir.

## Önerilen aksiyon sırası

1. Önce load verification permission gate’ini düzeltin; `shipment.load-verify` olmadan button gösterilmemeli.
2. Otomatik tüm paketleri okutma davranışını kaldırın veya açıkça development-only olarak işaretleyin; gerçek barcode/manual scan akışını bağlayın.
3. Fatura fiyatını frontend default `0` olmaktan çıkarın; backend price snapshot veya fiyat resolver üzerinden canonical fiyat üretin.
4. Currency’yi hardcoded TRY olmaktan çıkarıp source order/delivery-note currency’sine bağlayın.
5. Invoice response mapper’ında eksik finansal alanları 0 göstermeyin.
6. P-002/P-006 için gerçek API integration testleri ekleyin; özellikle permission denied, zero price, currency mismatch, over-invoicing, issue replay ve concurrent issue senaryolarını test edin.
7. Tüm duraklar tamamlanmadan “Rotayı tamamla” butonunu disabled/gizli yapın ve LoadPlan route selection’ı `activeRoutePlan` ile birleştirin.
8. Bu düzeltmelerden sonra backlog ve slice dokümanlarındaki `PASS` ifadelerini yeniden doğrulayın.

## Son karar

Gemini’nin çalışması **iyi bir başlangıç ve gerçek endpoint bağlantısı açısından başarılı**. Özellikle P-002 UI wiring, P-001 wizard ve package ID lookup hardening faydalı. Ancak iki konu production açısından bloke edici seviyededir:

> **Load verification yetkisi yanlış gate ediliyor ve kullanıcı barkod taramadan tüm paketler otomatik Loaded kabul ediliyor.**

Buna ek olarak fatura akışı **0 fiyatlı ve hardcoded TRY fatura oluşturabildiği için** finansal doğruluk gate’inden geçmemektedir.

Bu nedenle önerilen statü:

```text
P-002: UI/API WIRING PASS — PRODUCTION GATE BLOCKED
P-006: BASIC UI FLOW PASS — FINANCIAL GATE BLOCKED
Overall: REVIEW REQUIRED
Next action: Yukarıdaki 1–6 maddelerini düzelt, sonra P-007’ye geç
```

## İncelenen repository referansları

- `apps/web/src/components/shipping/shipment-detail.tsx`
- `apps/web/src/components/shipping/shipment-detail.test.tsx`
- `apps/web/src/components/shipping/delivery-note-detail.tsx`
- `apps/web/src/components/shipping/delivery-note-detail.test.tsx`
- `apps/web/src/components/finance/invoice-detail.tsx`
- `apps/web/src/lib/finance/invoices.ts`
- `src/FactoryErp.Api/Controllers/LoadVerificationController.cs`
- `src/FactoryErp.Infrastructure/Shipping/LoadVerificationCommandService.cs`
- `src/FactoryErp.Infrastructure/Shipping/DeliveryInvoiceFinanceService.cs`
- `src/FactoryErp.Domain/Shipping/DispatchRun.cs`
- `design/implementation-backlog.md`
- `design/implementation-web-mobile-slice.md`
