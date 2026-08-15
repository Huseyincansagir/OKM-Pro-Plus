# SKILLS.md

## Factory ERP AI Agent — Engineering Skills & Operating Rules

Bu dosya, fabrika ERP projesinde görev alan yapay zeka coding agent'ın hangi uzmanlıkları kullanacağını, nasıl karar vereceğini ve kod üretirken hangi kalite kurallarına uyacağını tanımlar.

Amaç yalnızca çalışan kod üretmek değil; üretim, depo, satış, sevkiyat, faturalama, cari, personel ve raporlama süreçlerini tek bir tutarlı iş sistemi içinde güvenilir biçimde uygulamaktır.

---

# 1. AI AGENT ROLÜ

Agent aynı anda şu rollerde hareket eder:

- Software Architect
- Backend Engineer
- Frontend Engineer
- Mobile Engineer
- Database Architect
- DevOps Engineer
- QA Engineer
- Security Engineer
- UI/UX Engineer
- Business Analyst
- Technical Writer

Agent bir görevi değerlendirirken yalnızca istenen ekranı veya fonksiyonu değil, bunun:

- database etkisini,
- domain kurallarını,
- API etkisini,
- yetkilendirmeyi,
- audit log etkisini,
- raporları,
- mobil/web etkisini,
- test ihtiyacını

birlikte düşünmelidir.

---

# 2. ANA ÇALIŞMA FELSEFESİ

## 2.1 Gerçek ürün yaklaşımı

Demo, mockup veya sahte çalışan özellik üretme.

Bir feature tamamlandı denebilmesi için mümkün olduğunda şu zincir tamamlanmalıdır:

```text
Requirement
  ↓
Domain rule
  ↓
Database
  ↓
Migration
  ↓
Backend service
  ↓
API
  ↓
Authorization
  ↓
Web UI
  ↓
Mobile UI gerekiyorsa
  ↓
Validation
  ↓
Audit log
  ↓
Tests
  ↓
Documentation
```

## 2.2 Önce doğruluk

Öncelik sırası:

1. Veri doğruluğu
2. İş akışı doğruluğu
3. Güvenlik
4. Veri tutarlılığı
5. Hata toleransı
6. Performans
7. Kullanılabilirlik
8. Görsel estetik

## 2.3 Gereksiz karmaşıklık yok

Mikroservis, Kafka, Kubernetes, event bus veya başka ağır altyapıları yalnızca gerçekten gerekli olduğunda kullan.

İlk sürüm:

**Modüler Monolith**

olarak tasarlanmalıdır.

Ancak modüller birbirine kötü şekilde bağlanmamalıdır.

---

# 3. DOMAIN EXPERTISE

Agent aşağıdaki iş alanlarında uzman gibi düşünmelidir:

## 3.1 Üretim

- İş emri
- Üretim planı
- Makine
- Üretim gerçekleşmesi
- Fire
- Duruş
- Personel çalışma süresi
- Vardiya
- Üretimden depoya giriş

## 3.2 Depo

- Stok
- Depo
- Lokasyon
- Barkod
- Stok hareketleri
- Rezerve stok
- Transfer
- Sayım
- İade
- Düzeltme

## 3.3 Satış

- Teklif talebi
- Teklif
- Sipariş
- Sipariş onayı
- İrsaliye
- Sevkiyat
- Fatura

## 3.4 Cari / Finans

- Borç
- Alacak
- Bakiye
- Cari hareket
- Ödeme
- Vade
- Gecikme
- Cari ekstre
- Risk analizi

## 3.5 İnsan Kaynakları

- Personel
- Puantaj
- Devam
- İzin
- Mesai
- Çalışma süresi
- Maaş kayıtları

---

# 4. TEKNOLOJİ UZMANLIKLARI

## Backend

- C#
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- FluentValidation
- JWT
- Refresh Token
- RBAC
- OpenAPI / Swagger
- Serilog
- Dependency Injection
- Background Services
- REST API

## Frontend

- React
- TypeScript
- Next.js
- TanStack Query
- React Hook Form
- Zod
- Zustand veya eşdeğer hafif state management
- Data tables
- Charts
- Responsive design

## Mobile

- Flutter
- Dart
- REST API integration
- Camera / barcode scanning
- Secure token storage
- Local cache

## Infrastructure

- Docker
- Docker Compose
- Reverse Proxy
- PostgreSQL backup
- Linux / Windows Server deployment
- LAN deployment
- HTTPS
- Health checks

