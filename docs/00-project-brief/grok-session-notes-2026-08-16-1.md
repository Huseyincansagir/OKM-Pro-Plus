# Grok Oturum Notları — 16 Ağustos 2026

**Konu:** OKM Pro Plus (Factory ERP-Lite) proje incelemesi, mimari öneriler ve açık kararlar  
**Katılımcı:** Grok (xAI) + Proje sahibi  
**Aşama:** DISCOVER → DESIGN (Design Gate hâlâ BLOCKED)

---

## 1. Proje İnceleme Özeti

Repository tamamen tasarım + dokümantasyon + skill sistemi aşamasındadır. Production kod, migration veya çalışan uygulama yoktur.

**Hedef yığın (mevcut kararlar):**
- Backend: ASP.NET Core + EF Core + PostgreSQL
- Web: Next.js + React + TypeScript
- Mobil: Flutter
- Auth: JWT + RBAC
- Deployment: Docker Compose (şirket içi)

**Canonical kaynak:** `/design/` klasörü  
**Arşiv:** `/docs/00` – `/docs/06`

**Güçlü yanlar:**
- Source of truth matrisi net
- Belge zinciri ve domain invariant’lar tanımlanmış
- Agent skill sistemi (architecture, implementation, QA, operations) hazır
- Immutable ledger + reversal prensibi vurgulanmış

**Riskler:**
- Design Gate BLOCKED (açık kararlar)
- Aynı dokümanların `/design` ve `/docs` altında tutulması (senkronizasyon riski)
- Mockup’larda marka tutarsızlığı (O-013)

---

## 2. ERP Mimari Önerileri

### 2.1 Genel Yaklaşım
Mevcut **Modüler Monolith** kararı korunmalıdır. Clean Architecture katmanları önerilir:

```
API → Application → Domain → Infrastructure / Persistence
```

Controller’a iş kuralı yazılmamalıdır.

### 2.2 Kritik Prensipler
- `StockMovement` ve `CurrentTransaction` **immutable** olmalı
- Fiziksel DELETE yasak → sadece Reversal / Cancellation
- `AvailableQuantity = Quantity - ReservedQuantity` (saklanmamalı, hesaplanmalı)
- Belge zinciri foreign key + status machine + audit log ile korunmalı

### 2.3 Transaction Sınırları (tek transaction içinde bitmeli)
- Sipariş onayı + stok rezervasyonu
- İrsaliye kesinleştirme + stok çıkışı
- Üretim tamamlama + stok girişi
- Ödeme + cari hareket + allocation

### 2.4 Primary Key & Concurrency
- Master + transactional: UUID (v7 tercih)
- Belge numaraları: ayrı `DocumentSequence` tablosu
- Optimistic concurrency: `xmin` veya `row_version`

### 2.5 Deployment Önerisi
```yaml
services:
  reverse-proxy:   # Traefik veya Nginx
  web:             # Next.js
  api:             # ASP.NET Core
  postgres:
  backup:          # Günlük pg_dump + retention
```

Backup: Günlük full, 14–30 gün retention, aylık restore testi zorunlu.

---

## 3. Açık Kararlar (OPEN DECISION) Önerileri

| ID | Konu | Önerilen Karar | Öncelik |
|----|------|----------------|---------|
| **O-002** | Kısmi sevkiyat | **İzin ver** | Yüksek |
| **O-003** | Kısmi fatura | **İzin ver** | Yüksek |
| **O-012** | Fiyat listesi | Fiyat listesi + müşteri grubu | Yüksek |
| **O-013** | Marka / logo | Tek marka hemen sabitle | Yüksek |
| **O-004** | BOM / Reçete | MVP’de **kapalı** | Orta |
| **O-005** | Lot / Seri | MVP’de **kapalı** | Orta |
| **O-001** | e-Belge / KDV | Adapter + stub | Orta |
| **O-007** | Risk skoru | Soft block (uyarı + onay) | Orta |
| **O-006** | Müşteri onay akışı | Basit manuel onay | Orta |
| **O-009** | Public katalog | Açık + rate limit + KVKK | Orta |
| **O-010** | Backup RPO/RTO | Günlük + 14 gün | Orta |
| **O-011** | Server / HTTPS | Linux + Docker + Traefik | Orta |
| **O-008** | Bordro | Kayıt + export (yasal motor yok) | Düşük |

### Detaylı Gerekçeler

**O-002 / O-003 – Kısmi Sevkiyat & Fatura**  
Fabrika operasyonunda siparişin tamamını tek seferde sevk etmek pratik değildir. Kısmi destek yoksa sistem kullanılamaz hale gelir.  
Mimari: `OrderedQty / ShippedQty / RemainingQty`, birden fazla `DeliveryNote` ve `Invoice` bağlantısı.

**O-012 – Fiyatlandırma**  
Tek fiyat kartı B2B için yetersizdir. `ProductPrice` + `PriceList` + `CustomerPriceGroup` modeli önerilir. Sipariş oluşturulunca fiyat kilitlenmelidir.

**O-004 / O-005 – BOM & Lot**  
MVP’de kapalı tutulsun. Sadece “üretim tamamlandı → bitmiş ürün stoğa girdi” yeterli. BOM ve lot takibi ileride eklenebilir.

**O-001 – e-Belge**  
`IInvoiceIntegrationService` interface’i tanımla, ilk sürümde stub kullan. Erken entegratör bağımlılığı risklidir.

**O-007 – Risk**  
Hard block yerine soft block (uyarı + yetkili onayı) tercih edilsin.

**O-013 – Marka**  
Mockup’larda MaviKağıt / NAVIS / Napkinova karışık. Kodlamadan önce tek marka, logo, renk token’ları ve görsel lisans politikası sabitlenmelidir.

---

## 4. Önerilen Sonraki Adımlar

1. Yukarıdaki açık kararları proje sahibi ile netleştirip `decision-log.md` güncelle
2. Design Gate’i `READY` durumuna getir
3. `factory-erp-architecture` skill’ini çalıştır → PostgreSQL şema + migration planı
4. Core domain sırası:
   - Identity & Access
   - Products + Customers
   - Warehouse + Stock
   - Sales (Quote → Order)
   - Shipping + Invoicing
   - Production

---

## 5. Not

Bu dosya, 16 Ağustos 2026 tarihli Grok oturumunun özetidir. Canonical kararlar her zaman `/design/decision-log.md` ve `/design/implementation-readiness.md` dosyalarında tutulmalıdır. Bu not, karar alma sürecine yardımcı olmak için arşivlenmiştir.
