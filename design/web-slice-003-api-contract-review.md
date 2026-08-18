# WEB SLICE 003 API CONTRACT REVIEW

**İnceleme kapsamı:** `OKM-Pro-Plus` deposundaki mevcut ASP.NET Core API, Application ve Infrastructure katmanlarının; WEB SLICE 003 — Auth & API Client için gerçek backend sözleşmesi açısından incelenmesi.

**İnceleme kuralı:** Bu raporda yalnızca kaynak kodda doğrulanabilen endpoint, request, response, hata ve token davranışları **existing** olarak adlandırılmıştır. Tasarım dokümanlarında bulunup kaynak kodda doğrulanamayan yüzeyler açıkça **NOT IMPLEMENTED** olarak işaretlenmiştir. İnceleme sırasında uygulama kodu, veritabanı veya migration değiştirilmemiştir. Repository’nin canonical kaynak ve implementation gate kuralları [AGENTS.md][1] ile uyumludur.

## Existing API

API’nin gerçek kök yolu `/api/v1`’dir. Controller’lar auth, public catalog, public quote request, sales order, delivery note, invoice, payment, mobile quantity/barcode, shipment, route/load plan, production, warehouse transfer, vehicle ve dispatch alanlarına dağılmıştır. WEB SLICE 001 raporu da auth, API client ve public catalog feature’larının henüz uygulanmadığını; auth/session ve API client’ın WEB SLICE 003’e bırakıldığını doğrular [2].

| Alan | Gerçekleşen endpoint yüzeyi | Authorization | Durum ve gözlem |
|---|---|---|---|
| Authentication | `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout`, `GET /api/v1/auth/me` | Login/refresh anonymous; logout/me authenticated | **Existing**. Dört temel auth endpoint’i controller’da mevcut [8]. |
| Public catalog | `GET /api/v1/public/catalog/products`, `GET /api/v1/public/catalog/products/{slug}` | Anonymous | **Existing**. Aktif ve public ürünlerin liste/detail görünümünü döndürür [15] [16]. |
| Public quote request | `POST /api/v1/public/quote-requests` | Anonymous | **Existing**. Tek seferlik public talep gönderimi vardır; idempotency kapsamına alınmamıştır [18] [20]. |
| Internal quote requests | `GET /api/v1/quote-requests`, `GET /api/v1/quote-requests/{id}`, `POST /api/v1/quote-requests/{id}/review` | Permission policy | **Existing**. Public talep ile iç inceleme yüzeyi ayrıdır [21]. |
| Sales orders | `POST /api/v1/orders`, `GET /api/v1/orders/{id}`, `POST /api/v1/orders/{id}/submit`, `/approve`, `/reject` | `order.create/read/submit/approve/reject` | **Existing**. Mutation’lar `Idempotency-Key` alır [18]. Sipariş listesi ve cancel endpoint’i kaynakta doğrulanmamıştır. |
| Delivery notes | `POST /api/v1/delivery-notes`, `GET /api/v1/delivery-notes/{id}`, `POST /api/v1/delivery-notes/{id}/issue` | Delivery-note permission’ları | **Existing**. Tasarımda geçen nested create route’u yerine düz `/delivery-notes` route’u kullanılmıştır [26]. |
| Invoice/payment | `POST /api/v1/invoices`, `GET /api/v1/invoices/{id}`, `POST /api/v1/invoices/{id}/issue`; `POST /api/v1/payments` | Invoice/payment permission’ları | **Existing**. Create/issue/apply akışları idempotency header’ı okuyacak şekilde bağlanmıştır [27] [28]. |
| Mobile quantity/barcode | `POST /api/v1/mobile/barcodes/resolve`, `POST /api/v1/mobile/quantity-previews` | Authenticated | **Existing**. Bunlar preview/resolve yüzeyleridir; stok commit endpoint’i oldukları varsayılamaz [22]. |
| Shipping and logistics | Shipment, route-plan, load-plan, load-verification, shipment-package, vehicle-fit ve dispatch route’ları | Permission policy’leri | **Existing**. Her endpoint’in ayrıntılı client contract’ı bu raporun auth/API-client sınırının dışındadır; header davranışı ortak kurallara bağlanmalıdır. |
| Health/root | `/health/live`, `/health/ready`, `/health/startup`, `/` | Anonymous | **Existing**. Bunlar uygulama ve health yüzeyidir; auth/session endpoint’i değildir [12]. |

Tasarım sözleşmesi REST, DTO, `application/problem+json`, `X-Correlation-Id`, `Idempotency-Key` ve `If-Match`/row version yaklaşımını hedeflemektedir [3]. Gerçek implementation bu hedefin önemli bir bölümünü taşımakla birlikte, özellikle auth challenge/forbidden yanıtları, 404 response’ları ve bazı status-code eşlemeleri henüz tek biçimli değildir.

## Auth Contract

### Endpoint matrisi

