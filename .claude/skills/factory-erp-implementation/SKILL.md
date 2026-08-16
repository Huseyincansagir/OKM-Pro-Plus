---
name: factory-erp-implementation
description: Tasarımı kodlayan ana geliştirme skill'i. Backend, web, mobile, database migration, auth, workflow, stok, cari, üretim, rapor, dosya ve bildirim özelliklerini gerçek çalışan kod olarak implement etmek için kullan.
---

# Factory ERP Implementation

## Amaç

`implementation-ready.md` ve ilgili tasarım artefact'larını gerçek ürüne dönüştür.

## Başlangıç koşulu

`/design/implementation-ready.md` yoksa, `READY` değilse veya canonical artefact'lar arasında karar yayılımı doğrulanmamışsa yeni business feature implement etmeye başlama. Bir agent'ın dosyayı READY yazmış olması tek başına yeterli kanıt değildir; `/design/decision-log.md`, solution matrix, `/design/decision-clarification-backlog.md`, domain, workflow, database, screen inventory, UI design package ve skill-impact review birlikte kontrol edilmelidir. Ambalaj, fiziksel ölçü, araç kapasitesi, rota/durak, yük birimi ve `ShipmentPackage` kuralları implementation öncesi aynı canonical sürümde bulunmalıdır. P0 açık kararlar proje sahibi tarafından kapatılmadan veya açıkça `ASSUMED WITH RISK` kabul edilmeden ilgili business feature'a başlama. Mevcut küçük altyapı düzeltmeleri gerekiyorsa yap, ancak domain tasarımını varsayarak gizlice değiştirme.

## Teknoloji

Varsayılan:

- Backend: ASP.NET Core Web API / C#
- ORM: Entity Framework Core
- Database: PostgreSQL
- Web: Next.js + React + TypeScript
- Mobile: Flutter
- State/query: TanStack Query ve gerekli yerde Zustand
- Forms: React Hook Form + Zod
- Logging: Serilog
- Container: Docker Compose

## Kodlama ilkeleri

- Clean Architecture
- SOLID
- DI
- DTO
- validation
- clear service boundaries
- predictable naming
- readable code
- minimal abstraction

## Feature completion rule

Bir feature tamamlanmış sayılmaz; şu katmanların hepsi tamamlanmalı:

`Database → Domain/Application → API → Authorization → Web/Mobile → Validation → Tests → Documentation`

Sevkiyat/lojistik feature'larında ek kapsam:

`VehicleType/Vehicle → Capacity → RoutePlan/RouteStop → LoadPlan/LoadUnit → ShipmentPackage → DeliveryProof`

Kapasite planlama veya teslimat tracking ekranı fake package data ile tamamlanmış sayılmaz; paket barkodu, müşteri/adres, durak ve durum bağlantıları gerçek domain kaynaklarından gelmelidir.

Mock endpoint veya fake frontend data ile bitirme.

## State transition

Belge durumlarını explicit enum/state machine/domain rules ile yönet.

State transition örneği:

`Draft → PendingApproval → Approved → Preparing → Shipped → Completed`

Geçersiz transition'ları backend'de reddet.

## Stok

Stok miktarını yalnızca UI'dan değiştirme.

Her fiziksel değişim bir `StockMovement` üretmeli.

Reservation, available quantity ve on-hand quantity birbirinden ayrılmalı.

## Cari

Cari bakiye, izlenebilir transaction'ların sonucudur. Kritik finansal hareketleri immutable kabul et.

## Dosya üretimi

PDF ve Excel çıktılarında aynı domain verisini kullan. Rapor ekranı ile export sonucu arasında tutarsızlık oluşturma.

## Frontend

