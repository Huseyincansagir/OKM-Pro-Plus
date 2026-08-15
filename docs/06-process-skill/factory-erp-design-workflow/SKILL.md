---
name: factory-erp-design-workflow
description: Kodlamaya başlamadan önce üretim yapan fabrikalar için web, public katalog ve mobil operasyon ERP arayüzlerini; iş akışlarını, veri ve teknik mimari ön taslağını, sunum içeriğini ve proje yönetimi çıktısını tasarlamak için kullan. Kullanıcı ERP-lite, üretim-depo-satış-cari-personel sistemi, barkodlu depo, public teklif kataloğu, kodlama öncesi UX, teknik taslak veya proje yönetimi sunumu istediğinde tetikle.
---

# Factory ERP Design Workflow

## Amaç

Üretim, depo, satış, sevkiyat, finans/cari ve personel süreçlerini tek merkezi sistemde tasarlarken kodlama öncesi kapsamı, kullanıcı deneyimini, veri modelini ve proje yönetimi kararlarını birlikte üret.

## Temel çalışma ilkeleri

- Kullanıcı arayüzünü Türkçe, teknik entity/property ve API isimlerini İngilizce tasarla.
- Tasarımı dekoratif ekran çizimi olarak bırakma; her ekran için amaç, kullanıcı rolü, alanlar, durumlar, yetki, bağlı belgeler ve sonraki işlemi tanımla.
- Satış, üretim, depo, sevkiyat, fatura, cari ve ödeme modüllerini kopuk özellikler olarak değil, ortak belge ve veri akışı olarak ele al.
- Finansal ve stok hareketlerini immutable/izlenebilir ledger yaklaşımıyla tasarla; silme yerine iptal veya ters kayıt davranışını belirt.
- Kullanıcıdan gereksiz onaylar istemeden makul varsayımlar yap; belirsiz ve proje riskini etkileyen kararları ayrı bir “beklenen kararlar” listesinde topla.
- Public müşteri deneyimini iç ERP’den ayır; public kullanıcıya stok, maliyet ve şirket içi risk bilgisi gösterme.
- Mobil uygulamayı masaüstünün küçültülmüş kopyası yapma; barkod, stok, sayım, transfer, sevkiyat ve üretim işlerini görev odaklı tasarla.

## Uygulanacak sıra

1. **Kapsamı çıkar:** Ürün, müşteri, public katalog, teklif, sipariş, onay, stok, üretim, irsaliye, sevkiyat, fatura, cari, ödeme, personel, rapor, bildirim, yönetim ve mobil modüllerini listele.
2. **Rolleri belirle:** Sistem yöneticisi, yönetici, satış, depo, üretim, muhasebe, İK, görüntüleyici ve public müşteri için görev ve izin sınırlarını yaz.
3. **Uçtan uca akışları kur:** En az `Teklif Talebi → Teklif → Sipariş → Onay → Rezervasyon → İrsaliye → Sevkiyat → Fatura → Cari → Ödeme` ve `İş Emri → Üretim → Fire/Duruş → Stok Girişi` akışlarını göster.
4. **Ekran envanterini oluştur:** Her modül için liste, detay, oluştur/düzenle, kritik onay ve empty/loading/error/permission durumlarını tanımla.
5. **Web bilgi mimarisini tasarla:** Sidebar, topbar, dashboard, filtreli data table, detay sekmeleri, stepper, timeline, drawer ve modal davranışlarını standardize et.
6. **Mobil akışları tasarla:** Giriş, bağlantı durumu, ana sayfa, barkod tarama, ürün sonucu, stok, sayım, transfer, sevkiyat, üretim, bildirim ve profil ekranlarını belirt.
7. **Veri ve teknik ön taslak hazırla:** PostgreSQL tablolarını domain gruplarıyla ayır; foreign key, unique constraint, index, transaction, audit, backup, Docker ve deployment sınırlarını yaz.
8. **Görsel yönü belirle:** Açık içerik alanı, derin lacivert navigasyon, teal birincil aksiyon, amber bekleyen, kırmızı kritik, yeşil başarılı durum. Mockup gereken kritik ekranları üret.
9. **Proje yönetimi sunumu hazırla:** En fazla 12 slaytta vizyon, kapsam, süreç, modül haritası, roller, UX sistemi, kritik ekranlar, mobil/public, yol haritası ve beklenen kararları anlat.
10. **Konuşma metni üret:** Her slayt için 80–150 kelimelik, proje yönetimi ekibine hitap eden; ana mesaj, iş etkisi ve sonraki adımı açıklayan Türkçe not yaz.
11. **Kalite kontrolü yap:** Tasarım envanterindeki her modülün dokümanda ve sunumda temsil edildiğini; stok/cari/izin yetkilerinin görünür olduğunu; kritik işlem ve ağ kesintisi durumlarının açıklandığını kontrol et.