| Endpoint | HTTP method | Route | Request | Başarılı response | Errors | Authorization | Token behavior | Idempotency / concurrency |
|---|---:|---|---|---|---|---|---|---|
| Login | `POST` | `/api/v1/auth/login` | JSON body: `userName`, `password` (`LoginRequest`) | `200 OK`; `AuthTokens`: `accessToken`, `accessTokenExpiresAt`, `refreshToken`, `refreshTokenExpiresAt`, `user` özeti | Boş kullanıcı adı/parola için `400 INVALID_REQUEST`; yanlış parola, bulunamayan veya pasif kullanıcı için `401 UNAUTHENTICATED` | `AllowAnonymous` | Yeni access JWT ve yeni refresh token üretir. | Idempotency yoktur. Her başarılı login yeni refresh-token kaydı üretir; kaynakta login yarışını birleştiren bir concurrency kuralı yoktur [8] [9] [10]. |
| Refresh | `POST` | `/api/v1/auth/refresh` | JSON body: `refreshToken` (`RefreshTokenRequest`) | `200 OK`; yeni access/refresh token çifti ve user özeti | Boş, bilinmeyen, revoked, expired veya pasif kullanıcıya ait token için `401 TOKEN_EXPIRED` | `AllowAnonymous`; request body refresh token ile kimliklenir | Eski refresh token `RevokedAt` ile iptal edilir ve yeni token çifti üretilir; bu rotation davranışıdır. | Idempotency yoktur. Kaynakta explicit transaction, row lock veya refresh-family concurrency guard görünmemektedir [8] [9]. |
| Logout | `POST` | `/api/v1/auth/logout` | JSON body: `refreshToken`; controller boş değer için ayrıca validation yapmaz | `204 No Content` | Endpoint `[Authorize]` olduğu için geçersiz access token framework seviyesinde reddedilir. Boş/bilinmeyen/revoked refresh token service tarafından no-op işlenir ve yine `204` döner. | `Authorize` | Verilen refresh token bulunursa revoke edilir. Access JWT için revoke-list veya server-side blacklist yoktur; access token süresi dolana kadar geçerli kalabilir. | Semantik olarak tekrarlandığında no-op/idempotent davranır; explicit idempotency kaydı yoktur [8] [9]. |
| Me | `GET` | `/api/v1/auth/me` | Body yok; `Authorization: Bearer <accessToken>` | `200 OK`; `user.id`, `user.userName`, `user.displayName`, `user.roles`, `user.permissions`, `company`, `permissionVersion` | Geçerli bearer yoksa framework authentication challenge’ı uygulanır; controller içinde custom 401 ProblemDetails yoktur. | `Authorize` | Access token claim’lerinden summary üretir; refresh yapmaz. `company.code` `default`, `company.name` `Factory ERP`, `permissionVersion` ise `g2` olarak sabit response alanıdır. | Idempotency ve write concurrency yoktur [8] [12]. |

### Request ve response alanları

`AuthTokens` kaydı access ve refresh token değerleriyle birlikte iki expiry alanı ve `UserSummary` döndürür. `UserSummary` içinde `id`, `userName`, `displayName`, `roles`, `permissions` ve `rowVersion` bulunur [10]. Login ve refresh response’larının gerçek alanları bu nedenle tasarım dokümanındaki “refresh token metadata only” hedefiyle tamamen aynı değildir: implementation ham `refreshToken` değerini JSON response içinde gönderir. Bu fark, WEB SLICE 003 başlamadan önce açık bir security/contract kararı olarak ele alınmalıdır.

### Authorization

JWT bearer doğrulaması issuer, audience, signing key ve lifetime kontrolüyle yapılır. Permission policy’leri role adına değil, JWT içindeki `permission` claim’lerine dayanır; bu nedenle web tarafındaki button visibility yalnızca UX filtresidir ve gerçek authorization kaynağı değildir [12].

## Token Lifecycle

Authentication service varsayılan olarak access token’ı **15 dakika**, refresh token’ı **14 gün** süreyle üretir. Issuer `factory-erp`, audience `factory-erp-clients` ve varsayılan signing key development-only bir placeholder’dır; production deployment’ta signing key mutlaka secret/environment üzerinden override edilmelidir [11].

Access JWT; `sub`, `jti`, `NameIdentifier`, `Name`, `Email`, `preferred_username`, role claim’leri ve permission claim’leri taşır. Refresh token, cryptographically random 48 byte değerin Base64 gösterimidir; database’e ham değer yerine SHA-256 hash’i yazılır. Refresh kaydı kullanıcı, hash, oluşturulma, expiry ve revoke zamanını içerir [9]. Bu, database sızıntısında ham refresh token’ın doğrudan okunmasını azaltan olumlu bir uygulamadır.

Refresh işlemi mevcut token’ı kontrol eder, geçerliyse eski kaydı revoke eder ve yeni token çifti oluşturur. Ancak implementation’da refresh-family, cihaz oturumu, tüm cihazlardan logout, access-token revocation veya concurrent refresh için açık bir lock/idempotency davranışı yoktur. Web client aynı refresh token’ı paralel olarak göndermemeli; tek-flight refresh mekanizması kullanmalıdır.

`/me` endpoint’i database’den yeni permission sorgulamak yerine access token claim’lerini okur. Bu nedenle permission değişikliği token expiry’sine kadar eski claim’lerle taşınabilir. `permissionVersion` alanının `g2` olarak sabitlenmesi, permission cache invalidation veya token refresh zorlaması için gerçek bir backend kontrolü değildir [8].

**Token storage kararı:** Repository’de henüz web token storage implementation’ı bulunmamaktadır; dolayısıyla mevcut kod `localStorage kullanıyor` şeklinde raporlanamaz. Bununla birlikte WEB SLICE 003’te access veya refresh token’ı `localStorage`’a yazmak önerilmez. `localStorage`, XSS veya üçüncü taraf script/bağımlılık ihlali halinde token’ın JavaScript tarafından okunmasına, kalıcı oturum hırsızlığına ve logout sonrasında eski kopyaların kalmasına izin verir. Tercih edilen çözüm refresh token’ın `HttpOnly`, `Secure`, uygun `SameSite` cookie veya Next.js server-side BFF oturumunda tutulması; access token’ın ise yalnızca memory scope’unda tutulmasıdır. Mevcut backend refresh token’ı request body ile aldığı için cookie/BFF yaklaşımı ayrıca netleştirilmeli; bu karar verilmeden `localStorage` bir “kolaylık” olarak seçilmemelidir.

## Error Contract

