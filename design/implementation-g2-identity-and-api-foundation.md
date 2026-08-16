# Factory ERP-Lite — G2 Identity and API Foundation Evidence

**Durum:** G2 tamamlandı — G3 ürün/ambalaj/stok slice’ına geçişe hazır
**Tarih:** 2026-08-16
**G1 başlangıç commit’i:** `5b40507`

## 1. Tamamlanan kapsam

G2, G1 persistence foundation üzerine kimlik doğrulama, permission enforcement ve ortak API davranışlarını ekledi. Application katmanı EF/HTTP bağımsız `IAuthenticationService`, `ICurrentUserAccessor`, `IIdempotencyStore`, `IAuditWriter` ve ilgili DTO/contract’ları tanımlar.

Infrastructure katmanında PBKDF2-SHA256 tabanlı salted password hashing, login, refresh rotation ve logout servisi; SHA-256 ile saklanan refresh token kayıtları; EF-backed idempotency store; transaction-friendly audit writer; AuthOptions binding ve kontrollü bootstrap admin seed’i eklenmiştir. Gerçek parola veya refresh token repository’ye yazılmaz.

API katmanında `/api/v1/auth/login`, `/auth/refresh`, `/auth/logout`, `/auth/me` ve permission korumalı `/api/v1/system/health` endpoint’leri çalışır. JWT bearer validation issuer, audience, signing key ve lifetime kontrolleriyle aktiftir. `system.read` permission claim’i backend policy ile zorunlu tutulur; UI button görünürlüğüne güvenilmez.

Ortak middleware sırası şöyledir:

```text
ExceptionProblemDetailsMiddleware
→ IdempotencyKeyMiddleware
→ Authentication
→ Authorization
→ Controllers
```

Correlation ID yoksa server üretir, response header’a yazar ve ProblemDetails içine ekler. Critical mutation route’larında (`orders`, `delivery-notes`, `invoices`, `payments`, `shipments`, `production`) `Idempotency-Key` yoksa güvenli `400 MISSING_IDEMPOTENCY_KEY` response’u döner. Replay/payload-hash persistence’i command handler’larıyla aynı transaction’da kullanılmak üzere `IIdempotencyStore` üzerinden bırakılmıştır.

## 2. Migration

`AddAuthenticationFields` migration’ı `users.password_hash varchar(512)` alanını ekler. Bootstrap admin seed’i migration içine parola gömmek yerine yalnızca Migrator process’inde `BOOTSTRAP_ADMIN_USERNAME` ve `BOOTSTRAP_ADMIN_PASSWORD` environment değerleri mevcutsa çalışır. Seed ikinci çalıştırmada mevcut user bulunduğu için duplicate üretmez.

## 3. Kanıtlar

| Kontrol | Sonuç |
|---|---|
| Release solution build | 0 warning / 0 error |
| Domain unit tests | 28 passed |
| Architecture tests | 5 passed |
| Infrastructure model + security unit tests | 7 passed |
| G1 + G2 migrations | Isolated PostgreSQL’de başarılı |
| Bootstrap identity | 1 admin user, `system_admin` role |
| Login | 200, access/refresh token üretildi |
| `/api/v1/auth/me` | 200, role ve permission summary döndü |
| Refresh rotation | 200, yeni token seti üretildi |
| Anonymous `/api/v1/system/health` | 401 |
| Admin `/api/v1/system/health` | 200, `system.read` policy geçti |
| Critical POST without idempotency key | 400, `MISSING_IDEMPOTENCY_KEY` |
| Critical POST with key | Middleware geçti, route henüz G3/G4’te yoksa 404 |

FluentAssertions testlerinde Xceed lisans bilgilendirme mesajı görülmektedir; bu test hatası değildir ve ticari kullanım öncesinde paket lisansı değerlendirilmelidir.

## 4. Bilinçli sınırlar

G2’de user/role/permission tabloları ve temel JWT/policy davranışı hazırlandı; kullanıcı CRUD, role assignment ekranı, şirket/depo scope, external identity provider, e-posta bildirimi ve tam audit command orchestration sonraki vertical slice’larda tamamlanacaktır. API halen ürün/order/delivery/invoice business endpoint’lerini içermemektedir.

## 5. G3 handoff

G3, `Product` aggregate ve packaging versioning ile başlayacaktır. Sıra; UOM/precision, product/category/barcode master data, packaging conversion snapshots, public catalog projection, quantity preview endpoint’i, finished-good stock receipt ve warehouse available quantity projection’ıdır. G3’te `quantityBase` server-side hesaplanmalı, public DTO’lar internal stock/risk/ledger alanlarını dışlamalı ve `PositiveQuantity`/`NonNegativeQuantity` ayrımı korunmalıdır.
