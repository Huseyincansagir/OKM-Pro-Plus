# Grok Oturum Notları İncelemesi

**Kaynak:** `docs/00-project-brief/grok-session-notes-2026-08-16-1.md`  
**İnceleme tarihi:** 16 Ağustos 2026  
**İnceleme amacı:** Grok oturumundaki teknik önerileri, mevcut canonical `/design` kararları ve Design Gate ile karşılaştırmak.

## 1. Genel sonuç

Grok notları mevcut DISCOVER → DESIGN yönüyle büyük ölçüde uyumludur. Özellikle modüler monolith yaklaşımı, immutable stok/cari ledger, transaction sınırları, adapter tabanlı e-belge yaklaşımı, fiyat listesi ihtiyacı, soft risk uyarısı ve marka tutarsızlığının açık karar olarak tutulması yararlı katkılardır.

Bununla birlikte Grok notlarında yer alan bazı maddeler **öneri** niteliğindedir; proje sahibi adına karar verilmiş kabul edilmemiştir. Bu nedenle O-001–O-013 kararları otomatik olarak `DECIDED` yapılmamış, Design Gate `BLOCKED` bırakılmıştır.

## 2. Korunan ve canonical tasarımla uyumlu öneriler

| Konu | Değerlendirme | Uygulama |
|---|---|---|
| Modüler monolith ve Clean Architecture | Mevcut D-002 ile uyumlu | Canonical teknik mimaride korunuyor. |
| `StockMovement` ve `CurrentTransaction` immutable ledger | D-007, D-009, D-012 ile uyumlu | Fiziksel silme yerine ters kayıt/iptal kuralı korunuyor. |
| Sipariş onayı + rezervasyon; irsaliye + stok çıkışı; üretim + stok girişi; ödeme + cari + allocation | Mevcut transaction taslağıyla uyumlu | `database-technical-architecture.md` içindeki transaction sınırları korunuyor. |
| `AvailableQuantity = Quantity - ReservedQuantity` | Mevcut stok invarianti ile uyumlu | Kullanılabilir miktarın bağımsız, sessizce yazılan bir ana kayıt olmaması korunuyor. |
| Fiyat listesi ve müşteri grubu | O-012’nin doğru teknik açılımı | `ProductPrice`, `PriceList`, `CustomerPriceGroup` aday modeli olarak korunuyor; karar hâlâ açık. |
| e-Belge adapter sınırı | O-001 için düşük bağımlılık önerisi | Sağlayıcı seçimi yapılmadan `IInvoiceIntegrationService` benzeri adapter sınırı mimari not olarak korunuyor. |
| Risk soft block | O-007 için makul aday | Soft block yalnızca aday karar olarak tutuluyor; yetkili onayı gerekip gerekmediği proje sahibi tarafından seçilecek. |
| Public katalog rate limit/KVKK | O-009 ile uyumlu risk azaltma | Public endpoint tasarımında rate limit, doğrulama ve KVKK metinleri karar girdisi olarak korunuyor. |
| Linux + Docker + reverse proxy | O-011 ile uyumlu teknik aday | Nginx/Traefik seçimi ve HTTPS/LAN modeli hâlâ açık. |
| UUID, document sequence, concurrency | Teknik olarak yararlı adaylar | UUID v7, `DocumentSequence` ve row-version/xmin mimari aday olarak bırakıldı; final seçim Architecture aşamasında yapılacak. |
| Günlük backup, 14–30 gün saklama, aylık restore testi | O-010 ile uyumlu operasyon adayı | RPO/RTO ve retention sahibi tarafından kesinleştirilecek. |

## 3. Karar olarak alınmayan öneriler

Aşağıdaki Grok önerileri teknik açıdan makul olsa da proje sahibi onayı olmadan canonical karar haline getirilmedi:

| Açık karar | Grok önerisi | Mevcut canonical durum |
|---|---|---|
| O-002 Kısmi sevkiyat | İzin ver | Açık karar; sipariş, rezervasyon, irsaliye ve kalan miktar kuralları netleştirilmeli. |
| O-003 Kısmi fatura | İzin ver | Açık karar; bir veya birden fazla irsaliyeye fatura ilişkisinin sınırı belirlenmeli. |
| O-004 BOM/reçete | MVP’de kapalı | Açık karar; mevcut A-007 sınırlı BOM varsayımıyla çelişebileceği için karar sahibi seçmeli. |
| O-005 Lot/seri | MVP’de kapalı | Açık karar; kalite, iade ve mevzuat etkisi değerlendirilmeden kapatılamaz. |
| O-006 Müşteri onayı | Basit manuel onay | Açık karar; public teklif talebinin müşteriye dönüşme zamanı belirlenmeli. |
| O-007 Risk | Soft block | Aday yaklaşım; uyarı mı, yetkili onayı mı, hard block mu netleşmeli. |
| O-008 Bordro | Kayıt + export | A-009 ile uyumlu güçlü varsayım; yine de İK/muhasebe kapsamı onaylanmalı. |
| O-009 Public katalog | Açık + rate limit + KVKK | Aday teknik yaklaşım; erişim, doğrulama ve hukuki metinler açık. |
| O-010 Backup | Günlük + 14 gün | Aday operasyon politikası; RPO/RTO ve restore sahibi kararı bekleniyor. |
| O-011 Server | Linux + Docker + Traefik | Aday; şirket server işletim sistemi, HTTPS ve reverse proxy açık. |
| O-012 Fiyatlandırma | Fiyat listesi + müşteri grubu | Güçlü teknik öneri; ticari fiyat politikasının sahibi karar vermeli. |
| O-013 Marka | Tek marka hemen sabitlensin | Gerekli karar; proje sahibi/pazarlama tarafından logo, isim, token ve asset politikası seçilmeli. |