Global exception middleware bütün yakalanmamış exception’ları `application/problem+json` response’una çevirir; response’ta `type`, `title`, `status`, `code`, `detail`, `instance`, `requestId`, `correlationId`, `retryable`, `errors` ve `actions` alanları bulunur. `X-Correlation-Id` request’ten alınır veya server tarafından üretilir [13]. Bununla birlikte controller’ların doğrudan döndürdüğü 401, 403 ve 404 response’ları aynı middleware’den geçmediği için pratik sözleşme tamamen homojen değildir.

| HTTP status | Gerçek implementation davranışı | Web client normalization kararı |
|---:|---|---|
| `400` | Auth controller eksik credential için custom ProblemDetails döndürür. Eksik `Idempotency-Key` için middleware `MISSING_IDEMPOTENCY_KEY` döndürür. `ArgumentException` global middleware’de `INVALID_REQUEST` olur. `[ApiController]` model-binding/validation response’u için ayrıca custom factory tanımlanmamıştır. | `BadRequest` kategorisine normalize et; `code`, `errors` ve `requestId` varsa koru. `MISSING_IDEMPOTENCY_KEY` için kullanıcıya sessiz retry değil, request-builder/header bug’ı olarak telemetry üret. |
| `401` | Login ve refresh controller’ı `UNAUTHENTICATED` veya `TOKEN_EXPIRED` ile custom 401 döndürür. Protected endpoint’ler için `JwtBearerEvents.OnChallenge` veya global challenge mapper yoktur; bu nedenle framework default response’u gelebilir [8] [12]. | `Unauthenticated` olarak normalize et. Yalnızca access-token ile çağrılan authenticated request’te bir kez refresh dene; login/refresh request’inde refresh loop başlatma. |
| `403` | Permission policy’leri `permission` claim’i ister. Custom forbidden handler yoktur. Bazı controller path’lerinde doğrudan bare `Forbid()` kullanılır; response gövdesi ProblemDetails olmayabilir [12] [23]. | Her 403’ü `PermissionDenied` olarak normalize et. Refresh deneme. Raw body yoksa status üzerinden anlam üret; permission code varsa koru. |
| `404` | Controller’ların çoğu `NotFound()` ile bare 404 döndürür. Public catalog ve mobile controller bazı durumlarda `RESOURCE_NOT_FOUND` alanlı minimal object döndürür, fakat bu da global ProblemDetails kadar zengin değildir [15] [22]. | `NotFound` olarak normalize et; `code` yoksa route/resource bağlamından güvenli genel mesaj üret. |
| `409` | Kaynakta explicit `Conflict()` veya ikinci bir 409 mapping bulunmamıştır. Global middleware yalnızca `DbUpdateConcurrencyException` için `409 QUANTITY_CONCURRENCY_CONFLICT`, `retryable=true` döndürür [13]. | `ConcurrencyConflict`/`Conflict` olarak normalize et; önce fresh GET yap, stale UI’ı yenile, kullanıcının girdisini koru ve körlemesine command replay etme. |
| `422` | `DomainException` bütün domain/business hatalarını 422’ye map eder; örneğin state conflict ve `IDEMPOTENCY_PAYLOAD_MISMATCH` mevcut service’lerde DomainException olarak fırlatılır. Bu nedenle tasarımda 409 beklenen bazı conflict’ler bugün 422’ye dönüşür [13] [20]. | `BusinessRuleViolation` veya `ValidationError` olarak normalize et. `IDEMPOTENCY_PAYLOAD_MISMATCH` için yeni key üretip tekrar göndermek yerine payload/key eşleşmesini düzelt. |
| `500` | Diğer yakalanmamış exception’lar generic `UNEXPECTED_ERROR`, `retryable=false` ve güvenli genel detail ile 500 olur. | `UnexpectedServerError` olarak normalize et; yalnızca `requestId`/`correlationId` göster, iç exception detail göstermeden destek kaydı oluştur. |

Architecture tasarımı ayrıca `STATE_TRANSITION_CONFLICT`, `IDEMPOTENCY_PAYLOAD_MISMATCH`, `RESOURCE_VERSION_CONFLICT`, `429 RATE_LIMITED` ve `503 DEPENDENCY_UNAVAILABLE` gibi daha ayrıntılı contract’lar hedeflemektedir [3]. Mevcut kaynakta bunların hepsi için tek ve merkezi bir runtime mapping bulunmadığı için web client, yalnızca ideal tasarım örneğine güvenmemeli; status, optional `code` ve body fallback’i birlikte ele almalıdır.

## 401 Refresh

Web client authenticated request’lere `Authorization: Bearer <accessToken>` eklemelidir. Bir request `401` döndürdüğünde client aşağıdaki sınırlandırılmış akışı uygulamalıdır:

| Adım | Kesin davranış |
|---:|---|
| 1 | Login ve refresh endpoint’lerinin kendi 401 response’larını access-token expiry gibi yorumlama. Public catalog ve public quote request 401 döndürürse otomatik refresh başlatma. |
| 2 | Authenticated access request’ten gelen ilk 401 için tek bir shared refresh promise oluştur. Paralel request’ler ayrı ayrı refresh çağrısı yapmamalıdır. |
| 3 | `POST /api/v1/auth/refresh` çağrısını mevcut refresh-token sözleşmesine göre gönder. Başarılı response’ta access ve refresh token çiftini atomik olarak değiştir. |
| 4 | Orijinal request’i en fazla bir kez tekrar dene. State-changing request’lerde aynı `Idempotency-Key` ve aynı payload korunmadan replay yapma. Yeni key üretmek aynı mutation’ın iki kez uygulanmasına yol açabilir. |
| 5 | Refresh 401 döndürürse session’ı temizle, memory token’larını sil, cookie/BFF session’ını kapat ve kullanıcıyı `/giris` akışına yönlendir. |
| 6 | Refresh veya replay sırasında tekrar 401 alınırsa sonsuz loop başlatma. Network hatası ile kimlik doğrulama hatasını telemetry’de ayır. |

