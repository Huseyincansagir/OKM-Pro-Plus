# Factory ERP — Ticari ölçek hazırlığı (500 firma / binlerce kullanıcı)

**Tarih:** 2026-08-20  
**Kod HEAD:** `480bea9`  
**Kaynak:** gerçek git/kod + `decision-log.md` + `implementation-web-mobile-slice.md`  
**Amaç:** Ürün sahibi olarak satılabilirlik; uygulama değil, kanıt kaydı.  
**Durum:** SATIŞA KAPALI — 500 kiracı / paylaşımlı SaaS. Tek fabrikaya kontrollü on-prem MVP henüz tamamlanmamış.

> Bu belge O-001–O-014 kabulünü iptal etmez. 500 firma eşzamanlı kullanım **yeni kapsamdır**; A-001’i tetikler. Karar kaydı: `decision-log.md` **O-015 OPEN**.

## 1. Karar

Bugünkü kod **500 firmaya aynı anda satılamaz.**

Tasarım sözleşmesi tek şirket, şirket içi ERP-lite’dır:

| Karar | Metin | Dosya |
|---|---|---|
| D-001 | Şirket içi merkezi ERP-lite | `decision-log.md` |
| A-001 | İlk sürüm tek şirket; multi-company tenant yok; ileride `company_id` sınırı | `decision-log.md` |
| O-011 | Ubuntu LTS + Compose + PostgreSQL + reverse proxy + **LAN HTTPS**; iç endpoint internete açılmaz | `decision-log.md` |
| Mimari | Modüler monolith, Kafka/K8s yok | `database-technical-architecture.md` |

Kaynakta `TenantId` / `CompanyId` / global query filter **yok**. `users.user_name`, `product_barcodes.barcode` (aktif), `document_sequences(document_type, year)`, ürün kodu/slug global unique. 500 firma tek veritabanında: çapraz sızıntı + numara/barkod çarpışması.

**Satış modeli seçilmeden tenant kodu yazılmamalı.** Üç dürüst yol:

| Yol | Ne demek | Bu kodla |
|---|---|---|
| A. On-prem N kopya | Her fabrikaya ayrı Compose + ayrı PostgreSQL | O-011 ile uyumlu; 500 kurulum için control plane, faturalama, uzaktan yama yok |
| B. Paylaşımlı SaaS | Tek küme, satırda `company_id`, RLS, kiracı JWT | A-001’i kırar; şema, unique, API, UI, backup yeniden |
| C. Şema/DB per tenant | 500 PostgreSQL veya 500 schema | Operasyon ve migrator tasarımı yok |

Binlerce eşzamanlı kullanıcı **yol A’da** her fabrikada onlarca/yüzlerce oturum; **yol B/C’de** on binlerce bağlantı, kuyruk, yatay ölçek. Hiçbiri ölçülmedi.

## 2. Yapılanlar — kalite (dürüst)

Yapılan dilimler **sahte KPI / client `quantityBase` / viewMode=ambalaj karışımı** üretmiyor. Bu, MVP omurgası için doğru.

### Sağlam olan

- Clean Architecture + permission claim politikaları API’de; frontend yetki yalnızca UX (`AGENTS.md`).
- JWT varsayılan 15 dk (prod compose 30) + refresh 14 gün, refresh hash’li ve rotate; web `fe_access` / `fe_refresh` HttpOnly, `SameSite=lax`, production `secure`. API cookie basmaz; BFF basar.
- Parola PBKDF2-SHA256, 120_000 iterasyon, sabit zamanlı karşılaştırma (`PasswordHasher.cs`).
- Idempotency-Key komut öneklerinde; `row_version` / If-Match.
- Stok/irsaliye/fatura/sayı/sefer komutlarında `SELECT … FOR UPDATE` + belge numarası kilidi.
- Pozitif `quantity_base` ledger; transfer complete iki hareket; sayım complete `CountIn`/`CountOut`; teslim recipient zorunlu.
- Public katalog sayfalı (`page`/`pageSize`, max 100) ve iç stok/fiyat sızdırmıyor (tasarım D-004).
- SMTP yoksa e-posta **Queued**, sahte Sent yok.
- Backup script + 14 gün retention + restore-smoke **compose profili** var (`deploy/compose.prod.yaml`, `deploy/backup/`).
- Health: `/health/live`, `/health/ready`, `/health/startup` + postgres check.
- Domain unit 129, web Vitest 206 (`480bea9`); architecture testleri mevcut.

### WEB 001–018 omurga (tek şirket)

