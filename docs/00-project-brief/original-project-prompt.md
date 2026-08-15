# ROLE

Sen kıdemli bir **software architect, full-stack developer, mobile developer, database architect, DevOps engineer, UI/UX designer, QA engineer ve product engineer** olarak hareket edeceksin.

Aşağıda tanımlanan sistemi baştan sona **çalışan, üretime alınabilir, modüler, güvenli, ölçeklenebilir ve sürdürülebilir bir ürün** olarak geliştir.

Bu proje bir fabrika için şirket içi kullanılacak **ERP-lite / üretim + satış + depo + cari + sevkiyat + personel yönetim sistemi** olacaktır.

Sadece örnek kod, mockup, pseudo-code veya yarım implementasyon üretme. Gerçek çalışan uygulamayı oluştur.

Temel hedef:

**Web + mobil + şirket içi server üzerinde çalışan tek bir merkezi sistem.**

Sistemin tüm modülleri aynı merkezi backend ve veritabanını kullanacak.

---

# 1. TEMEL İŞ KAPSAMI

Şirket peçete ve benzeri ürünler üretiyor.

Yaklaşık 100 farklı ürün bulunuyor.

Sistem aşağıdaki süreçleri uçtan uca yönetmeli:

```text
Ürün Kataloğu
      ↓
Teklif Talebi
      ↓
Teklif
      ↓
Sipariş
      ↓
Sipariş Onayı
      ↓
Kesin Sipariş
      ↓
Stok Kontrolü
      ↓
İrsaliye
      ↓
Sevkiyat
      ↓
Fatura
      ↓
Cari Hesap
      ↓
Ödeme
      ↓
Raporlama
```

Üretim tarafı:

```text
Üretim Planı
      ↓
Üretim İş Emri
      ↓
Makine
      ↓
Personel
      ↓
Üretim Gerçekleşmesi
      ↓
Kalite / Fire
      ↓
Depo Stok Girişi
```

Personel tarafı:

```text
Personel
 ↓
Mesai / Puantaj
 ↓
İzin
 ↓
Çalışma Süresi
 ↓
Üretim İlişkisi
 ↓
Maaş / Personel Raporları
```

---

# 2. TEKNOLOJİ YIĞINI

Aşağıdaki teknoloji mimarisini kullan:

## Backend

* ASP.NET Core Web API
* C#
* Entity Framework Core
* PostgreSQL
* FluentValidation
* JWT Authentication
* Role + Permission based authorization
* OpenAPI / Swagger
* Serilog
* Background Services / hosted jobs
* REST API
* Clean Architecture

Backend katmanları:

```text
API
Application
Domain
Infrastructure
Persistence
```

Domain mantığı controller'lara gömülmeyecek.

---

# 3. WEB UYGULAMASI

Web:

* React
* TypeScript
* Next.js
* Modern responsive UI
* Component based architecture
* TanStack Query
* Zustand veya eşdeğer state management
* React Hook Form
* Zod
* Data tables
* Chart library
* Responsive dashboard

Web hem masaüstü hem tablet ekranlarında kullanılabilir olmalı.

Ana navigasyon:

```text
Dashboard

Üretim
 ├─ İş Emirleri
 ├─ Üretim Kayıtları
 ├─ Makineler
 ├─ Üretim Raporları

Depo
 ├─ Stok
 ├─ Stok Hareketleri
 ├─ Depolar
 ├─ Barkod İşlemleri

Satış
 ├─ Teklif Talepleri
 ├─ Teklifler
 ├─ Siparişler
 ├─ İrsaliyeler
 ├─ Sevkiyatlar
 ├─ Faturalar

Müşteriler
 ├─ Müşteri Listesi
 ├─ Cari Hesap
 ├─ Cari Ekstre
 ├─ Ödemeler
 ├─ Risk Analizi

Ürünler
 ├─ Ürünler
 ├─ Kategoriler
 ├─ Barkodlar

Personel
 ├─ Personeller
 ├─ Puantaj
 ├─ İzinler
 ├─ Mesai
 ├─ Maaş

Raporlar

Bildirimler

Ayarlar

Kullanıcılar
Roller
Yetkiler

Audit Log
```

---

# 4. MOBİL UYGULAMA

Mobil uygulama:

* Flutter
* Android + iOS
* Aynı backend API kullanılacak.

Mobil öncelikle operasyon çalışanları için tasarlanacak.

Ana ekran:

```text
Barkod Tara
Stok
Sipariş
Sevkiyat
Üretim
Personel
Bildirimler
```

Telefon kamerasından barkod okutma zorunlu.

Barkod:

```text
Camera
 ↓
Barcode decoder
 ↓
Product lookup API
 ↓
Product
 ↓
Stock / operation
```

Desteklenecek yaygın barkod formatlarını uygun bir kütüphane ile destekle.

---

