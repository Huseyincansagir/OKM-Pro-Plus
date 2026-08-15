# Fabrika ERP Arayüz Tasarım Paketi

## Tasarım aşaması sonucu

Kodlamaya başlamadan önce uygulamanın temel arayüz mimarisi, web ve mobil ekran akışları, roller, görsel tasarım dili ve öncelikli ekran prototipleri hazırlandı. Tasarım; üretim, depo, barkod, teklif, sipariş onayı, irsaliye, sevkiyat, fatura, cari, ödeme, rapor ve personel süreçlerini tek bir ortak operasyon modeli içinde ele almaktadır.

> Ana tasarım kararı: Kullanıcı her ekranda kaydın mevcut durumunu, kimden işlem beklediğini ve sıradaki adımı açıkça görmelidir.

## Teslim edilen dosyalar

| Dosya | İçerik |
|---|---|
| `uretim-depo-ui-design-brief.md` | Ürün vizyonu, kullanıcı rolleri, tasarım ilkeleri, ana iş akışları ve öncelikli ekran listesi |
| `uretim-depo-web-ux-architecture.md` | Web menüsü, dashboard'lar, detay ekranları, sipariş, depo, üretim, sevkiyat, cari ve rapor akışları |
| `uretim-depo-mobile-ux-architecture.md` | Mobil ana ekran, barkod, stok, sayım, transfer, sevkiyat ve üretim akışları |
| `uretim-depo-visual-design-system.md` | Renk sistemi, tipografi yaklaşımı, bileşen kararları ve görsel kabul kriterleri |
| `uretim-depo-erp-dashboard-reference.png` | Yönetici dashboard görsel referansı |
| `uretim-depo-order-detail-mockup.png` | Sipariş detay ve sorumlu onayı mockup'ı |
| `uretim-depo-product-catalog-mockup.png` | Ürün katalog mockup'ı |
| `uretim-depo-mobile-barcode-mockup.png` | Mobil barkod tarama mockup'ı |

## Onaylanan temel ekran yönü

Web arayüzü için açık içerik alanı, derin lacivert sol menü, teal birincil aksiyon rengi, amber bekleyen durumları ve kırmızı kritik durumları kullanan kurumsal ERP yaklaşımı seçildi. Bu yapı, yoğun tablo ve rapor bilgilerini korurken arayüzün gereksiz derecede dekoratif olmasını önlemektedir.

Mobil uygulama ise masaüstü menüsünün küçültülmüş kopyası olmayacak; barkod tarama, sevkiyat, üretim kaydı ve görev tamamlama üzerine kurulacaktır. Mobil ana ekranın en önemli işlemi “Barkod Tara” olacak, ağ bağlantısı durumu sürekli görünür tutulacaktır.

## Kodlamaya geçmeden önce netleştirilmesi gereken altı karar

| Konu | Varsayılan tasarım kararı |
|---|---|
| Marka | Şirket logosu ve kurumsal renkler gelene kadar lacivert–teal görsel sistem kullanılacak |
| Sipariş onayı | İlk sürümde tek sorumlu onayı; ileride tutara göre kademeli onaya açık mimari |
| Public katalog | Kullanıcı hesabı olmadan kontrollü teklif talebi oluşturma |
| Mobil kapsam | İlk sürümde depo ve üretim operasyonları öncelikli |
| Fatura | İç sistemde belge ve cari altyapısı; e-belge entegrasyonu ayrıca bağlanabilir |
| Depo | Birden fazla depo, gerektiğinde depo içi konum desteği |

## Sonraki geliştirme sırası

Tasarım onaylandıktan sonra uygulama aşağıdaki sırayla kodlanmalıdır:

1. Ortak tasarım sistemi ve route iskeleti.
2. Kimlik doğrulama, kullanıcı, rol ve izinler.
3. Ürün kataloğu, müşteri ve public teklif talebi.
4. Depo, stok hareketleri ve barkod.
5. Teklif, sipariş ve sorumlu onayı.
6. İrsaliye, sevkiyat ve stok çıkışı.
7. Fatura, cari hesap ve ödeme.
8. Üretim iş emri, makine, üretim gerçekleşmesi ve üretim personeli.
9. Personel, puantaj, izin ve mesai.
10. Dashboard, raporlar, bildirimler ve audit log.
11. Mobil operasyon uygulaması.
12. PDF, Excel, yedekleme, yerel server kurulumu ve uçtan uca test.

## Tasarım kabul senaryosu

Tasarım paketi aşağıdaki uçtan uca akışı görsel ve işlevsel açıdan açıklamaktadır:

```text
Ürün kataloğu
→ Teklif sepeti
→ Teklif talebi
→ Teklif hazırlama
→ Sipariş oluşturma
→ Sorumlu onayı
→ Stok rezervasyonu
→ İrsaliye
→ Sevkiyat
→ Fatura
→ Cari borç
→ Ödeme
→ Güncel bakiye
```

Ayrıca üretim ve mobil operasyon akışları da doğrulanmıştır:

```text
Üretim iş emri
→ Makine ve personel atama
→ Üretim kaydı
→ Fire ve duruş
→ Üretim tamamlama
→ Depo stok girişi
```

```text
Mobil giriş
→ Barkod tara
→ Ürün bul
→ Stok görüntüle
→ Sevkiyat veya sayım işlemi
→ Yetki kontrollü kayıt
```

Bu aşamada kod yazılmadı; çıktı tamamen kodlama öncesi arayüz ve kullanıcı deneyimi tasarım paketidir.
