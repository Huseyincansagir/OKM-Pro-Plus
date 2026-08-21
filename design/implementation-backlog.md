# Factory ERP — Yapılacaklar

**Kaynak:** `commercial-scale-readiness.md`  
**Durum:** OPEN  
**Kural:** Sahte ekran/KPI yok. GET-by-id uydurma yok. Tenant şeması **O-015 kapanmadan** yazılmaz.

WEB 018 PASS. Bu liste sonraki saldırı kuyruğudur; `implementation-web-mobile-slice.md` buraya bakar.

## 0. Kapı

- [ ] **O-015** kapat: A (N on-prem kopya) / B (paylaşımlı `company_id`) / C (şema veya DB per tenant). Hedef eşzamanlı kullanıcı yaz.

## 1. Tek fabrika — huniyi kapat (O-015 beklemez)

Sıra: belge zinciri önce, saha sonra.

- [x] **P-003** Teklif issue → sipariş (UI + gerçek dönüşüm; IssueQuote sessiz sipariş açmaz)
- [x] **P-001** Load-plan create/lock sihirbazı (API var; web GET-only)
- [x] **P-002** Sefer hazırlama UI (`POST /route-plans/{id}/dispatch`)
- [x] **P-006** Fatura oluştur/kes UI
- [ ] **P-007** Ödeme: GET list + UI (`POST /payments` only)
- [ ] **P-008** Üretim complete UI (`/uretim` list-only)
- [ ] **P-009** Ürün/ambalaj yazma UI (staff GET only)
- [ ] **P-005** Dosya POD (imza/foto); metin recipient kalsın
- [ ] **P-004** Flutter + kamera barkod
- [ ] **P-012** Kullanıcı/rol UI; `#yonetim` `#raporlar` `#bildirimler` stub kapat veya kaldır
- [ ] **P-010** e-Belge: O-001 stub sınırı durur; gerçek GİB ayrı release
- [ ] **P-011** Bordro: O-008 dışı; başlatma

## 2. Tek fabrika — güvenlik / ölçek / ops (O-015 beklemez)

- [ ] **S-004** Staff listeler: gerçek pagination (D-014); `Take(100)` sessiz kesim yasak
- [ ] **S-005** Public quote: rate limit, sunucu honeypot/CAPTCHA (O-009); UI honeypot yetmez
- [ ] **S-006** Login lockout / brute-force
- [ ] **P-018** Parola politikası (login `min(1)` yetmez)
- [ ] **S-013** Kullanıcı pasif → access JWT ölür (jti/blacklist veya kısa TTL + refresh zorunlu)
- [ ] **P-019** `POST /mobile/quantity-previews` permission policy
- [ ] **S-007 / P-020** Outbox hosted worker; SMTP aynı istekte kalmasın
- [ ] **S-017** Idempotency: public quote, `/mobile/*`, `/physical-logistics/*`
- [ ] **S-010 / P-016** Prod compose’a web; nginx TLS (O-011); `location /` 404 kalksın
- [ ] **S-016** Günlük backup cron (O-010); profile one-shot yetmez
- [ ] **P-013** `docs/operations` (DEPLOYMENT, BACKUP, RESTORE, MONITORING, INCIDENT)
- [ ] **S-012 / P-014** CI: Postgres service, web vitest, integration yeşil; Playwright yoksa E2E ayrı
- [ ] **S-008** Structured log (Serilog) + correlation
- [ ] **S-009** Npgsql pool açık ayar; yük hedefi yazılmadan “binlerce kişi” iddia yok
- [ ] **S-011** Yük/soak testi (hedef O-015 ile)
- [ ] **P-015** Public KVKK silme/saklama
- [ ] **P-017** Root README ve dashboard “GET /orders yok” caption düzelt

## 3. 500 firma — O-015 kapandıktan sonra

Yol B/C seçilirse Design Gate yeniden. Yol A ise uygulama izolasyonu DB kopyasıdır; yine control plane gerekir.

- [ ] **S-001** Kiracı modeli (seçilen yol)
- [ ] **S-002** Unique’ler `(company_id, …)`; belge numarası per tenant
- [ ] **S-014** GET-by-id kiracı/sahiplik; `ICurrentUserAccessor` kaydı
- [ ] **S-015** `/me` sabit `"default"` şirket kalksın
- [ ] **S-003** Control plane (onboarding, kota, yama) — 500 kurulum elde tutulamaz
- [ ] **S-017** Idempotency key kiracı kapsamı

Kanıt ayrıntısı: `commercial-scale-readiness.md`.
