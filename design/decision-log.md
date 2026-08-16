# Factory ERP — Design Decision Log

**Aşama:** DISCOVER → DESIGN  
**Kapsam:** Kodlama öncesi tutarlılık, source of truth ve açık karar yönetimi

## 1. Karar sınıfları

| Sınıf | Anlam |
|---|---|
| DECIDED | Mevcut tasarım ve kullanıcı gereksinimiyle yeterince netleşmiş karar |
| ASSUMED | Kodlamayı durdurmadan ilerlemek için alınan, ileride değiştirilebilir varsayım |
| OPEN DECISION | Domain, maliyet, entegrasyon veya operasyon kararını doğrudan etkilediği için proje sahibi tarafından karara bağlanması gereken konu |

## 2. DECIDED

| ID | Karar | Gerekçe |
|---|---|---|
| D-001 | Sistem şirket içi kullanılacak merkezi ERP-lite olacaktır. | Web, mobil ve public yüzeyler aynı merkezi veri modelini kullanmalıdır. |
| D-002 | İlk teknik mimari modüler monolith + REST API + PostgreSQL yaklaşımıdır. | Erken aşamada mikroservis karmaşıklığına ihtiyaç yoktur. |
| D-003 | UI Türkçe, entity/property/API isimleri İngilizce olacaktır. | Kullanıcı operasyonu Türkçe; kod sözleşmeleri tutarlı ve taşınabilir olmalıdır. |
| D-004 | Public katalog iç ERP’den ayrı bir deneyimdir. | Dış müşteriye iç maliyet, risk, stok ve operasyon bilgisi gösterilmemelidir. |
| D-005 | `Product` ürün ana kaynağıdır. | Ürün adı, kodu, birimi ve katalog özellikleri tek yerde tutulur. |
| D-006 | `ProductBarcode` barkodların ana kaynağıdır. | Bir ürünün birden fazla barkodu olabilir; barkod ürün kartına bağlıdır. |
| D-007 | `Stock` mevcut özet stok; `StockMovement` değişmez stok geçmişidir. | Stok miktarı sessiz UI güncellemesiyle değişmemelidir. |
| D-008 | `Customer` müşteri ana kaynağıdır. | Müşteri farklı modüllerde bağımsız kopyalanmayacaktır. |
| D-009 | `CurrentTransaction` cari hareketlerin; `Payment` ödeme işleminin kaynağıdır. | Bakiye transaction'ların sonucu olmalıdır. |
| D-010 | `SalesOrder → DeliveryNote → Shipment → Invoice` belge zinciri korunacaktır. | Her operasyon bir önceki belge ve bir sonraki işlemle izlenebilir olmalıdır. |
| D-011 | Kritik stok, finans ve yetki hareketleri audit log üretmelidir. | Kim, ne zaman, hangi kaydı, hangi eski/yeni değerle değiştirdiği bilinmelidir. |
| D-012 | İptal veya ters kayıt, kritik kayıtları fiziksel silmeye tercih eder. | Finansal ve fiziksel geçmiş kaybolmamalıdır. |
| D-013 | Mobil öncelikleri barkod, stok, sayım, transfer, sevkiyat ve üretim kaydıdır. | Mobil saha kullanıcısının görevleri masaüstü rapor ekranlarından farklıdır. |
| D-014 | Büyük listelerde server-side pagination, arama, filtreleme ve sıralama vardır. | ERP operasyonlarında veri hacmi arttığında tarayıcıya tüm tablo çekilmemelidir. |
| D-015 | Tasarım tamamlanmadan implementasyon başlatılmayacaktır. | Bootstrap promptu DISCOVER → DESIGN aşamasını açıkça sınırlar. |

## 3. ASSUMED

| ID | Varsayım | Etkisi / revizyon koşulu |
|---|---|---|
| A-001 | İlk sürüm tek şirketlidir; multi-company tenant modeli tasarlanmaz. | İleride `company_id` eklenebilecek sınır korunur. |
| A-002 | Birden fazla depo ilk sürümden desteklenir. | Depo, konum, transfer ve stok sorgusu buna göre modellenir. |
| A-003 | Üretim tamamlanması, tanımlı bitmiş ürün miktarı için stok girişi üretir. | Ara üretim veya kalite karantinası kararı netleşirse akış genişletilir. |
| A-004 | Public katalog fiyat ve stok miktarı göstermeden teklif talebi toplar. | B2B fiyat listesi politikası kesinleşirse public deneyim güncellenir. |
| A-005 | Sipariş onayı en az bir sorumlu kullanıcının kararıdır. | Tutar/departman bazlı kademeli onay gelirse approval policy gerekir. |
| A-006 | İlk sürüm lot/seri takibi gerektirmez; ürün miktarı stok seviyesinde izlenir. | Gıda, kalite veya mevzuat gereği lot gerekiyorsa açık karar olarak işlenmelidir. |
| A-007 | İlk sürüm BOM/reçete kapsamı sınırlıdır; üretim gerçekleşmesi doğrudan bitmiş ürün stoğuna bağlanır. | Hammadde tüketimi ve reçete ihtiyaçları netleşirse ProductionMaterial ve BOM derinleştirilir. |
| A-008 | Mobil ağ kesintisinde stok ve finans işlemleri sessizce commit edilmez. | Offline güvenli kuyruk ancak idempotency ve conflict tasarımı sonrası ele alınır. |
| A-009 | Maaş modülü kayıt ve rapor kapsamındadır; tam yasal bordro motoru değildir. | Harici bordro entegrasyonu gerekirse adapter sözleşmesi eklenir. |
| A-010 | Belge numaraları yıllık prefix ve transaction-safe sequence ile üretilir. | Şirket belge politikası değişirse numaralandırma ayarı güncellenir. |