Mevcut backend access token’ı revoke etmediği için logout sonrasında access token’ın expiry’sine kadar yaşayabilmesi client tarafından varsayılmalıdır. Logout akışında server çağrısı başarısız olsa bile client local session state’ini temizlemelidir.

## 403 Permission

Authorization policy’leri permission claim tabanlıdır. Web client `/me` response’undaki permission listesini navigation ve ekran erişim görünümü için kullanabilir; ancak bu liste backend policy’sinin yerine geçmez. Kullanıcıda permission yoksa backend 403 döndürür, client refresh denemeden `PermissionDenied` state’i üretmelidir.

403 response body’si bazı endpoint’lerde bare framework response olabileceği için normalizer yalnızca `code` alanına bağımlı olmamalıdır. `status === 403` öncelikli sınıflandırma olmalı, varsa `FORBIDDEN` veya `OVERRIDE_PERMISSION_REQUIRED` kodları ikinci seviye bilgi olarak taşınmalıdır. Override gerektiren LoadPlan akışlarında gerekçe ve permission ayrımı UI’da gösterilebilir; override yetkisi frontend’de üretilemez [23].

## 409 Concurrency

Architecture kararları `If-Match`/ETag, public row version, Read Committed, deterministik source-row lock, transaction-local re-read ve typed concurrency ProblemDetails yaklaşımını kabul etmiştir [7]. Gerçek implementation’da ortak logistics controller `If-Match` header’ını tırnaklardan arındırarak `long row_version` olarak parse eder; eksik veya geçersiz değer `ArgumentException` ile 400’e dönüşür [24].

Uygulanacak web davranışı şudur: 409 geldiğinde client önce ilgili resource’u güncel olarak tekrar okumalı, stale formu otomatik olarak command’e çevirmemeli, kullanıcıya hangi alanların değiştiğini göstermeli ve yalnızca kullanıcı onayından sonra yeni row version ile yeni command üretmelidir. Aynı stale payload’ın otomatik tekrar gönderilmesi kabul edilmez. Mevcut kaynakta 409’un merkezi olarak yalnızca `DbUpdateConcurrencyException` için üretildiği; state transition ve idempotency payload mismatch hatalarının ise bugün 422’ye düştüğü unutulmamalıdır.

## Idempotency

Header adı kesin olarak **`Idempotency-Key`**’dir. Middleware, `POST` request’lerinde aşağıdaki kritik path segment’lerinde header yoksa request’i application katmanına göndermeden `400 MISSING_IDEMPOTENCY_KEY` ile durdurur: `/api/v1/orders`, `/api/v1/delivery-notes`, `/api/v1/invoices`, `/api/v1/payments`, `/api/v1/shipments`, `/api/v1/production`, `/api/v1/quote-requests`, `/api/v1/warehouse-transfers`, `/api/v1/vehicle-types`, `/api/v1/vehicles`, `/api/v1/drivers`, `/api/v1/route-plans`, `/api/v1/load-plans`, `/api/v1/load-verification` ve `/api/v1/dispatch-runs` [14].

Header zorunluluğu ile gerçek replay davranışı aynı şey değildir. Middleware yalnızca key’in varlığını kontrol eder ve key’i `HttpContext.Items` içine koyar. Replay, payload hash ve response persistence service katmanında `IIdempotencyStore` kullanan command’lerde gerçekleşir. Sales service örneğinde scope actor/action/resource bağlamında oluşturulur; aynı scope + key + aynı payload hash için stored response body deserialize edilerek döndürülür, aynı key farklı payload ile kullanılırsa `IDEMPOTENCY_PAYLOAD_MISMATCH` fırlatılır [20].

| Command grubu | Idempotency gereksinimi | Client davranışı |
|---|---|---|
| Order create/submit/approve/reject | Gerekli; middleware + service replay | Form submit başına bir key üret; network retry’de aynı key/payload’ı koru. |
| Delivery note create/issue | Gerekli; stok/reservation/ledger etkisi nedeniyle kritik | Issue sırasında key ve gerekiyorsa `If-Match` zorunlu. |
| Invoice create/issue ve payment apply | Gerekli; belge/cari etkisi nedeniyle kritik | Aynı payment/invoice mutation’ı yeni key ile yeniden gönderme. |
| Shipment, route, load-plan, load-verification, dispatch | Gerekli; fiziksel plan/state etkisi vardır | Header’ı ortak request builder otomatik eklemeli; scan/complete akışında key kaybolmamalı. |
| Production completion ve stock transfer | Gerekli; stok ledger etkisi vardır | Offline sessiz commit yok; retry yalnızca idempotent request ile yapılmalı. |
| Internal quote-request review | Gerekli; review state ve customer binding etkisi vardır | Aynı review command’i aynı key/payload ile tekrar edilebilir. |
| Public quote-request create | **Mevcut implementation’da korunmuyor** | Frontend her submit için client request id gönderebilir, fakat backend şu an bunu saklamaz/replay etmez. Bu gap kapanmadan duplicate talep riski vardır. |
| Read-only GET, `me`, catalog list/detail | Normalde gerekmez | Key üretme; correlation ve auth header kurallarını ayrı uygula. |

Public quote request özellikle istisnadır: gerçek path `/api/v1/public/quote-requests` olup middleware listesinde yalnızca `/api/v1/quote-requests` vardır. Ayrıca `CreatePublicQuoteRequestAsync` içinde idempotency store lookup/save bulunmamaktadır. Aynı kullanıcının timeout sonrası aynı talebi yeniden göndermesi iki `QuoteRequest` oluşturabilir [14] [18] [20].