## 4. Çelişki veya dikkat gerektiren noktalar

İlk dikkat noktası, Grok notlarının O-004 ve O-005 için “MVP’de kapalı” demesidir. Mevcut canonical tasarım bu konuları hâlâ `OPEN DECISION` olarak tutar; çünkü BOM/reçete ve lot/seri kararı üretim, kalite, iade ve stok modelini değiştirir. Bu yüzden öneri karar olarak kaydedilmiş, karar sınıfı değiştirilmemiştir.

İkinci dikkat noktası, Grok notlarının bazı teknik kararları kesinleşmiş gibi yazmasıdır. UUID v7, Traefik, 14 günlük retention ve soft risk block iyi adaylardır; fakat bunlar proje sahibinin operasyon, güvenlik ve maliyet kararlarının yerine geçmez. Bunlar Architecture veya Operations aşamasında kesinleştirilecektir.

Üçüncü dikkat noktası, numaralı docs klasörlerinin arşiv olarak korunmasıdır. Canonical source of truth `/design`, coding-agent skill paketi ise kök `/.claude/skills` olarak korunmuştur. `docs/00`–`docs/06` ve `docs/06-process-skill/.claude/skills` senkronize teslim/arşiv kopyalarıdır.

## 5. Önerilen sonraki sıra

Önce proje sahibi tarafından O-001–O-013 maddeleri için karar sahibi, seçilen değer ve hedef tarih belirlenmelidir. Ardından `/design/decision-log.md`, `/design/domain-model.md`, `/design/business-workflows.md`, `/design/database-technical-architecture.md` ve `/design/implementation-readiness.md` birlikte güncellenmelidir.

Açık kararlar kapandıktan sonra `implementation-ready.md` `READY` durumuna çekilebilir ve `factory-erp-architecture` skill'i çalıştırılabilir. Architecture aşaması tamamlanmadan `factory-erp-implementation` ile business feature kodlanmamalıdır.

## 6. İnceleme sonucu

```text
GROK NOTES STATUS:
USEFUL CONTRIBUTIONS MERGED
OPEN DECISIONS PRESERVED
DESIGN STATUS:
BLOCKED
NEXT SKILL:
factory-erp-architecture
```

## 7. Son Grok commit incelemesi

16 Ağustos 2026 tarihli `3db498b` ve `e290c86` commit'lerinde O-001–O-013 maddelerinin tamamı `DECIDED` yapılarak Design Gate `READY FOR ARCHITECTURE` seviyesine taşınmıştır. Bu işlem, önerilerin proje sahibi kararına dönüştüğünü gösteren karar sahibi, tarih, gerekçe ve artefact yayılım kanıtları bulunmadığı için kabul edilmemiştir.

Teknik öneriler değerli olsa da şu tutarsızlıklar görülmüştür:

- Domain modelinde `PriceList` ve `CustomerPriceGroup` henüz tam source-of-truth olarak işlenmemiştir.
- Workflow belgesinde `PartiallyShipped` bulunmasına rağmen partial invoicing state ve allocation kuralları eksiktir.
- Screen inventory'de fiyat listesi/müşteri fiyat grubu yönetimi ve public rate-limit/KVKK davranışı ayrı ekran/uygulama durumu olarak tamamlanmamıştır.
- `database-technical-architecture.md` kararları koşullu taslak olarak içerir; tüm D-016–D-028 sonuçlarını uygulamaya hazır schema kararı olarak taşımamıştır.
- Marka ve görsel değerlerin somut seçimi yapılmadan O-013'ün kapatılması mümkün değildir.

Bu nedenle son commit'in yararlı teknik etkileri solution matrix'e ve karar bağımlı tasarım bölümlerine aktarılmış, ancak canonical karar sınıfları ve Design Gate önceki güvenli durumuna döndürülmüştür.