## Çıktı paketi

Varsayılan olarak aşağıdaki dosyaları üret:

| Dosya | İçerik |
|---|---|
| `master-screen-inventory.md` | Tüm modül, route, ekran, rol ve durum envanteri |
| `web-ux-architecture.md` | Web bilgi mimarisi ve ekran akışları |
| `production-warehouse-deep-dive.md` | Üretim ve depo ekranlarının ayrıntılı incelemesi |
| `database-technical-architecture.md` | Veritabanı, transaction, API ve deployment ön taslağı |
| `mobile-design.md` | Mobil ekran ve barkod akışları |
| `public-catalog-design.md` | Public katalog ve teklif sepeti |
| `visual-design-system.md` | Renk, tipografi, bileşen ve durum sistemi |
| `project-management-slides.md` | En fazla 12 slaytlık sunum içeriği |
| `slide_notes.md` | Slayt bazlı konuşma metni |

## Tasarım ekranı şablonu

Her ekranı aşağıdaki başlıklarla dokümante et:

```text
Ekran adı
Amaç
Kullanıcı rolleri
Route
Üst özet / KPI
Ana alanlar ve tablo kolonları
Birincil / ikincil aksiyonlar
Durumlar
Yetki davranışı
Bağlı belgeler ve veri etkisi
Boş / yükleniyor / hata / ağ yok durumu
Mobil uyarlama
Kabul senaryosu
```

## Teknik tasarım kontrol listesi

- `users`, `roles`, `permissions`, `audit_logs` ile RBAC ve denetim izini kur.
- `products`, `customers`, `warehouses`, `stocks`, `stock_movements`, `stock_reservations` ana verisini ortaklaştır.
- `quote_requests → quotes → sales_orders → delivery_notes → shipments → invoices → current_transactions` bağlantısını görünür tasarla.
- `production_orders`, `production_records`, `production_personnel`, `machines`, `machine_downtimes` ile üretim gerçekleşmesini modelle.
- Sipariş onayı, irsaliye kesinleştirme, ödeme ve üretim tamamlama için transaction sınırlarını açıkça yaz.
- Belge numarası, ürün kodu, barkod ve stok anahtarları için unique/index yaklaşımını belirt.
- Tarihleri database’de UTC, kullanıcı arayüzünde Türkiye yerel zamanı olarak ele al.
- Finansal ve stok kayıtlarında fiziksel silme kullanma; iptal/ters hareket tasarla.
- Mobil ağ kesintisinde stok veya finans hareketini sessizce offline göndermeme kuralını koru.

## Sunum kalite standardı

Sunumu proje yönetimi ekibinin karar alacağı şekilde kurgula. Teknik ayrıntıyı tamamen silme; ancak her teknik kararı proje etkisiyle bağla. Her slaytta tek ana mesaj bulunsun. Ekran mockup’larını yalnızca süs olarak kullanma; hangi iş akışını ve hangi riski çözdüğünü açıklayan kısa metinle eşleştir.

## Son teslim kontrolü

Teslimden önce şu sorulara cevap ver:

1. Satıştan tahsilata ana akış kesintisiz mi?
2. Üretim tamamlanınca stoğa girişin ne zaman oluştuğu açık mı?
3. Depo kullanıcısı hangi finansal işlemleri yapamıyor?
4. Sipariş onayını kim, hangi bilgileri görerek veriyor?
5. Public müşteri hangi bilgileri bırakıyor ve iç sisteme ne oluşuyor?
6. Mobilde barkod, sayım, sevkiyat ve üretim akışları tanımlı mı?
7. Veritabanı transaction ve audit sınırları yazılı mı?
8. Proje yönetimi ekibinden beklenen kararlar listelenmiş mi?