# 5. VERİTABANI

PostgreSQL kullan.

Database ilişkisel olarak tasarlanacak.

En az aşağıdaki entity'ler bulunmalı:

```text
User
Role
Permission
RolePermission
UserRole
AuditLog

Customer
CustomerAddress
CustomerContact
CustomerNote

Product
ProductCategory
ProductBarcode
ProductImage
ProductPrice

Warehouse
WarehouseLocation
Stock
StockMovement
StockReservation

QuoteRequest
QuoteRequestItem

Quote
QuoteItem

SalesOrder
SalesOrderItem
SalesOrderApproval

DeliveryNote
DeliveryNoteItem

Shipment
ShipmentItem
Vehicle
Driver

Invoice
InvoiceItem

CurrentAccount
CurrentTransaction
Payment
PaymentMethod

ProductionOrder
ProductionOrderItem
ProductionRecord
ProductionPersonnel
ProductionMaterial
Machine
MachineDowntime

Employee
EmployeeDepartment
Attendance
Overtime
LeaveRequest
LeaveType
SalaryRecord

Notification
NotificationRecipient

SystemSetting
FileStorage
```

Her tablo:

* Primary key
* CreatedAt
* UpdatedAt
* CreatedBy
* UpdatedBy

alanlarına ihtiyaç duyuyorsa bunları ekle.

Silme yapılacak entity'lerde uygun yerde soft delete kullan.

Foreign key, index ve unique constraint'leri profesyonelce tasarla.

Database migration sistemi kullanılacak.

---

# 6. ÜRÜN YÖNETİMİ

Ürün kartı:

```text
Ürün Kodu
Barkod
Ürün Adı
Kategori
Ürün Açıklaması
Ürün Görseli
Birim
Koli İçeriği
Paket İçeriği
Satış Fiyatı
Maliyet
Minimum Stok
Aktif/Pasif
```

Bir ürünün birden fazla barkodu olabilir.

Ürün fotoğrafı sisteme yüklenebilir.

Ürün kataloğu mobil ve web için görsel açıdan kaliteli olmalı.

Ürün kartında:

```text
Fotoğraf
Ürün adı
Kod
Barkod
Stok
Fiyat
Açıklama
```

göster.

---

# 7. MÜŞTERİ / CARİ

Customer entity profesyonel hazırlanmalı.

Bilgiler:

```text
Müşteri Kodu
Firma Adı
Vergi Dairesi
Vergi No
Yetkili
Telefon
E-posta
Adres
Teslimat Adresi
Fatura Adresi
Ödeme Vadesi
Notlar
Risk Durumu
Aktif/Pasif
```

Cari hesapta:

```text
Borç
Alacak
Bakiye
Vade
Gecikme
```

takip edilecek.

Cari hareketleri immutable financial ledger mantığında tasarla.

Ödeme:

```text
Tarih
Müşteri
Tutar
Ödeme Tipi
Açıklama
Belge
```

alanlarına sahip olacak.

Ödeme tipleri:

* Nakit
* Havale
* EFT
* Çek
* Senet
* Kredi Kartı
* Diğer

---

# 8. CARİ EKSTRE

Her müşteri için:

```text
Açılış Bakiyesi

Borç
Alacak
Bakiye

Tarih
İşlem
Belge No
Borç
Alacak
Bakiye
```

tablosu göster.

PDF ve Excel export oluştur.

Tarih aralığı filtrelemesi bulunmalı.

---

# 9. MÜŞTERİ RİSK ANALİZİ

Sistemin müşteri bazında basit bir risk motoru olmalı.

Risk hesaplamasında:

* Toplam borç
* Geciken borç
* En uzun gecikme
* Ortalama ödeme süresi
* Vadesi geçmiş faturalar
* Son ödeme davranışı
* Son 12 ay satış hacmi
* Ödeme düzensizliği

kullanılabilir.

Risk:

```text
LOW
MEDIUM
HIGH
CRITICAL
```

olarak sınıflandırılabilir.

Risk algoritmasını kod içinde sabit yazmak yerine configurable hale getir.

Dashboard'da:

```text
Riskli Müşteriler
Geciken Ödemeler
Vadesi Yaklaşanlar
```

göster.

---

# 10. TEKLİF TALEBİ

Şirket dışındaki müşteri herhangi bir kullanıcı hesabı olmadan veya kontrollü public form üzerinden ürünleri seçerek teklif talebi oluşturabilmeli.

Public katalog:

```text
Ürün Fotoğrafı
Ürün Adı
Ürün Kodu
Açıklama
```

ve:

```text
[TEKLİFE EKLE]
```

butonu.

Teklif sepeti:

```text
Ürün
Miktar
Not
```

Müşteri bilgileri:

```text
Firma
Yetkili
Telefon
E-posta
```

girilir.

Gönderildiğinde şirket sistemine:

```text
Yeni Teklif Talebi
```

bildirimi düşer.

---

# 11. TEKLİF YÖNETİMİ

Teklif talebi:

```text
NEW
REVIEWING
QUOTED
ACCEPTED
REJECTED
EXPIRED
```

durumlarında olabilir.

Teklif oluşturulduğunda:

* Ürün
* Miktar
* Birim fiyat
* İskonto
* Vergi
* Toplam
* Geçerlilik tarihi
* Not

hesaplanmalı.

Teklif PDF üretilebilmeli.

Teklif onaylandığında satış siparişine dönüştürülebilmeli.

---

# 12. SİPARİŞ

Sipariş:

```text
Taslak
Onay Bekliyor
Onaylandı
Reddedildi
Hazırlanıyor
Kısmi Sevk
Tamamlandı
İptal
```

durumlarını desteklemeli.

Sipariş oluşturma:

```text
Müşteri
Teslimat Adresi
Ürünler
Miktar
Birim Fiyat
İskonto
Vergi
Ödeme Şartı
Not
```

Sipariş ilk oluşturulduğunda kesin sipariş sayılmamalı.

Sorumlu onayı gerekmeli.

Onay:

```text
Pending
Approved
Rejected
```

olarak tutulmalı.

Onaylayan kullanıcı, tarih ve açıklama kaydedilmeli.

---

# 13. İRSALİYE

Kesinleşmiş siparişten irsaliye oluştur.

İrsaliye:

```text
İrsaliye No
Sipariş No
Müşteri
Adres
Ürünler
Miktarlar
Tarih
Açıklama
```

içermeli.

İrsaliye oluşturulduğunda stok çıkışı doğru işlenmeli.

Stoktan yapılan çıkış ile irsaliye ilişkili olmalı.

İrsaliyenin durumu:

```text
Draft
Prepared
Issued
Shipped
Invoiced
Cancelled
```

olmalı.

---

# 14. SEVKİYAT

Sevkiyat modülü:

```text
Sevkiyat No
İrsaliye
Müşteri
Araç
Şoför
Yükleme Tarihi
Çıkış Tarihi
Teslim Durumu
Teslim Tarihi
```

alanlarını taşımalı.

Durum:

```text
Hazırlanıyor
Sevk Edilecek
Sevk Edildi
Teslim Edildi
İptal
```

olmalı.

---

# 15. FATURA

Sistemde fatura oluşturma altyapısı olmalı.

Fatura:

```text
Fatura No
Tarih
Müşteri
İrsaliye
Sipariş
Ürünler
Ara Toplam
İskonto
Vergi
Genel Toplam
Vade Tarihi
Ödeme Durumu
```

alanlarına sahip olmalı.

Ödeme durumu:

```text
Ödenmedi
Kısmi Ödendi
Ödendi
Gecikmiş
```

olmalı.

---

# 16. FATURALAŞMAMIŞ İRSALİYELER

Özel rapor oluştur.

Örneğin:

```text
İrsaliye No
Tarih
Müşteri
Tutar
Gün Sayısı
```

göster.

Uzun süre faturalaşmamış irsaliyeleri yöneticinin dashboard'unda uyarı olarak göster.

---

# 17. DEPO

Birden fazla depo destekle.

Stok miktarı:

```text
Product
Warehouse
Location
Quantity
ReservedQuantity
AvailableQuantity
```

mantığında çalışmalı.

Stok hareketleri:

```text
Üretim Girişi
Satış Çıkışı
Transfer
Sayım
İade
Düzeltme
```

olarak tutulmalı.

Stok hareketlerinin geçmişi silinmemeli.

---

# 18. BARKOD

Web ve mobilde barkod okuma desteği yap.

Mobil:

```text
Kamera aç
 ↓
Barkod tara
 ↓
Ürün bul
 ↓
Ürün detay
 ↓
İşlem
```

Web'de USB barkod okuyucu kullanımını da destekle.

Barkod okuyucu klavye gibi input veriyorsa sistem bunu anlayabilmeli.

---

# 19. ÜRETİM

Üretim iş emri oluştur:

```text
İş Emri No
Ürün
Hedef Miktar
Planlanan Tarih
Makine
Öncelik
Açıklama
```

Durum:

```text
Planned
Released
InProgress
Paused
Completed
Cancelled
```

olmalı.

---

# 20. ÜRETİM GERÇEKLEŞMESİ

Üretim kaydı:

```text
İş Emri
Makine
Başlangıç
Bitiş
Üretilen Miktar
Fire
Duruş
Not
```

tutmalı.

Bir üretim kaydına birden fazla personel bağlanabilmeli.

Her personel için:

```text
Çalışma süresi
Rol
Vardiya
```

tutulabilmeli.

---

# 21. MAKİNE TAKİBİ

Makine kartı:

```text
Makine Kodu
Makine Adı
Departman
Model
Seri No
Aktif/Pasif
```

Üretim raporları makine bazında:

```text
Toplam Üretim
Toplam Fire
Toplam Çalışma Süresi
Duruş Süresi
Verimlilik
```

göstermeli.

---

# 22. PERSONEL

Personel kartı:

```text
Sicil No
Ad Soyad
Departman
Pozisyon
İşe Giriş
Telefon
E-posta
Maaş
Durum
```

Personel modülü:

```text
Puantaj
Mesai
İzin
Devam
Çalışma Süresi
```

içermeli.

---

# 23. İZİN

İzin talebi:

```text
Personel
İzin Tipi
Başlangıç
Bitiş
Gün
Açıklama
```

durumları:

```text
Bekliyor
Onaylandı
Reddedildi
```

olmalı.

Onay süreci bulunmalı.

---

# 24. MESAİ / PUANTAJ

Günlük:

```text
Giriş
Çıkış
Çalışılan Saat
Fazla Mesai
Eksik Mesai
```

hesaplanabilmeli.

Personel bazında aylık rapor:

```text
Normal çalışma
Fazla mesai
Devamsızlık
İzin
```

göstermeli.

---

# 25. MAAŞ

Temel personel maaş kayıtlarını destekle.

Aylık:

```text
Brüt/Net bilgi
Fazla Mesai
Kesintiler
İkramiye
Avans
Net Ödeme
```

takip edilebilecek şekilde tasarla.

Gerçek bordro mevzuatı konusunda varsayım yapma; maaş modülünü gerektiğinde dış bordro sistemiyle entegre edilebilecek şekilde modüler oluştur.

---

# 26. YETKİLENDİRME

RBAC sistemi oluştur.

Örnek roller:

```text
SUPER_ADMIN
ADMIN
MANAGER
SALES
WAREHOUSE
PRODUCTION
ACCOUNTING
HR
VIEWER
```

Ancak sadece role göre değil, **permission** seviyesinde kontrol yap.

Örnek permission:

```text
product.read
product.create
product.update
product.delete

order.read
order.create
order.approve
order.cancel

invoice.read
invoice.create
invoice.cancel

production.read
production.create
production.update

customer.read
customer.create
customer.update

payment.read
payment.create
```

Kullanıcı bazında gerekiyorsa ekstra permission override desteği tasarla.

---

# 27. AUDIT LOG

Sistemde kritik her işlem loglanmalı.

Örneğin:

```text
Kim?
Ne yaptı?
Hangi kayıtta?
Eski değer?
Yeni değer?
Ne zaman?
IP?
```

Özellikle:

* Sipariş onayı
* Sipariş iptali
* Fatura oluşturma
* Fatura iptali
* Cari hareket
* Ödeme
* Stok düzeltmesi
* Personel değişikliği
* Yetki değişikliği

loglanmalı.

---

# 28. BİLDİRİMLER

Sistem içi notification sistemi oluştur.

Örnek:

```text
Yeni sipariş onayı bekliyor
Yeni teklif talebi geldi
Ödeme gecikti
Kritik stok
Faturalaşmamış irsaliye
İzin onayı bekliyor
Sevkiyat hazır
```

Bildirimler kullanıcıya göre filtrelenmeli.

---

# 29. DASHBOARD

Dashboard rol bazlı değişmeli.

Manager:

```text
Bugünkü satış
Bugünkü üretim
Bekleyen sipariş
Bekleyen onay
Tahsilat
Geciken ödemeler
Stok uyarıları
Üretim performansı
```

Depo:

```text
Stok
Kritik stok
Bekleyen sevkiyat
Giriş/çıkış
```

Üretim:

```text
Aktif iş emirleri
Makine durumu
Üretim
Fire
```

Muhasebe:

```text
Tahsilat
Borç
Alacak
Geciken fatura
Faturalaşmamış irsaliye
```

İK:

```text
Bugünkü personel
Devamsızlık
İzin
Mesai
```

görmeli.

---

# 30. RAPORLAR

Minimum şu raporlar olacak:

## Satış

* Günlük
* Haftalık
* Aylık
* Yıllık
* Müşteri bazlı
* Ürün bazlı

## Üretim

* Makine bazlı
* Personel bazlı
* Ürün bazlı
* Günlük
* Aylık
* Yıllık
* Fire

## Stok

* Mevcut stok
* Kritik stok
* Stok hareketleri
* Depo bazlı stok
* Ürün bazlı stok

## Cari

* Borç/alacak
* Cari ekstre
* Geciken ödemeler
* Müşteri risk raporu

## Fatura

* Günlük
* Haftalık
* Aylık
* Yıllık
* Ödeme durumuna göre

## İrsaliye

* Günlük
* Bekleyen
* Faturalaşmamış

## Personel