Response davranışında service’ler body ve status code saklasa da controller response status’unu seçmeye devam eder. Örneğin order create controller ilk veya replay sonucunda `Created(...)` döndürür; service içindeki persisted status code’un replay sırasında doğrudan HTTP response olarak yeniden yazıldığı merkezi bir middleware yoktur. WEB client için anlamlı sözleşme, aynı key ve payload tekrarında aynı resource DTO’sunun dönmesi; status/Location ayrıntısının endpoint bazında ayrıca test edilmesi olmalıdır.

`409` kullanımı mevcut backend’de dar kapsamlıdır: explicit `Conflict()` response’u bulunmamaktadır; yalnızca `DbUpdateConcurrencyException` → `409 QUANTITY_CONCURRENCY_CONFLICT` mapping’i vardır [13]. `IDEMPOTENCY_PAYLOAD_MISMATCH` bugün 422 olarak dönebildiği için client hem 409 hem 422’de idempotency/conflict code’larını okuyabilmeli, ancak aynı key’i farklı payload ile tekrar kullanmamalıdır.

## Public Catalog

Public yüzey yalnızca `IsActive && IsPublic` ürünleri sorgular. Fiyat, stok miktarı, risk, cari, maliyet veya iç operasyon alanları public DTO’ya taşınmaz. Ürün listesinde arama `name`, `code` veya `slug` üzerinden; kategori filtresi kategori slug’ı üzerinden yapılır. Liste adı ve koduna göre sıralanır, `page` en az 1’e ve `pageSize` 1–100 aralığına clamp edilir [16].

| Use-case | Gerçek endpoint | Request | Response | Hata/authorization | Durum |
|---|---|---|---|---|---|
| Product list | `GET /api/v1/public/catalog/products` | Query: `search?`, `category?`, `page?` (default 1), `pageSize?` (default 24) | `ProductPage`: `items`, `page`, `pageSize`, `totalCount`, `hasNextPage`; product item’da `id`, `code`, `slug`, `name`, `description?`, `sizeLabel?`, category, `baseUom`, effective/sellable `packagings`, `primaryImageUrl?` | Anonymous; normal success 200 | **Existing** [15] [17]. |
| Product detail | `GET /api/v1/public/catalog/products/{slug}` | Route slug | Aynı `PublicProductDto` | Anonymous; ürün yoksa custom minimal 404 ve `RESOURCE_NOT_FOUND` | **Existing** [15] [17]. |
| Quote request | `POST /api/v1/public/quote-requests` | `companyName`, `contactName`, `phone`, `email`, `items[]`, `note?`, `consentAccepted`; item’da `productId`, `enteredQuantity`, `enteredPackagingId?`, `viewMode` | `201 Created`; `QuoteRequestDto`: `id`, `requestNumber`, `status`, `source`, candidate contact fields, items with `quantityBase` and `packagingSnapshot`, `createdAt` | Anonymous; consent yoksa veya company/contact/items eksikse domain error 422; product/packaging bulunamazsa 422 | **Existing** [18] [19] [20]. |
| Quote basket | Server-side basket resource, basket item add/update/remove endpoint’leri | Tasarımda UI/quote basket kavramı var, fakat gerçek route/DTO yok | Yok | **NOT IMPLEMENTED** | WEB SLICE 003 client yalnızca ileride kullanılacak abstraction sınırını hazırlayabilir; olmayan endpoint’i çağırmamalı. |
| Quote request submission/finalization | Ayrı basket finalize veya `POST /public/quote-requests/{basketId}/submit` endpoint’i | Yok | Yok | **NOT IMPLEMENTED** | Mevcut public POST doğrudan tek transaction içinde talep oluşturur; ayrı submit contract’ı yoktur. |

Public quote request service, `ConsentAccepted` kontrolü yapar; firma, iletişim, telefon, email ve en az bir item zorunludur. Item miktarının `quantityBase` değeri frontend’den doğruluk kaynağı olarak alınmaz; ürünün geçerli packaging katsayısı backend’de preview üzerinden hesaplanır ve packaging snapshot yazılır [20]. Bu davranış, domain modelindeki “entered quantity + packaging, server-side base quantity” kuralıyla uyumludur [4].

Public catalog tasarımında rate limit, honeypot/CAPTCHA, consent ve saklama/silme kontrolleri karar olarak kabul edilmiştir; runtime’da bu kontrolleri gerçekleştiren bir rate-limiter veya bot protection pipeline’ı bu incelemede doğrulanmamıştır. Bu nedenle public quote request’i production-ready abuse protection varmış gibi değerlendirilmemelidir [7] [12].

## Missing Endpoints

Aşağıdaki yüzeyler tasarım contract’ında tanımlı olmakla birlikte mevcut controller route’larında doğrulanmamıştır. Her satır özellikle **NOT IMPLEMENTED** olarak işaretlenmiştir; bunlar mevcut endpoint gibi web client’a bağlanmamalıdır.