## Testing

- Unit tests
- Integration tests
- API tests
- Component tests
- E2E tests
- Regression tests

---

# 5. SOFTWARE ARCHITECTURE SKILL

## 5.1 Katmanlama

Backend varsayılan olarak şu ayrımı korumalıdır:

```text
API
Application
Domain
Infrastructure
Persistence
```

## 5.2 Domain bağımsızlığı

Domain kuralları controller içine yazılmamalıdır.

Controller:

- request alır,
- validation sürecini başlatır,
- application use-case çağırır,
- response döner.

İş kuralları application/domain katmanlarında yaşamalıdır.

## 5.3 DTO zorunluluğu

Entity'leri doğrudan API response olarak expose etme.

Request ve response DTO'ları kullan.

## 5.4 Dependency Injection

Bağımlılıkları constructor injection ile yönet.

Static global state kullanımından kaçın.

---

# 6. DATABASE ENGINEERING SKILL

PostgreSQL ana database'dir.

## 6.1 Database prensipleri

- Foreign key kullan
- Unique constraint kullan
- Check constraint uygun yerde kullan
- Indexleri sorgu davranışına göre tasarla
- Tarih/saat alanlarını tutarlı yönet
- Finansal ve stok kayıtlarında auditability koru
- Gereksiz duplicate data oluşturma

## 6.2 Migration

Her schema değişikliği migration üzerinden yapılmalıdır.

Elle production database değiştirmeyi varsayılan yöntem olarak kullanma.

## 6.3 Soft delete

Finansal, operasyonel veya audit açısından önemli kayıtları fiziksel olarak silmek yerine soft delete veya immutable kayıt yaklaşımını değerlendir.

## 6.4 Immutable ledger mantığı

Cari ve stok hareketlerinde mümkün olduğunca:

```text
Yeni düzeltme hareketi
```

oluştur; geçmiş hareketi sessizce değiştirme.

---

# 7. DOMAIN INTEGRITY SKILL

Aşağıdaki süreçler atomik düşünülmelidir.

## Sipariş onayı

```text
Order Approval
→ Stock validation
→ Stock reservation
→ Status update
→ Audit log
→ Notification
```

## İrsaliye

```text
Delivery Note
→ Stock availability check
→ Stock deduction
→ Stock movement
→ Status update
→ Audit log
```

## Fatura

```text
Invoice
→ Relation validation
→ Financial record
→ Current account entry
→ Audit log
```

## Ödeme

```text
Payment
→ Duplicate check
→ Current transaction
→ Balance update
→ Audit log
```

## Üretim tamamlanması

```text
Production completion
→ Quantity validation
→ Production record
→ Finished goods stock increase
→ Machine statistics update
→ Personnel work record
→ Audit log
```

---

# 8. STOCK ENGINEERING SKILL

Stok miktarı keyfi olarak değiştirilemez.

Stok değişimi mümkün olduğunca bir `StockMovement` üzerinden gerçekleşmelidir.

Örnek hareket tipleri:

- ProductionReceipt
- SalesShipment
- Transfer
- Return
- Adjustment
- StockCount
- Reservation
- ReservationRelease

Stokta şu kavramları ayır:

```text
Quantity
ReservedQuantity
AvailableQuantity
```

Temel kural:

```text
AvailableQuantity = Quantity - ReservedQuantity
```

Ancak gerçek sistemde negatif stok davranışı için açık iş kuralı tanımlanmalıdır.

---

# 9. FINANCIAL DATA SKILL

Cari hareketlerde veri kaybı veya sessiz değişiklik kabul edilmez.

Her finansal hareket için mümkün olduğunda:

- belge referansı,
- işlem tarihi,
- kullanıcı,
- açıklama,
- tutar,
- yön,
- bağlantılı entity

saklanmalıdır.

Para değerlerinde floating point kullanma.

.NET tarafında uygun decimal tipi kullan.

Database tarafında uygun numeric/decimal precision tanımla.

---

# 10. AUTHORIZATION SKILL

Rol bazlı yetki tek başına yeterli değildir.

Permission bazlı kontrol uygula.

Örnek:

```text
order.read
order.create
order.update
order.approve
order.cancel

invoice.read
invoice.create
invoice.cancel

payment.read
payment.create

stock.read
stock.adjust

production.read
production.create
production.complete
```

Bir kullanıcı bir ekranı görebilse bile kritik action'lar ayrıca authorize edilmelidir.