- Loading, empty, error, permission denied ve offline state'leri tamamla.
- Türkçe UI.
- Responsive web.
- Table yoğun operasyonlara uygun.
- Mobilde görev odaklı akış.
- Sevkiyat mobil akışında kullanıcı yalnızca aktif route stop'a atanmış `ShipmentPackage` kayıtlarını görebilmeli ve teslim edebilmeli.
- Barkod tarama sonucu ürün, ambalaj/yük birimi ve işlem bağlamı doğrulanmadan işlem formu veya kesinleştirme aksiyonu açılmamalı.
- `Temel Birim / Ambalaj / Kırılım` toggle'ı yalnızca görünümü değiştirmeli; işlem seviyesi ayrı state ve `quantity_base` önizlemesiyle yönetilmeli.
- Kargo planı ekranında hard constraint, soft warning, vehicle-fit gerekçesi, algorithm/version ve manuel değişiklik açıklaması görünür olmalı.
- Her araç adayı için `load_plan_vehicle_candidates` veya `VehicleFitEvaluation` sonucu, kapasite kullanım oranları ve `rejection_code` saklanmalı; yalnızca önerilen araç kaydedilmemeli.
- Toplam kg/m³ kontrolü; zemin alanı, kapı açıklığı, iç ölçü, yükseklik, yön, istif, durak erişimi ve varsa aks/yük bölgesi kontrolünden ayrı çalıştırılmalı.
- Kapasite güvenlik payları ve skor ağırlıkları policy/parameter set olarak versionlanmalı; kod içine sessiz sabit olarak gömülmemeli.
- First Fit Decreasing veya seçilen heuristik deterministik input snapshot ile çalışmalı; matematiksel optimalite garanti ediliyorsa ayrıca kanıtlanmadan böyle sunulmamalı.
- Mobil toggle API'sinde `viewMode` ile `operationPackagingId` ayrı request alanları olmalı; `quantityBase`, `display` ve `packagingSnapshot` server-side hesaplanmalı.
- Miktar preview transaction oluşturmamalı; sayım, transfer, load scan ve delivery endpoint'leri `Idempotency-Key`, context/permission ve concurrency kontrolü olmadan kesinleşmemeli.
- Araç, sevkiyat, durak ve paket durumları ayrı badge/state olarak gösterilmeli; kapasite ve ambalaj görünümü toggle ile değişebilmeli.

## Mobile

Öncelik:

- barkod
- stok sayım
- stok transferi
- sevkiyat
- üretim kaydı
- bildirim

Telefon ağ bağlantısı kaybolduğunda stok/finans hareketini sessizce offline commit etme.

## Error handling

Global API exception handler, standard error response ve kullanıcı dostu frontend mesajları kullan.

## Data integrity

Aşağıdaki hatalar kabul edilmez:

- duplicate document number
- duplicate package barcode scan or delivery application
- duplicate barcode scan must not create a second stock/transfer/count/delivery movement
- toggle change must not change `quantity_base` or selected operation quantity
- package assigned to wrong customer/address/route stop
- vehicle capacity or route overlap not validated
- hard/soft constraint severity not preserved
- algorithm version or input snapshot missing for a generated load plan
- manual replan without reason/audit/version
- client-supplied quantityBase accepted without recalculation
- duplicate mobile operation without Idempotency-Key
- missing packaging snapshot on a committed quantity movement
- negative unintended stock
- duplicate payment
- double invoicing
- orphaned financial transaction
- untracked stock adjustment
- unauthorized state transition

## Karar kapanışı ve kapsam kilidi

- Feature başlamadan önce ilgili O-ID'lerini ve clarification backlog alt sorularını yazılı olarak eşleştir.
- Vergi/e-belge, kısmi sevkiyat/fatura, BOM/lot, deployment, fiyat, marka ve ilgili permission kararları kapanmadıysa migration/API/state tasarımını varsayımla kilitleme.
- Her seçilmiş karar için owner/date/rationale/effected artefacts/acceptance tests kaydı olmadan `Definition of Done` değerlendirmesi yapma.
- Karar değişikliği implementation sırasında ortaya çıkarsa kod içinde sessizce uyarlama yapma; Design Gate'e geri dön ve canonical belgeleri güncelle.

## Agent workflow

1. Repository'yi incele.
2. Tasarım artefact'ını aç; özellikle `shipment-logistics-ui-design.md` dosyasını oku.
3. Mevcut implementation ile karşılaştır.
4. Vehicle, capacity, route stop, load unit ve shipment package etkilerini çıkar.
5. Migration + backend + frontend/mobile + test planını uygula.
6. Paket/rota/teslimat ve barkod idempotency/permission testlerini ekle.
7. Toggle görünümünün işlem seviyesini ve `quantity_base` değerini yanlışlıkla değiştirmediğini test et.
8. Kod üret.
9. Build/type-check/test çalıştır.
10. Hataları kendin düzelt.
11. Integration test yap.
12. Dokümantasyonu güncelle.

Gereksiz yere kullanıcıdan onay isteme.

## Definition of Done

Feature ancak:

- build başarılı
- migration uygulanabilir
- API testleri geçiyor
- ilgili UI çalışıyor
- permission uygulanmış
- audit/log davranışı tamam
- edge-case'ler ele alınmış
- test eklenmiş
- docs güncellenmiş

olduğunda tamamdır.