* Puantaj
* Fazla mesai
* İzin
* Devamsızlık
* Çalışma süresi

Tüm uygun raporlar:

```text
Excel
PDF
CSV
```

olarak export edilebilmeli.

---

# 31. ARAMA / FİLTRELEME

Sistemdeki tüm büyük listelerde:

* Search
* Sort
* Pagination
* Advanced filter
* Date range
* Status filter
* Export

olmalı.

Örneğin siparişlerde:

```text
Müşteri
Sipariş no
Durum
Tarih
Tutar
```

ile filtreleme yapılmalı.

---

# 32. UI / UX

UI profesyonel bir kurumsal ERP uygulaması gibi olmalı.

Tasarım:

* Modern
* Temiz
* Yoğun bilgi gösterimine uygun
* Masaüstü odaklı fakat responsive
* Mobilde operasyonel
* Tutarlı component sistemi

Kullan:

```text
Sidebar
Topbar
Breadcrumb
Data Table
Modal
Drawer
Tabs
Card
Form
Date picker
Dropdown
Search
Status badge
Toast
Confirmation dialog
```

Aşırı dekoratif tasarım yapma.

Öncelik:

**kullanılabilirlik + hız + bilgi yoğunluğu + netlik**

olmalı.

Türkçe arayüz kullan.

---

# 33. NUMARALANDIRMA

Belge numaraları düzgün şekilde otomatik oluşturulmalı.

Örnek:

```text
SIP-2026-000001
IRS-2026-000001
FAT-2026-000001
TEK-2026-000001
URE-2026-000001
SEV-2026-000001
```

Race condition oluşmadan transaction-safe sequence sistemi kullan.

---

# 34. DOSYA YÖNETİMİ

Dosyalar doğrudan database BLOB olarak tutulmak zorunda değil.

Server filesystem / storage altında:

```text
/uploads/products
/uploads/customers
/uploads/invoices
/uploads/delivery-notes
/uploads/quotes
/uploads/documents
```

gibi mantıklı klasörleme yap.

Dosya metadata database'de tutulmalı.

---

# 35. BACKUP

Database için otomatik backup sistemi ekle.

En az:

```text
Daily backup
Retention policy
Manual backup
Restore documentation
```

olmalı.

Backup başarısız olursa admin'e bildirim üret.

---

# 36. SECURITY

Minimum:

* Password hashing
* JWT
* Refresh token
* Role authorization
* Permission authorization
* Input validation
* SQL injection protection
* XSS protection
* CSRF uygun endpointlerde
* Rate limiting
* Secure headers
* File upload validation
* MIME validation
* Maximum upload size
* Audit logs
* Session invalidation
* Password policy

kullan.

Parolaları düz metin saklama.

---

# 37. API

API RESTful hazırlanmalı.

Örnek:

```text
/api/auth
/api/users
/api/roles
/api/products
/api/customers
/api/quotes
/api/orders
/api/delivery-notes
/api/shipments
/api/invoices
/api/payments
/api/current-accounts
/api/warehouses
/api/stocks
/api/production
/api/machines
/api/employees
/api/attendance
/api/leaves
/api/reports
/api/notifications
```

Swagger/OpenAPI tam dokümante edilmeli.

Her endpoint için:

* request DTO
* response DTO
* validation
* error handling
* authorization

uygulanmalı.

Entity'leri doğrudan API response olarak döndürme.

---

# 38. ERROR HANDLING

Global exception middleware oluştur.

Standart API response modeli kullan.

Örneğin:

```json
{
  "success": false,
  "message": "Sipariş onaylanamadı.",
  "errors": []
}
```

HTTP status code'ları doğru kullan.

Frontend kullanıcıya teknik exception göstermemeli.

---

# 39. TRANSACTION VE VERİ TUTARLILIĞI

Özellikle aşağıdaki işlemler transaction içinde yapılmalı:

### Sipariş onayı

```text
Order approval
+
Stock check
+
Stock reservation
+
Status update
+
Audit log
+
Notification
```

### İrsaliye

```text
Delivery note creation
+
Stock deduction
+
Stock movement
+
Audit log
```

### Ödeme

```text
Payment
+
Current transaction
+
Balance recalculation
+
Audit log
```

### Üretim

```text
Production completion
+
Stock increase
+
Production record
+
Machine statistics
```

Race condition ve duplicate transaction problemlerine karşı önlem al.

---

# 40. DOMAIN KURALLARI

Önemli iş kuralları:

1. Onaylanmamış sipariş kesin sipariş sayılamaz.
2. İptal edilmiş sipariş sevk edilemez.
3. Stoktan fazla ürün sevk edilemez.
4. Faturalanmış irsaliye yanlışlıkla tekrar faturalanamaz.
5. Aynı ödeme iki kez uygulanamaz.
6. Cari bakiyesi hesaplama hatasına açık olmamalı.
7. Stok hareketleri izlenebilir olmalı.
8. Onay işlemleri audit log'a yazılmalı.
9. Yetkisi olmayan kullanıcı mali işlemleri değiştirememeli.
10. Silinmiş kritik finansal kayıtlar fiziksel olarak yok edilmemeli.
11. Aynı belge numarası iki kez oluşmamalı.
12. Üretim tamamlanmadan üretim stoğa eklenmemeli.
13. İrsaliye iptali gerekiyorsa stok hareketi ters kayıt ile düzeltilmeli.
14. Fatura iptali gerekiyorsa ilişkili finansal kayıtlar korunmalı.
15. Tüm tarih/saat işlemleri merkezi ve tutarlı timezone politikası kullanmalı.

---

# 41. LOCAL SERVER

Sistem şirket bilgisayarındaki server'da çalışabilecek.

Docker kullan.

Minimum:

```text
docker-compose.yml
```

içerisinde:

```text
backend
frontend
postgres
reverse-proxy
```

bulunabilir.

Development:

```text
docker compose up
```

ile ayağa kalkmalı.

Production deployment dokümantasyonu oluştur.

Server:

```text
Windows Server veya Linux
```

üzerinde çalışabilecek şekilde tasarla.

---

# 42. NETWORK

Sistem şirket LAN'ında çalışabilmeli.

Örneğin:

```text
http://erp.local
```

veya şirket DNS/IP üzerinden erişilebilir.

Mobil cihazlar şirket Wi-Fi'sına bağlandığında API'ye erişebilmeli.

CORS, HTTPS ve local network konfigürasyonunu dokümante et.

---

# 43. OFFLINE / NETWORK PROBLEMLERİ

Mobil uygulama en azından bazı read-only verileri cache edebilmeli.

Barkod okuma sırasında network kesilirse kullanıcıya açık hata göster.

Finansal veya stok hareketlerinde offline transaction yapıp sessizce göndermek yerine güvenli davran.

---

# 44. TEST

Gerçek testler oluştur.

### Backend

* Unit tests
* Integration tests
* Database tests

### Frontend

* Component tests
* API interaction tests

### E2E

Minimum aşağıdaki senaryoları test et:

```text
Müşteri oluştur
↓
Ürün oluştur
↓
Teklif talebi oluştur
↓
Teklif oluştur
↓
Sipariş oluştur
↓
Siparişi onayla
↓
İrsaliye oluştur
↓
Sevkiyat oluştur
↓
Fatura oluştur
↓
Ödeme oluştur
↓
Cari bakiyeyi kontrol et
```

Üretim:

```text
İş emri oluştur
↓
Üretim kaydı
↓
Üretimi tamamla
↓
Stok artışını doğrula
```

---

# 45. SEED DATA

Development ortamı için örnek seed data oluştur.

En az:

```text
20 ürün
5 müşteri
3 kullanıcı
5 rol
3 makine
10 personel
2 depo
```

oluştur.

Örnek yönetici hesabı da oluştur ama ilk girişte password değiştirmeye zorla.

---

# 46. INTERNATIONALIZATION

İlk sürüm Türkçe olacak.

Kod mimarisini ileride İngilizce desteği eklenebilecek şekilde tasarla.

Para birimi:

```text
TRY
```

temel olacak.

Tarih formatları Türkiye'ye uygun gösterilecek ancak database UTC standardına uygun tasarlanacak.

---

# 47. FRONTEND FORM KURALLARI

Formlar validation içermeli.

Örneğin:

```text
Müşteri adı zorunlu
Ürün miktarı > 0
Fiyat >= 0
Tarih geçerli
E-posta formatı
Telefon formatı
```

Validation frontend + backend tarafında yapılmalı.

---

# 48. EMPTY / LOADING / ERROR STATES

Her sayfada düşün:

```text
Loading
Empty
Error
Success
Permission denied
No results
```

durumları düzgün göster.

Kullanıcı "sayfa bozuldu mu?" diye düşünmemeli.

---

# 49. ACCESSIBILITY

Temel accessibility kurallarına uy.

* Keyboard navigation
* Focus state
* Semantic HTML
* Label
* Contrast
* Accessible buttons

---

# 50. PERFORMANS

Sistemin 100 ürünle sınırlı kalacağını varsayma.

İleride:

```text
10.000+
müşteri
100.000+
sipariş
milyonlarca stok hareketi
```

olabileceğini varsay.

Bu nedenle:

* Pagination
* DB index
* server-side filtering
* projection
* caching gerektiği yerde
* async I/O
* N+1 query önleme
* lazy/eager loading bilinçli kullanımı

uygulanmalı.

Dashboard sorgularını optimize et.

---

# 51. RAPORLARDA GRAFİKLER

Grafikler:

* Satış trendi
* Üretim trendi
* Ürün satış dağılımı
* Müşteri satış dağılımı
* Ödeme performansı
* Stok durumu
* Makine performansı

gösterebilmeli.

Grafikler tarih aralığına göre güncellenebilmeli.

---

# 52. PDF

Aşağıdaki belgeler PDF üretmeli:

```text
Teklif
Sipariş
İrsaliye
Fatura
Cari Ekstre
Raporlar
```

PDF'lerde şirket bilgileri ve profesyonel tasarım bulunmalı.

---

# 53. EXCEL

Excel export aşağıdaki alanlarda olmalı:

```text
Ürün
Stok
Sipariş
Müşteri
Cari
Ödeme
Üretim
Personel
Raporlar
```

---

# 54. LOGGING / MONITORING

Backend logları structured logging formatında olsun.

En az:

```text
Info
Warning
Error
Critical
```

seviyeleri.

Unhandled exception'lar loglanmalı.

Health endpoint:

```text
/health
```

oluştur.

Database bağlantısı ve kritik servislerin sağlık durumu kontrol edilebilmeli.

---

# 55. DOCUMENTATION

Projede şu dosyaları oluştur:

```text
README.md
ARCHITECTURE.md
DATABASE.md
API.md
DEPLOYMENT.md
SECURITY.md
BACKUP.md
USER_ROLES.md
BUSINESS_RULES.md
TESTING.md
```

README içerisinde projeyi sıfırdan çalıştırma adımlarını yaz.

---

# 56. GIT YAPISI

Monorepo kullan.

Örnek:

```text
/company-erp

/apps
  /web
  /mobile
  /api

/packages
  /shared-types
  /shared-config

/infrastructure
  /docker
  /nginx

/docs

/tests
```

Gerekiyorsa yapıyı teknolojiye göre optimize et.

Git history temiz ve anlamlı olacak şekilde commit stratejisi kullan.

---

# 57. KOD KALİTESİ

Kod:

* SOLID
* DRY
* KISS
* Clean Code
* Domain driven yaklaşım gereken yerlerde
* Dependency Injection
* DTO
* Mapper
* Service
* Repository sadece gerçekten gerekli yerde

prensiplerine uygun olmalı.

Aşırı abstraction yapma.

"Enterprise olsun" diye gereksiz karmaşıklık oluşturma.

Kod okunabilir ve junior developer'ın devam ettirebileceği seviyede olsun.

---

# 58. AI AGENT ÇALIŞMA KURALLARI

Bu projeyi geliştirirken aşağıdaki kurallar kesinlikle uygulanacak:

## Kural 1

Eksik olduğunu düşündüğün teknik ayrıntılarda mantıklı profesyonel varsayım yap ve ilerle.

Gereksiz şekilde kullanıcıdan sürekli onay isteme.

## Kural 2

Bir özelliği yarım bırakma.

Bir feature:

```text
Database
+
Backend
+
API
+
Frontend
+
Validation
+
Authorization
+
Testing
```

seviyesinde tamamlanmalı.

## Kural 3

Mock data ile bitirme.

Mock implementation varsa development seed data olarak kullan; gerçek API'yi yaz.

## Kural 4

TODO bırakma.

Placeholder:

```text
implement later
coming soon
TODO
```

gibi yarım alan bırakma.

Zorunlu bir entegrasyon henüz mümkün değilse abstraction layer oluştur ve açıkça dokümante et.

## Kural 5

Kod yazmadan önce mimariyi oluştur.

Önce:

```text
Architecture
Database schema
Entity relationships
API structure
Permission model
UI route map
```

oluştur.

Sonra implementasyona geç.

## Kural 6

Her yeni modül önceki modüllerle entegre çalışmalı.

Örneğin:

```text
Order → Delivery Note → Shipment → Invoice → Current Account
```

kopuk modüller olmamalı.

## Kural 7

Financial ve stock işlemlerinde transaction kullan.

## Kural 8

Güvenlikten taviz verme.

## Kural 9

UI'da Türkçe kullan.

Code/entity/property isimlerinde İngilizce kullan.

---

# 59. GELİŞTİRME SIRASI

Projeyi kendi içinde şu sırayla geliştir:

### Phase 1

Architecture + repository + Docker + database

### Phase 2

Authentication + authorization + users + roles + permissions

### Phase 3

Products + customers

### Phase 4

Warehouse + stock + barcode

### Phase 5

Quotes + quote requests

### Phase 6

Sales orders + approval workflow

### Phase 7

Delivery notes + shipment

### Phase 8

Invoices + current accounts + payments

### Phase 9

Production + machines + production personnel

### Phase 10

Employees + attendance + leave + overtime

### Phase 11

Reports + dashboard

### Phase 12

Notifications + audit log

### Phase 13

Mobile application

### Phase 14

PDF + Excel + backup + deployment