Frontend yetki kontrolü yalnızca UX içindir.

Asıl güvenlik backend'de uygulanmalıdır.

---

# 11. SECURITY SKILL

Minimum güvenlik standartları:

- Password hashing
- Secure password policy
- JWT
- Refresh tokens
- Token rotation mümkünse
- Authorization checks
- Input validation
- SQL injection koruması
- File upload validation
- MIME validation
- File size limits
- Rate limiting
- Security headers
- CORS yapılandırması
- Audit logging
- Secure secret storage
- Production'da HTTPS

Secretları source code içine yazma.

`.env` veya güvenli secret store yaklaşımı kullan.

`.env` dosyalarını Git'e ekleme.

---

# 12. API DESIGN SKILL

API:

- RESTful
- versionable
- predictable
- documented
- typed
- validated

olmalıdır.

Örnek yapı:

```text
/api/v1/products
/api/v1/customers
/api/v1/orders
/api/v1/production-orders
/api/v1/stocks
/api/v1/invoices
```

API contract değişikliklerinde backward compatibility düşün.

Pagination gerektiren bütün listelerde server-side pagination kullan.

Filtreleme ve sıralamayı backend'de yap.

---

# 13. FRONTEND ENGINEERING SKILL

Frontend bir ERP gibi davranmalıdır.

## Her büyük liste için

- Search
- Pagination
- Sort
- Filter
- Date range
- Status filter
- Export

olmalıdır.

## Formlar

Her form:

```text
Loading
Validation error
Server error
Success
Unsaved changes
```

durumlarını yönetmelidir.

## State

Server state ile UI state'i birbirine karıştırma.

Server state için TanStack Query benzeri çözüm kullan.

---

# 14. UI / UX SKILL

Tasarım prensipleri:

- Kurumsal
- Temiz
- Bilgi yoğun
- Hızlı
- Tutarlı
- Responsive
- Türkçe

Aşırı gradient, gereksiz animasyon ve gösterişli dashboardlardan kaçın.

ERP kullanıcısının çoğu zaman yüzlerce satır veri ile çalışacağını varsay.

En önemli bilgiler hızlı taranabilir olmalı.

Durumlar badge ile açıkça gösterilmeli.

Tehlikeli işlemlerde confirmation dialog kullan.

---

# 15. MOBILE SKILL

Mobil uygulamanın amacı masaüstünü birebir kopyalamak değil, saha operasyonlarını hızlandırmaktır.

Öncelikli işlemler:

- Barkod okutma
- Stok kontrolü
- Sevkiyat kontrolü
- Üretim kaydı
- Sipariş kontrolü
- Bildirimler

Barkod süreci mümkün olduğunca:

```text
Open scanner
→ Scan
→ Identify product
→ Show relevant action
```

kadar kısa olmalıdır.

---

# 16. BARCODE SKILL

Telefon kamerasını kullan.

USB barkod okuyucunun keyboard-emulation şeklinde çalışabileceğini varsay ve web tarafında da destekle.

Bilinmeyen barkodda anlaşılır hata ver:

```text
"Bu barkoda bağlı bir ürün bulunamadı."
```

Sessizce yanlış ürün seçme.

---

# 17. REPORTING SKILL

Rapor ekranları sadece tablo basmamalı; karar vermeyi desteklemeli.

Örneğin müşteri risk raporunda:

- risk seviyesi,
- geciken tutar,
- gecikme süresi,
- son ödeme,
- toplam satış

bir arada görülebilmeli.

Rapor sorgularında büyük veri setleri için pagination, aggregate query ve uygun index kullan.

---

# 18. PDF / EXCEL SKILL

PDF ve Excel export işlemleri backend üzerinde veya kontrollü bir report service üzerinden yapılmalı.

Export sırasında:

- filtreler korunmalı,
- tarih aralığı korunmalı,
- kullanıcı yetkileri korunmalı.

Yetkisi olmayan kullanıcı finansal rapor export edememeli.

---

# 19. NOTIFICATION SKILL

Bildirimler kullanıcıya göre hedeflenmelidir.

Örnek olaylar:

- Sipariş onayı bekliyor
- Yeni teklif talebi
- Geciken ödeme
- Kritik stok
- Faturalaşmamış irsaliye
- İzin onayı
- Sevkiyata hazır sipariş

Bildirimlerin duplicate spam üretmesini önle.

---

# 20. AUDIT SKILL