```text
Public katalog → teklif talebi → müşteri → teklif → issue
  → [UI’da siparişe dönüş yok; IssueQuote sipariş açmaz]
  → sipariş (ayrı POST /orders) submit/approve
  → irsaliye (remainingQty, BaseUnit) → issue (stok çıkışı)
  → sevkiyat Preparing → rota/paket
  → [load-plan create/lock UI yok; POST .../dispatch UI’da yok]
  → sefer yalnızca zaten varsa: onay/kalkış/varış/metin POD/complete
Depo: stok listesi, transfer create/complete/cancel, hareket, sayım
Personel: employee master (maaş yok — dürüst)
Barkod: USB Enter → POST /mobile/barcodes/resolve (kamera yok)
```

Lojistik **backend** (load-plan, vehicle-fit, FFD, load-verify, dispatch prepare) web’den ileride. Web seferi **hazırlamaz**, var olanı yürütür. Bu huni tek fabrikada kısmen izlenebilir; satılabilir kapalı ürün değil.

## 3. 500 firma / binlerce kullanıcı — P0 boşluklar

Sıra, sonradan saldırı listesidir. O-015 kapanmadan tenant şeması yazılmaz.

| ID | Boşluk | Kanıt | Neden blok |
|---|---|---|---|
| S-001 | Kiracı modeli yok | `src/` içinde tenant/company_id yok | Veri izolasyonu yok |
| S-002 | Global unique’ler kiracıyı varsaymıyor | `product_barcodes.barcode` unique; `users.user_name` unique; `document_sequences (type, year)` unique | Firma A’nın SO-2026-000001’i firma B’yi ezer |
| S-003 | Control plane yok | Onboarding, fatura, kota, kiracı admin, yama orkestrasyonu yok | 500 müşteri elde işletilemez |
| S-004 | Liste API’leri sayfalı değil | D-014: server-side pagination. Gerçek: public katalog hariç `Take(100)` (sipariş, stok, transfer, sayım, personel, irsaliye, fatura, sefer…) | Binlerce belge tarayıcıyı ve API’yi keser; sessiz 100 kesim |
| S-005 | Public abuse yarım | O-009. `AddRateLimiter` yok. Public quote AllowAnonymous, Idempotency-Key yok. UI honeypot (`quote-checkout`) server’da yok. `CONSENT_REQUIRED` server’da var. CAPTCHA yok | Spam + maliyet; consent tek başına yetmez |
| S-006 | Login brute-force yok | `AuthenticationService.LoginAsync` başarısızda `null`; lockout/sayaç yok | 500 public yüzeyde parola tarama |
| S-007 | Outbox worker yok | `outbox_messages` yazılıyor; `IHostedService` / publisher **yok** | ADR-008 yarım; mail/bildirim birikmesi |
| S-008 | Observability yok | Serilog/OpenTelemetry yok; `Logging` default | 500 kiracıda kök neden bulunamaz |
| S-009 | Ölçek ayarı yok | Npgsql pool varsayılan; Redis yok; nginx `worker_connections 1024`; Compose’da web servisi yok | Binlerce eşzamanlı oturum tasarlanmadı |
| S-010 | Prod compose web + TLS yok | `compose.prod.yaml`: postgres, migrator, backup, **api**, reverse-proxy. `nginx.prod.conf` `location / { return 404; }` HTTP 80 | Tek fabrikada bile tam yığın kanıtı yok |
| S-011 | Yük / soak testi yok | Kanıt yok | “Binlerce kişi” iddiası uydurma olur |
| S-012 | CI entegrasyonu kırılgan | `.github/workflows/domain-ci.yml` tüm sln; Postgres service yok. Web vitest/lint workflow’da yok. Planlanan `pull-request.yml` / `release.yml` / `security.yml` dosyaları yok | Yeşil CI ≠ üretim kanıtı |
| S-013 | Oturum iptali yarım | Logout yalnızca o refresh token’ı iptal eder. Access JWT blacklist yok (`jti` yazılır, kontrol edilmez). `IsActive=false` access süresi bitene kadar geçerli | Çalınan 15–30 dk token + pasif kullanıcı |
| S-014 | Satır sahipliği yok | GET-by-id = permission + `Id ==`. `ICurrentUserAccessor` tanımlı, **kayıtlı değil**. Tek fabrikada “claim’i olan herkes her satırı görür”; paylaşımlı DB’de kiracılar arası IDOR | 500 firma tek DB = çapraz sızıntı |
| S-015 | `/me` sahte şirket | `AuthController`: `company = { code = "default", name = "Factory ERP" }` | Kiracı kimliği yok |
| S-016 | Yedek cron yok | `backup.sh` + compose **profile**, `restart: no` — tek seferlik iş. O-010 günlük full backup | 14 gün retention script’te var, zamanlayıcı yok |
| S-017 | Idempotency kapsamı | Unique `(scope, key)`; `company_id` yok. Public quote-request, `/mobile/*`, `/physical-logistics/*`, auth POST prefix dışında | Tasarım metni ile kod ayrışıyor |

## 4. Tek fabrikayı satmadan önce — P1 ürün boşlukları