| Tasarım yüzeyi | Beklenen contract | Gerçek durum |
|---|---|---|
| Internal product CRUD | `GET/POST/PATCH /api/v1/products`, product packaging management | **NOT IMPLEMENTED** olarak değerlendirilmelidir. Mevcut product controller surface public catalog ve mobile quantity utility ile sınırlıdır. |
| Users and roles | `/api/v1/users`, `/api/v1/roles`, user role assignment | **NOT IMPLEMENTED**. Identity persistence entity’lerinin bulunması HTTP endpoint’in bulunduğu anlamına gelmez. |
| Quote resource | `POST /api/v1/quotes`, `POST /api/v1/quotes/{id}/issue` | **NOT IMPLEMENTED**. Mevcut public POST quote değil, `QuoteRequest` oluşturur. |
| Server-side quote basket | Basket create/item add/update/remove | **NOT IMPLEMENTED**. Basket yalnızca ileride web UI state’i olarak tasarlanabilir; server contract uydurulmamalıdır. |
| Public basket submission | Basket’i quote request’e dönüştüren ayrı submit/finalize komutu | **NOT IMPLEMENTED**. Mevcut tek public request endpoint’i doğrudan talep oluşturur. |
| Order list and cancel | `GET /api/v1/orders`, `POST /api/v1/orders/{id}/cancel` | **NOT IMPLEMENTED** olarak işaretlenmelidir; mevcut SalesController’da yalnızca order detail ve submit/approve/reject command’leri doğrulanmıştır [18]. |
| Nested delivery-note creation | `POST /api/v1/orders/{orderId}/delivery-notes` | **NOT IMPLEMENTED** exact route olarak. Mevcut route `POST /api/v1/delivery-notes`’tır. |
| Delivery-note validate/reverse/remainder close | `/delivery-notes/{id}/validate`, `/reverse`, `/close-remainder` | **NOT IMPLEMENTED** exact route olarak. Mevcut controller create/get/issue ile sınırlıdır [26]. |
| Invoiceable quantity, invoice list, validate/reverse | `/delivery-notes/{id}/invoiceable-quantities`, `GET /invoices`, `/validate`, `/reverse` | **NOT IMPLEMENTED** exact route olarak. Mevcut invoice surface create/get/issue ile sınırlıdır [27]. |
| Public session/cookie refresh contract | `Set-Cookie` tabanlı refresh veya BFF session endpoint’i | **NOT IMPLEMENTED**. Mevcut refresh body’de ham refresh token bekler; web security kararı ayrıca verilmelidir. |

Tasarım dokümanındaki route listesi architecture yönüdür; kaynak kodda controller/action bulunmadığı sürece implementation contract değildir. Bu ayrım, özellikle quote basket, quote resource ve `/api/v1/products` için korunmalıdır [3].

## Web Client Architecture

WEB SLICE 003 için önerilen abstraction, backend’in mevcut davranışını saklamalı ve ileride normalize edilecek alanları tek noktada toplamalıdır. Client component’leri doğrudan `fetch` veya raw ProblemDetails ayrıştırmamalıdır.

| Abstraction | Sorumluluk | Kesin sınır |
|---|---|---|
| `apiClient` | Base URL, JSON serialization, `Authorization`, `X-Correlation-Id`, `Idempotency-Key`, `If-Match`, timeout ve response parsing | Endpoint path’leri `/api/v1` ile exact kullanılmalı; entity doğrudan UI’a sızdırılmamalı. |
| `authClient` | Login, refresh, logout, me çağrıları | Login/refresh response’taki token çiftini session manager’a teslim eder; token’ı component state’ine yaymaz. |
| `session handling` | Access token memory state’i, user summary, auth status, single-flight refresh, session clear | Access token ve özellikle raw refresh token `localStorage`’a yazılmamalı. Cookie/BFF kararı yoksa bu açık bir security gap olarak raporlanmalı. |
| `error normalization` | Status + optional ProblemDetails code + request/correlation id ayrıştırması | Body boş veya framework default olsa bile 401/403/404/500 anlamlı typed client error’a dönüşmeli. |
| `request headers` | Authenticated request’lerde bearer; write command’lerde idempotency; concurrency command’lerinde If-Match; tüm request’lerde correlation | `Idempotency-Key` network retry’de değişmemeli; yeni business mutation’da yeni key üretilmeli. |
| `refresh handling` | 401 single-flight, en fazla bir refresh ve bir replay | Login/refresh/public request’lerinde loop yok; refresh 401’de session temizlenir. |
| `logout` | Server revoke çağrısı ve client state cleanup | Server logout hata verse bile memory token/cookie/BFF state’i temizlenir. |
| `me` | User/role/permission summary ve initial session bootstrap | Permission listesi UI gate içindir; backend authorization’ın yerine geçmez. |

### Header contract

| Header | Ne zaman | Client davranışı |
|---|---|---|
| `Authorization: Bearer <accessToken>` | Authenticated endpoint’ler | Access token memory’den okunur; login/refresh public body flow’una eklenmez. |
| `X-Correlation-Id` | Tüm API request’leri | Client bir request id üretebilir; server gelen değeri echo eder veya yoksa üretir. Hata telemetry’sinde `requestId` ve `correlationId` birlikte saklanır. |
| `Idempotency-Key` | Side-effect üreten POST’lar | Bir mutation intent’i için stabil key; aynı payload ile retry; farklı payload için yeni intent ve yeni key. |
| `If-Match` | Resource row version kullanan mutation’lar | Backend’in beklediği numeric row version’ı, gerektiğinde HTTP ETag biçimindeki tırnakları koruyarak gönder; 409 sonrası fresh read yap. |
| `Content-Type` | JSON request body | `application/json`; request contract’ındaki entered quantity ve packaging alanları gönderilir. |

### Error normalization result

Client error modelinde en az `status`, `code?`, `title?`, `detail?`, `requestId?`, `correlationId?`, `retryable?`, `errors?`, `actions?` ve `rawResponse` tutulmalıdır. `status` ana sınıflandırma kaynağıdır; `code` farklı endpoint’lerde aynı status altında ayrıntı sağlar. Böylece bare 403 veya bare 404 response’ları da güvenli şekilde işlenir.

### Session bootstrap ve logout