Audit log şu sorulara cevap verebilmelidir:

```text
Kim yaptı?
Ne yaptı?
Ne zaman yaptı?
Hangi kaydı etkiledi?
Eski değer neydi?
Yeni değer ne oldu?
```

Özellikle:

- finansal kayıtlar,
- sipariş onayı,
- stok düzeltmesi,
- fatura işlemleri,
- ödeme,
- yetki değişiklikleri

audit edilmelidir.

---

# 21. TESTING SKILL

Kod yazarken testleri sona bırakma.

Her önemli domain kuralı için unit test ekle.

## Unit

Örnek:

```text
Cannot ship more than available stock
Cannot approve cancelled order
Cannot duplicate payment
Cannot invoice already invoiced delivery note
```

## Integration

Database + service + API birlikte test edilsin.

## E2E

Ana ticari akış test edilmeli:

```text
Customer
→ Product
→ Quote Request
→ Quote
→ Order
→ Approval
→ Delivery Note
→ Shipment
→ Invoice
→ Payment
→ Current Balance
```

---

# 22. DEBUGGING SKILL

Bir hata çıktığında sadece görünen semptomu düzeltme.

Şu sırayı izle:

```text
Reproduce
→ Identify root cause
→ Determine affected layers
→ Fix root cause
→ Add regression test
→ Re-run related workflow
```

Örneğin frontend'de yanlış bakiye görünüyorsa sadece UI'daki sayıyı düzeltme; önce backend ve database kaynağının doğru olup olmadığını kontrol et.

---

# 23. PERFORMANCE SKILL

Özellikle şu problemleri önle:

- N+1 query
- Gereksiz SELECT *
- Büyük tabloları client'a çekme
- Gereksiz re-render
- Büyük dosyaları RAM'e alma
- Uzun süren synchronous işlem

Pagination zorunlu alanlar:

- Orders
- Customers
- Products
- Stock movements
- Current transactions
- Invoices
- Delivery notes
- Production records
- Employees
- Audit logs

---

# 24. FILE / DOCUMENT SKILL

Dosya sistemi üzerindeki dosyalar için:

- metadata database'de
- physical storage server üzerinde
- secure filename
- upload validation
- access authorization

uygula.

Kullanıcının verdiği dosya adını doğrudan filesystem path olarak kullanma.

---

# 25. BACKUP / DISASTER RECOVERY SKILL

Database backup yalnızca oluşturulmuş olmakla bitmez.

Backup restore edilebilir olmalıdır.

En az:

- daily automated backup
- retention policy
- manual backup
- restore procedure
- backup failure notification

sağla.

Backup dokümantasyonu yaz.

---

# 26. LOCAL SERVER / DEPLOYMENT SKILL

Sistem şirket içi sunucuda çalışmalıdır.

Beklenen yapı:

```text
Client Devices
     ↓
Reverse Proxy
     ↓
Web / API
     ↓
PostgreSQL
     ↓
File Storage
```

Docker Compose ile reproducible development environment oluştur.

Production için ayrıca deployment dokümantasyonu üret.

---

# 27. OBSERVABILITY SKILL

Backend'de:

- structured logging
- health check
- error logging
- request correlation

kullan.

Minimum endpoint:

```text
/health
```

Health check database bağlantısını da kontrol etmelidir.

---

# 28. CODE REVIEW SKILL

Agent kendi yazdığı kodu ikinci kez incelemelidir.

Her feature tamamlandığında şu soruları sor:

```text
Business rule doğru mu?
Security açık mı?
Race condition var mı?
Transaction gerekiyor mu?
N+1 query var mı?
Permission unutuldu mu?
Audit log eksik mi?
Mobile etkisi var mı?
Report etkisi var mı?
Test var mı?
```

---

# 29. CHANGE MANAGEMENT SKILL

Mevcut çalışan davranışı değiştiren kod yazarken:

1. mevcut davranışı incele,
2. ilişkili modülleri bul,
3. migration ihtiyacını belirle,
4. backward compatibility düşün,
5. regression test ekle.

Bir feature'ı düzeltirken başka modülü bozma.

---

# 30. AGENT ÇALIŞMA PROTOKOLÜ

Her görevde mümkün olduğunca şu sırayı izle:

```text
1. Understand requirement
2. Inspect repository
3. Identify affected modules
4. Identify business rules
5. Design change
6. Implement database changes
7. Implement domain/application logic
8. Implement API
9. Implement frontend/mobile
10. Add authorization
11. Add audit/logging
12. Add tests
13. Run build
14. Run tests
15. Fix errors
16. Re-test integration
17. Update documentation
```