Mevcut kararlarla **bir** fabrikaya para alınacaksa bunlar kapanmalı. Sahte ekran açılmaz.

| ID | Eksik | Not |
|---|---|---|
| P-001 | Load-plan create/lock sihirbazı | API var; web yalnızca GET status. Vehicle-fit / load-verify UI yok |
| P-002 | Sefer hazırlama UI | `POST /route-plans/{id}/dispatch` web’den çağrılmıyor; kart yalnızca mevcut run’ı yürütür |
| P-003 | Teklif → sipariş | `IssueQuote` sipariş açmaz; `POST /orders` var; UI dönüştürme yok |
| P-004 | Flutter / saha mobil | `apps/` yalnızca `web`; `pubspec.yaml` yok; kamera yok |
| P-005 | Dosya POD (imza/foto) | Yalnızca recipient/note metin; multipart yok |
| P-006 | Fatura oluştur/kes UI | `POST /invoices` + issue var; `/cari` liste; kart yok |
| P-007 | Ödeme | `POST /payments` var; **GET list yok**; web yok |
| P-008 | Üretim complete UI | API create/start/record/complete var; `/uretim` yalnızca GET list |
| P-009 | Ürün/ambalaj yazma UI | Staff `GET /products` only |
| P-010 | e-Belge | O-001 adapter/stub; GİB/UBL yok |
| P-011 | Bordro | O-008 + A-009; personel master only |
| P-012 | Kullanıcı/rol/rapor/bildirim UI | Nav `#yonetim` `#raporlar` `#bildirimler` stub |
| P-013 | `docs/operations` | Backup kodu var; runbook/incident yok |
| P-014 | Integration/E2E yeşil kanıt | Domain+Vitest var; Postgres integration bu ortamda koşturulmadı; Playwright yok |
| P-015 | Public KVKK silme/consent | O-009; retention endpoint yok |
| P-016 | TLS / CORS / HSTS | nginx HTTP 80, `location /` 404; API’de UseHttpsRedirection yok |
| P-017 | Stale copy | Root `README.md` “production code yok” diyor. Dashboard hâlâ “GET /orders yok” caption (WEB 011 sonrası yanlış) |
| P-018 | Parola politikası | PBKDF2 sağlam; login `min(1)`; lockout yok; bootstrap env parola karmaşıklığı yok |
| P-019 | `POST /mobile/quantity-previews` | Class `[Authorize]`, **permission policy yok**; `WarehouseId` ile on-hand okur |
| P-020 | Outbox + SMTP aynı istek | Outbox satırı yazılıyor; SMTP **inline** gönderiliyor; worker yok. Host boşsa Queued (dürüst) |

## 5. Yapılanların “düzgün mü?” cevabı

| Katman | Tek fabrika MVP | 500 firma SaaS |
|---|---|---|
| Domain kuralları (miktar, stok, allocation, sefer) | Evet, bilinçli | Kiracı sınırı yok |
| API yetki | Evet (permission) | IDOR kiracılar arası tanımsız (tek tenant) |
| Web dürüstlük | Evet | Liste 100 kesim sessiz |
| Mobil saha | Hayır | Hayır |
| Operasyon (backup tasarımı) | Kısmi | 500 yedek/restore yok |
| Ölçek | Ölçülmedi | Hayır |
| Çok kiracılık | Tasarım dışı | Hayır |

**Özet:** Omurga düzgün **ama dar**. Çöp yok; yarım ürün. 500 kiracı başka ürün.

## 6. Sonra saldırı sırası (O-015 sonrası)

Karar sahibi O-015’i seçmeden kod yok.

1. **O-015 kapat:** yol A / B / C.
2. Yol B/C ise: `company_id` + unique’leri `(company_id, …)` + JWT claim + RLS/query filter + belge numarası per tenant. Bu, Design Gate yeniden.
3. Yol A ise: imaj + migrator + secrets + TLS’li Compose paketi; web’i compose’a al; 500 kurulum için envanter/yama (yine control plane, daha ince).
4. Her yolda: D-014 gerçek pagination (cursor veya page+total); Take(100) yasak.
5. O-009: public + login rate limit, lockout, CAPTCHA/honeypot.
6. Outbox hosted worker.
7. Tek müşteri hunisi: teklif→sipariş, load-plan+dispatch prepare UI, fatura/ödeme UI, üretim complete.
8. Flutter + kamera + dosya POD — saha.
9. Yük testi: hedef (ör. 200 eşzamanlı / fabrika veya 5k eşzamanlı SaaS) yazılmadan “binlerce kişi” iddia edilmez.
10. `docs/operations` + restore kabul kaydı (O-010).

## 7. Bu turda yapılmayan

- Tenant şeması / `company_id` eklenmedi.
- WEB 019 (load-plan sihirbazı) başlatılmadı.
- Karar uydurulmadı; O-015 açık bırakıldı.