Uygulama açılışında mevcut güvenli session mekanizması varsa önce session restore, sonra `/api/v1/auth/me` çağrısı yapılmalıdır. `/me` 401 döndürürse session expired state’i; 403 beklenmeyen authorization state’i; 200 dönerse permission-aware shell state’i üretilmelidir. Logout, refresh token revoke çağrısını denedikten sonra response başarılı veya başarısız olsa da client session state’ini temizlemelidir.

## Security Risks

| Risk | Kaynak kanıtı | Etki | WEB SLICE 003 kuralı |
|---|---|---|---|
| Default signing key | `AuthOptions.SigningKey` development-only placeholder değere sahiptir [11] | Production’da override edilmezse JWT forgery riski | Secret manager/environment zorunlu; placeholder ile production startup engellenmeli. |
| Raw refresh token response | `AuthTokens` ham `RefreshToken` döndürür [9] [10] | XSS veya yanlış client persistence ile uzun ömürlü session hırsızlığı | Refresh token storage kararı body-based mevcut contract’a rağmen HttpOnly cookie/BFF yönünde alınmalı. |
| `localStorage` XSS riski | Web token storage henüz implementation değildir | Her injected script token’ı okuyabilir; kalıcı access elde edilebilir | `localStorage` kullanma; access memory, refresh HttpOnly/BFF. |
| Inconsistent 401/403/404 body | Global mapper yalnızca exception’ları map eder; framework challenge/forbid ve bare `NotFound()` yolları vardır [12] [13] [15] [23] | UI parser yalnızca ProblemDetails beklerse hata state’i kaybolur | Status-first fallback normalizer zorunlu. |
| 409 contract gap | Explicit 409 yalnızca `DbUpdateConcurrencyException` için vardır [13] | State/idempotency conflict’leri 422’ye düşebilir | Client 409 ve 422’de typed conflict code’larını okuyabilmeli; backend mapping ayrıca düzeltilmeli. |
| Refresh race/session invalidation | Refresh rotation var; explicit lock/family/session revocation yok [9] | Paralel refresh taleplerinde belirsiz davranış; access token logout sonrası yaşayabilir | Single-flight refresh, session clear ve güvenli retry. |
| Public quote duplicate | Public quote path middleware prefix listesinde yok; service idempotency store kullanmıyor [14] [20] | Timeout/retry duplicate quote request üretebilir | Public submit için server-side idempotency veya unique client request id contract’ı eklenmeden reliable retry iddiası kurma. |
| Public abuse controls | Public catalog/public request için rate limit ve bot/consent kararları tasarımda vardır, runtime pipeline’da doğrulanmamıştır [7] [12] | Spam, automated quote abuse ve PII riskleri | Public endpoint’ler production gate’inden önce rate limit/CAPTCHA/honeypot/retention testlerinden geçmeli. |
| CORS/secure headers görünürlüğü | `Program.cs` içinde `AddCors`, custom security header, HSTS veya rate limiter kaydı bulunmamaktadır [12] | Browser deployment’ta origin, clickjacking, MIME ve transport riskleri | Deployment/security slice’ında explicit CORS allowlist, HTTPS/HSTS ve secure header policy eklenmeli. |
| Claim staleness | `/me` token claim’lerinden permission summary üretir; permissionVersion sabittir [8] | Yetki değişikliği access token expiry’sine kadar yansımayabilir | Permission change/session invalidation contract’ı netleştirilmeli; UI permission güvenlik kaynağı sayılmamalı. |
| Record-level authorization gap | Endpoint permission policy’leri mevcut, fakat bu inceleme bütün aggregate ownership/company/warehouse scope’larını exhaustive doğrulamadı | IDOR/BOLA riski gözden kaçabilir | WEB SLICE acceptance’ında cross-record access testleri zorunlu tutulmalı. |

## Recommended Implementation Order

1. **Contract gap’lerini kapat:** Refresh token’ın JSON body ile mi yoksa HttpOnly cookie/BFF ile mi taşınacağı; 401/403/404 normalize formatı; idempotency mismatch’in 409 mu 422 mi olacağı; public quote idempotency’sinin nasıl sağlanacağı netleştirilmeden feature implementation başlatılmamalıdır.

2. **Typed transport sınırını oluştur:** `apiClient`, `authClient`, session state ve error normalizer tek bir transport katmanında tanımlanmalı; sayfa ve component’ler doğrudan raw HTTP response ayrıştırmamalıdır. Mevcut implementation’da gerçekten bulunan endpoint’ler exact path’leriyle kullanılmalı, design-only route’lar eklenmemelidir.

3. **Header policy’sini merkezileştir:** Auth bearer, correlation id, idempotency key ve `If-Match` üretimi request builder/interceptor seviyesinde yapılmalı. Ancak public quote request için mevcut backend açığı nedeniyle key göndermek tek başına yeterli kabul edilmemelidir.

4. **Auth lifecycle’ı uygula:** Login → memory access token → `/me`; access expiry → single-flight refresh → one replay; refresh failure → session clear → `/giris`; logout → revoke attempt + unconditional client cleanup sırası uygulanmalıdır.

5. **Permission-aware shell’i bağla:** `/me` permission summary navigation ve görünürlük için kullanılmalı; 403 response’u backend kararının nihai kanıtı olarak `PermissionDenied` state’ine dönüşmelidir. Role adına göre frontend-only authorization yapılmamalıdır.

6. **Public catalog contract’ını ayrı tut:** Catalog list/detail DTO’su internal product, stock, price, risk veya finance alanlarını tüketmemeli. Quote basket UI state’i için backend endpoint varmış gibi davranılmamalı; server-side basket contract’ı ayrıca tasarlanmalıdır.