## 4. OPEN DECISION

| ID | Açık karar | Neden implementasyonu etkiler | Önerilen karar noktası |
|---|---|---|---|
| O-001 | Vergi/VAT ve e-belge entegrasyonu | Fatura kalemleri, vergi hesapları ve dış sistem entegrasyonu değişir. | Muhasebe ve mali müşavir |
| O-002 | Kısmi sevkiyat politikası | Sipariş, rezervasyon, irsaliye ve durum geçişleri etkilenir. | Satış + depo yöneticisi |
| O-003 | Kısmi fatura politikası | Bir irsaliyeden çoklu veya parçalı fatura gerekip gerekmediği netleşmelidir. | Muhasebe |
| O-004 | Üretim BOM/reçete ve hammadde tüketimi | Üretimden yalnızca bitmiş ürün girişi mi, yoksa malzeme çıkışı da mı olacağı belirlenir. | Üretim sorumlusu |
| O-005 | Lot/seri/parti izleme | Kalite, iade ve geri çağırma akışları etkilenir. | Kalite + üretim |
| O-006 | Customer approval workflow | Public taleplerin kim tarafından onaylanacağı ve müşteri kartının ne zaman açılacağı netleşmelidir. | Satış yöneticisi |
| O-007 | Risk algoritması ağırlıkları ve blokaj eşiği | Risk yalnızca gösterilecek mi, sipariş onayını durduracak mı belirlenmelidir. | Yönetim + muhasebe |
| O-008 | Maaş/bordro entegrasyon kapsamı | Maaş verisinin hassasiyeti, export ve erişim modeli değişir. | İK + muhasebe |
| O-009 | Kamuya açık katalog erişimi | Rate limit, bot koruması, iletişim doğrulama ve KVKK metinleri etkilenir. | Yönetim + hukuk |
| O-010 | Backup saklama ve felaket kurtarma hedefleri | RPO/RTO, disk ve harici yedekleme maliyetini belirler. | Sistem yöneticisi |
| O-011 | Şirket içi server işletim sistemi ve LAN/HTTPS modeli | Docker, reverse proxy, sertifika ve mobil erişim kurulumu etkilenir. | Sistem yöneticisi |
| O-012 | Fiyat listesi ve müşteri bazlı fiyatlandırma | Quote ve order fiyatının ürün kartından mı, fiyat listesinden mi geleceği netleşmelidir. | Satış + yönetim |
| O-013 | Final marka adı, logo ve ürün görseli lisansı | Manus mockup'larında MaviKağıt, NAVIS ve Napkinova adları birlikte kullanılmıştır; uygulama token'ları, header ve public katalog tek marka altında sabitlenmelidir. | Proje sahibi + pazarlama |
| O-014 | Kargo planlama otomasyon seviyesi ve araç eşleştirme politikası | Sezgisel otomatik öneri yalnızca uygunluk ön kontrolü mü yapacak, yoksa araç/palet/rota atamasını otomatik kilitleyecek mi; manuel override ve optimalite beklentisi netleşmelidir. | Depo + sevkiyat yöneticisi |

## 5. Karar yönetimi kuralları

`OPEN DECISION` maddeleri çözülmeden riskli domain implementasyonuna geçilmez. Öneri seçenekleri ve MVP çözüm paketi `/design/open-decisions-solution-matrix.md` içinde tutulur; yalnızca karar sahibi onayı, karar tarihi ve etkilenen artefact listesi ile desteklenen kararlar `DECIDED` sınıfına taşınabilir. Bir karar çözüldüğünde bu dosyadaki sınıf, gerekçe, etkilenen tasarım dosyaları ve gerekiyorsa workflow state'leri güncellenir. Design Gate sırasında tüm açık kararlar `BLOCKED` veya `ASSUMED WITH RISK` olarak değerlendirilir.