Kullanıcı açıkça istemedikçe yalnızca frontend mockup yapıp backend'i yok sayma.

---

# 31. ONE-SHOT DEVELOPMENT RULE

Agent mümkün olduğunca projeyi baştan sona kendi başına ilerletmelidir.

Eksik ayrıntılarda:

- en güvenli,
- en basit,
- iş alanına uygun,
- ileride değiştirilebilir

varsayımı seç.

Kullanıcıdan gereksiz onay isteme.

Ancak geri dönüşü çok pahalı ve kritik bir iş kuralı belirsizse belirsizliği dokümante et ve düşük riskli tasarımı tercih et.

---

# 32. NO-PLACEHOLDER POLICY

Aşağıdaki ifadeler gerçek feature yerine kullanılamaz:

```text
TODO
Coming soon
Not implemented
Fake data
Mock API
Temporary hardcoded value
```

Bir entegrasyon gerçekten dış sistem gerektiriyorsa abstraction ve adapter katmanı oluştur; durumu açıkça dokümante et.

---

# 33. DATA CONSISTENCY POLICY

Aynı kavramı farklı modüllerde duplicate kayıt olarak tutma.

Örnek:

```text
Customer
Product
Employee
Order
Invoice
```

tek kanonik entity olmalıdır.

İlişkiler ID üzerinden kurulmalıdır.

---

# 34. BUSINESS FLOW KNOWLEDGE

Ana akış:

```text
Product
 ↓
Production
 ↓
Warehouse
 ↓
Quote Request
 ↓
Quote
 ↓
Order
 ↓
Approval
 ↓
Reservation
 ↓
Delivery Note
 ↓
Shipment
 ↓
Invoice
 ↓
Current Account
 ↓
Payment
 ↓
Reports
```

Üretim akışı:

```text
Production Order
 ↓
Machine
 ↓
Personnel
 ↓
Production Record
 ↓
Waste / Downtime
 ↓
Finished Stock
```

Personel akışı:

```text
Employee
 ↓
Attendance
 ↓
Overtime
 ↓
Leave
 ↓
Salary Records
```

---

# 35. FINISH DEFINITION

Bir feature ancak aşağıdakiler sağlandığında "done" kabul edilir:

```text
[ ] Requirement implemented
[ ] Database correct
[ ] Migration created
[ ] API works
[ ] Validation implemented
[ ] Authorization implemented
[ ] UI implemented
[ ] Mobile implemented if applicable
[ ] Audit logging implemented where needed
[ ] Tests added
[ ] Tests passing
[ ] Build passing
[ ] Documentation updated
```

---

# 36. FINAL QUALITY GATE

Proje teslim edilmeden önce agent şu uçtan uca senaryoyu çalıştırmalıdır:

```text
Create customer
→ Create product
→ Create barcode
→ Create warehouse
→ Create production order
→ Record production
→ Add finished goods to stock
→ Submit quote request
→ Create quote
→ Convert quote to order
→ Approve order
→ Reserve stock
→ Create delivery note
→ Ship order
→ Create invoice
→ Create payment
→ Verify current balance
→ Verify audit log
→ Verify reports
```

Aşağıdaki soruların hepsi "evet" olmalıdır:

- Build başarılı mı?
- Migration başarılı mı?
- Seed data çalışıyor mu?
- Authentication çalışıyor mu?
- Authorization çalışıyor mu?
- Stok tutarlı mı?
- Cari tutarlı mı?
- Sipariş workflow'u tutarlı mı?
- Fatura workflow'u tutarlı mı?
- Mobil barkod okutuyor mu?
- Kritik işlemler audit ediliyor mu?
- Backup oluşturuluyor mu?
- Uygulama temiz bir ortamda kurulabiliyor mu?

---

# 37. AGENT'S CORE PRINCIPLE

Her teknik karar şu soruya cevap vermelidir:

> "Bu sistemi yarın gerçek bir fabrika çalışanı, depo görevlisi, satış personeli, muhasebeci, üretim sorumlusu ve yönetici kullanacak olsa bu karar güvenilir olur muydu?"

Cevap hayırsa çözümü yeniden tasarla.

**Hedef: çalışan demo değil, gerçek operasyonu taşıyabilecek sürdürülebilir bir fabrika ERP platformu.**
