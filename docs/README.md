# ERP-Lite Tasarım ve Mimari Dokümantasyon Paketi

Bu klasör, fabrika üretim–depo–satış–sevkiyat–cari–personel yönetim sistemi için hazırlanan kodlama öncesi tasarım, teknik mimari, sunum ve yeniden kullanılabilir beceri çıktılarının tamamını içerir.

## Klasörler

| Klasör | İçerik |
|---|---|
| [`00-project-brief`](./00-project-brief/) | Kullanıcının verdiği özgün kapsam ve teknik gereksinim promptu |
| [`01-design`](./01-design/) | Web, public katalog, mobil, görsel sistem ve tüm ekran envanteri |
| [`02-architecture`](./02-architecture/) | Veritabanı mimarisi ve teknik altyapı ön taslağı |
| [`03-production-warehouse`](./03-production-warehouse/) | Üretim ve depo modüllerinin ayrıntılı ekran/veri incelemesi |
| [`04-presentation`](./04-presentation/) | Proje yönetimi sunumu, konuşma metni ve HTML slayt kaynakları |
| [`05-assets/mockups`](./05-assets/mockups/) | Arayüz ekran mockup görselleri |
| [`06-process-skill`](./06-process-skill/) | Süreci yeniden kullanmak için oluşturulan `factory-erp-design-workflow` becerisi |

## Önerilen okuma sırası

1. [`original-project-prompt.md`](./00-project-brief/original-project-prompt.md) ile özgün gereksinim kaynağını inceleyin.
2. [`00-complete-ui-design-package.md`](./01-design/00-complete-ui-design-package.md) ile genel kapsamı inceleyin.
3. [`01-master-screen-inventory.md`](./01-design/01-master-screen-inventory.md) üzerinden ekran ve modül envanterini kontrol edin.
4. [`database-technical-architecture.md`](./02-architecture/database-technical-architecture.md) ile veri ve deployment ön taslağını değerlendirin.
5. [`production-warehouse-deep-dive.md`](./03-production-warehouse/production-warehouse-deep-dive.md) ile üretim/depo kritik akışlarını inceleyin.
6. [`project-management-slides.md`](./04-presentation/project-management-slides.md) ve [`slide_notes.md`](./04-presentation/slide_notes.md) ile proje yönetimi sunumunu kullanın.
7. [`factory-erp-design-workflow/SKILL.md`](./06-process-skill/factory-erp-design-workflow/SKILL.md) ile süreci sonraki benzer projelerde tekrar uygulayın.

## Tasarım kapsamı

Dokümantasyon; dashboard, satış, ürünler, public katalog, teklif, sipariş ve onay, depo, stok, barkod, üretim, makine, irsaliye, sevkiyat, fatura, cari hesap, ödeme, risk analizi, personel, puantaj, izin, mesai, maaş, raporlar, bildirimler, kullanıcılar, roller, yetkiler, audit, ayarlar, backup, mobil operasyon ve sistem sağlık ekranlarını kapsar.

## Ana uçtan uca süreçler

```text
Public Katalog
→ Teklif Talebi
→ Teklif
→ Sipariş
→ Sorumlu Onayı
→ Stok Rezervasyonu
→ İrsaliye
→ Sevkiyat
→ Fatura
→ Cari
→ Ödeme
```

```text
Üretim İş Emri
→ Makine ve Personel Atama
→ Üretim Gerçekleşmesi
→ Fire / Duruş
→ Üretim Tamamlama
→ Depo Stok Girişi
```

Bu dosyalar kodlama öncesi analiz ve tasarım çıktılarıdır. Uygulama implementasyonuna geçmeden önce veritabanı, API sözleşmeleri, permission modeli ve kritik işlem transaction sınırları proje ekibi tarafından gözden geçirilmelidir.
