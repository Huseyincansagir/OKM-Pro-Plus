---
name: factory-erp-implementation
description: Tasarımı kodlayan ana geliştirme skill'i. Backend, web, mobile, database migration, auth, workflow, stok, cari, üretim, rapor, dosya ve bildirim özelliklerini gerçek çalışan kod olarak implement etmek için kullan.
---

# Factory ERP Implementation

## Amaç

`implementation-ready.md` ve ilgili tasarım artefact'larını gerçek ürüne dönüştür.

## Başlangıç koşulu

`/design/implementation-ready.md` yoksa, `READY` değilse veya canonical artefact'lar arasında karar yayılımı doğrulanmamışsa yeni business feature implement etmeye başlama. Bir agent'ın dosyayı READY yazmış olması tek başına yeterli kanıt değildir; `/design/decision-log.md`, solution matrix, domain, workflow, database, screen inventory ve skill-impact review birlikte kontrol edilmelidir. Mevcut küçük altyapı düzeltmeleri gerekiyorsa yap, ancak domain tasarımını varsayarak gizlice değiştirme.

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
- negative unintended stock
- duplicate payment
- double invoicing
- orphaned financial transaction
- untracked stock adjustment
- unauthorized state transition

## Agent workflow

1. Repository'yi incele.
2. Tasarım artefact'ını aç.
3. Mevcut implementation ile karşılaştır.
4. Etkilenen domainleri çıkar.
5. Migration + backend + frontend/mobile + test planını uygula.
6. Kod üret.
7. Build/type-check/test çalıştır.
8. Hataları kendin düzelt.
9. Integration test yap.
10. Dokümantasyonu güncelle.

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