7. **Mutation retry politikasını uygula:** Side-effect POST’larda key stabil kalmalı; 401 replay yalnızca aynı key/payload ile yapılmalı; 409 fresh-read ve user-confirmed retry gerektirmeli; 422 business/idempotency mismatch körlemesine tekrar edilmemelidir.

8. **Contract testlerini ekle:** Auth dört endpoint’i, token rotation/revoke, invalid refresh, 401/403/404 fallback, idempotency same-key replay, payload mismatch, missing-key 400, `If-Match` conflict, public quote duplicate ve public DTO data-leak senaryoları acceptance test olarak yazılmalıdır.

9. **Security gate’i tamamla:** Signing key secret zorunluluğu, CORS allowlist, HTTPS/HSTS, secure headers, rate limiting, public abuse controls, refresh cookie/BFF davranışı ve cross-record authorization testleri tamamlanmadan WEB SLICE 003 production-ready kabul edilmemelidir.

## Implementation sırasında uyulacak kesin kurallar

- **KOD YAZMA ve contract uydurma:** Bu review’de doğrulanmayan endpoint’i existing olarak gösterme. Yeni endpoint, database alanı veya migration bu raporun çıktısı değildir.
- Base path olarak yalnızca `/api/v1` kullan; path isimlerini design dokümanındaki öneriden değil, mevcut controller route’undan doğrula.
- Access token’ı `localStorage` veya kalıcı JavaScript-readable storage’a yazma. Refresh token için HttpOnly cookie/BFF kararı yoksa bu durumu açık security blocker olarak taşı.
- Raw access/refresh token’ı loglama, telemetry veya error detail’e koyma.
- 401 geldiğinde yalnızca authenticated access request’leri için single-flight refresh uygula; en fazla bir refresh ve bir replay yap.
- State-changing replay’de aynı `Idempotency-Key` ve aynı payload korunmadan request tekrarlama. Yeni business intent için yeni key üret.
- 403’te refresh deneme; `PermissionDenied` state’i üret. UI permission listesi backend authorization’ın yerine geçmez.
- 409’da fresh read ve kullanıcı kontrollü yeniden değerlendirme uygula; stale request’i körlemesine replay etme.
- 422’de `code` ve `errors` alanlarını işle; `IDEMPOTENCY_PAYLOAD_MISMATCH` için aynı key’i farklı payload ile yeniden kullanma.
- Side-effect POST’larda `Idempotency-Key` gönder; missing-key 400’ü client request-builder hatası olarak izle.
- `X-Correlation-Id` gönder veya server tarafından üretilen değeri response/telemetry ile ilişkilendir.
- `If-Match` gereken resource mutation’larında row version gönder; 409 sonrası yeni version almadan tekrar issue etme.
- Miktar işlemlerinde `quantityBase` değerini doğruluk kaynağı sayma; entered quantity + packaging gönder, base quantity’yi backend’in hesaplamasına izin ver.
- Public catalog response’unda stok, fiyat, risk, cari, maliyet, personel veya allocation detayı isteme/kullanma.
- Public quote request’in mevcut implementation’da idempotent olmadığını açıkça kabul et; reliable retry için backend contract tamamlanmadan kullanıcı submit’ini otomatik tekrar gönderme.
- Logout server çağrısı başarısız olsa dahi client memory/session state’ini temizle.
- WEB SLICE 003 kapsamı auth/session/API client’tır; quote basket, internal product CRUD, server-side quote resource ve design-only route’lar sonraki contract/implementation slice’larına bırakılmalıdır.

> **NEXT SLICE:**
>
> **WEB SLICE 003 — Auth & API Client**

## References

[1]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/AGENTS.md "Repository agent instructions"
[2]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/implementation-web-mobile-slice.md "WEB/mobile slice implementation status"
[3]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/architecture-api-contracts.md "Canonical architecture API contracts"
[4]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/domain-model.md "Canonical domain model"
[5]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/business-workflows.md "Canonical business workflows"
[6]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/database-technical-architecture.md "Database technical architecture"
[7]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/decision-log.md "Decision log and accepted ADR baseline"
[8]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Controllers/AuthController.cs "Implemented authentication controller"
[9]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Infrastructure/Authentication/AuthenticationService.cs "Implemented authentication service"
[10]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Application/Identity/AuthenticationContracts.cs "Authentication DTO contracts"
[11]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Infrastructure/Authentication/AuthOptions.cs "Authentication options"
[12]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Program.cs "API bootstrap and authorization policies"
[13]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Errors/ExceptionProblemDetailsMiddleware.cs "Global ProblemDetails mapping"
[14]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Idempotency/IdempotencyKeyMiddleware.cs "Idempotency header middleware"
[15]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Controllers/PublicCatalogController.cs "Public catalog controller"
[16]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Infrastructure/Products/ProductCatalogService.cs "Public catalog service"
[17]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Application/Products/ProductContracts.cs "Product and quantity DTO contracts"
[18]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Controllers/SalesController.cs "Sales and public quote controllers"
[19]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Application/Sales/SalesContracts.cs "Sales and public quote DTO contracts"
[20]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Infrastructure/Sales/SalesCommandService.cs "Sales command and idempotency behavior"
[21]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Controllers/QuoteRequestsController.cs "Internal quote-request controller"
[22]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Controllers/MobileOperationsController.cs "Mobile quantity and barcode controller"
[23]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Controllers/LoadPlansController.cs "Load-plan authorization behavior"
[24]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Controllers/LogisticsControllerBase.cs "Shared logistics header helpers"
[25]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Controllers/DeliveryNotesController.cs "Delivery-note controller"
[26]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Controllers/InvoicesController.cs "Invoice controller"
[27]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/src/FactoryErp.Api/Controllers/PaymentsController.cs "Payment controller"