### Phase 15

Full integration testing + performance + security review

Bu sıralamayı kullanıcıdan onay beklemeden uygula.

---

# 60. SON KABUL KRİTERİ

Proje ancak aşağıdaki senaryo uçtan uca başarıyla çalıştığında tamamlanmış kabul edilir:

```text
1. Admin giriş yapar.

2. Müşteri oluşturur.

3. Ürün oluşturur.

4. Ürün barkodu tanımlar.

5. Depo oluşturur.

6. Üretim iş emri oluşturur.

7. Makinede üretim gerçekleşir.

8. Personel üretime atanır.

9. Üretim tamamlanır.

10. Ürün stoğa girer.

11. Müşteri ürün kataloğunu görür.

12. Teklif talebi oluşturur.

13. Şirket çalışanı teklif hazırlar.

14. Teklif siparişe dönüşür.

15. Sipariş onaya gönderilir.

16. Yetkili siparişi onaylar.

17. Stok rezerve edilir.

18. İrsaliye hazırlanır.

19. Stok çıkışı yapılır.

20. Sevkiyat oluşturulur.

21. Sevkiyat tamamlanır.

22. Fatura oluşturulur.

23. Cari borç oluşur.

24. Müşteri ödeme yapar.

25. Ödeme cari hesaba işlenir.

26. Bakiye güncellenir.

27. Cari ekstre görüntülenir.

28. Yönetici satış raporunu görür.

29. Üretim raporunu görür.

30. Riskli müşterileri görür.

31. Faturalaşmamış irsaliyeleri görür.

32. Personel puantajını görür.

33. Mobil cihazdan barkod okutulur.

34. Yetkisiz kullanıcı finansal alanlara erişemez.

35. Audit log tüm kritik işlemleri gösterir.
```

Bu senaryonun tümü gerçek veritabanı ve gerçek API üzerinden çalışmalıdır.

---

# 61. ÇIKTI BEKLENTİSİ

Bu promptu alan AI agent yalnızca kod üretmesin.

Kendisini şu şekilde çalıştırmalı:

```text
ANALYZE
↓
ARCHITECT
↓
DESIGN
↓
IMPLEMENT
↓
MIGRATE
↓
TEST
↓
DEBUG
↓
INTEGRATE
↓
SECURE
↓
DOCUMENT
↓
DEPLOY
↓
VERIFY
```

Her aşamada kendi ürettiği kodu eleştirel olarak kontrol et.

Compile error, migration error, TypeScript error, dependency error, runtime error, API contract error veya integration error varsa kendisi çözmeden sonraki aşamaya geçme.

Frontend ile backend contract'larını sürekli doğrula.

Database migration'larını test et.

Seed data ile sistemi ayağa kaldır.

E2E workflow'u çalıştır.

Son aşamada projeyi temiz bir makinede sıfırdan kuruyormuş gibi doğrula.

---

# 62. SON TESLİM

Sonuçta aşağıdakilerin hepsi çalışır halde bulunmalı:

```text
✅ Backend
✅ PostgreSQL database
✅ Web application
✅ Mobile application
✅ Authentication
✅ Authorization
✅ Product management
✅ Customer management
✅ Quote request
✅ Quote
✅ Sales order
✅ Approval workflow
✅ Warehouse
✅ Stock
✅ Barcode
✅ Production
✅ Machine tracking
✅ Personnel tracking
✅ Shipment
✅ Delivery note
✅ Invoice
✅ Current account
✅ Payment
✅ Risk analysis
✅ Reports
✅ Dashboard
✅ Notifications
✅ Audit log
✅ PDF
✅ Excel
✅ Backup
✅ Docker
✅ Deployment documentation
✅ Automated tests
```

Sistem yalnızca demo gibi görünmemeli.

**Gerçek şirket operasyonunu yönetebilecek bir temel ürün kalitesinde olmalı.**

Önceliğin görsel efektler değil:

**veri doğruluğu + iş akışı doğruluğu + güvenlik + hata toleransı + sürdürülebilir mimari + kullanıcı deneyimi** olmalı.

Projeyi gereksiz yere basitleştirme. Ancak ihtiyacın olmayan mikroservis, Kafka, Kubernetes vb. altyapıları sırf "enterprise" görünmek için ekleme. İlk sürüm **modüler monolith** olarak tasarlanmalı; gelecekte büyütülebilecek şekilde kodlanmalı.

Son olarak sistemin tüm modüllerinin aynı gerçek veri modeline bağlı olduğundan emin ol. Aynı müşteri, ürün, sipariş, stok, irsaliye, fatura, cari ve personel bilgisi farklı modüllerde duplicate olarak tutulmamalı.

**Amaç: şirket içinde her gün kullanılabilecek gerçek bir üretim/satış/depo/cari/insan kaynakları yönetim platformu oluşturmaktır.**
