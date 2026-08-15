# Factory ERP — Business Workflows

## 1. Tasarım ilkesi

Her workflow yalnızca ekranlar arası bir yönlendirme değildir. Her geçiş; aktör, giriş, state, permission, database etkisi, stok etkisi, finansal etkisi ve audit gereksinimiyle birlikte tanımlanır. Uygulama aşamasında geçişler backend use-case ve transaction sınırı olarak hayata geçirilmelidir.

## 2. Satıştan tahsilata workflow'u

```text
Public Quote Request
→ Quote
→ Sales Order
→ Approval
→ Stock Reservation
→ Delivery Note
→ Shipment
→ Invoice
→ Current Account
→ Payment
```

| Geçiş | Aktör | Girdi | State | Permission | Database etkisi | Stok etkisi | Finansal etkisi | Audit |
|---|---|---|---|---|---|---|---|---|
| Talep gönderme | Public müşteri | Ürün, miktar, firma, iletişim | `NEW` | Public request submit | `QuoteRequest`, items, consent metadata | Yok | Yok | Talep oluşturma |
| Talep inceleme | Satış | Talep, müşteri eşleştirme, not | `REVIEWING` | `quote-request.review` | Talep sorumlu ve durum güncellemesi | Yok | Yok | İnceleme başlangıcı |
| Teklif oluşturma | Satış | Ürün, miktar, fiyat, iskonto, vergi, geçerlilik | `QUOTED` | `quote.create` | `Quote`, items, sequence | Yok | Tahmini toplam | Teklif oluşturma |
| Teklif kabulünden sipariş | Satış | Kabul edilmiş teklif | `SalesOrder.Draft` | `order.create` | `SalesOrder`, items, source reference | Yok | Sipariş tutarı oluşur, cari oluşmaz | Dönüşüm |
| Sipariş onaya gönderme | Satış | Taslak sipariş, teslim/ödeme bilgisi | `PendingApproval` | `order.submit` | Approval pending, order state | Yok | Yok | State transition |
| Sipariş onayı | Yönetici/sorumlu | Stok kontrolü, risk, teslim, ödeme şartı | `Approved` | `order.approve` | Approval, order state, reservation | `StockReservation` oluşturulur | Cari borç henüz oluşmaz | Onaylayan, tarih, açıklama |
| Sipariş reddi | Yönetici/sorumlu | Ret gerekçesi | `Rejected` | `order.approve` | Approval rejected, order state | Rezervasyon oluşmaz/varsa çözülür | Yok | Ret gerekçesi |
| İrsaliye hazırlama | Depo | Onaylı sipariş, sevk miktarı, adres | `Prepared` | `delivery-note.create` | DeliveryNote taslağı | Henüz kesin stok çıkışı yok | Yok | Hazırlama |
| İrsaliye kesinleştirme | Depo/yönetici | Barkod doğrulaması ve miktar | `Issued` | `delivery-note.issue` | DeliveryNote issued | `StockMovement(SalesShipment)`, reservation release | Yok veya policy'ye göre sevk geliri | Stok çıkışı |
| Sevkiyat oluşturma | Depo/sevkiyat | İrsaliye, araç, şoför | `Preparing` | `shipment.create` | Shipment ve items | İrsaliye çıkışıyla ilişkilidir | Yok | Sevkiyat hazırlığı |
| Sevk etme | Depo/sevkiyat | Yükleme ve çıkış bilgisi | `Shipped` | `shipment.ship` | Shipment state, departure | Tekrar stok düşülmez | Yok | Sevk geçişi |
| Teslim | Sevkiyat/satış | Teslim tarihi ve not | `Delivered` | `shipment.deliver` | Delivery state, proof metadata | Yok | Yok | Teslim kaydı |
| Fatura oluşturma | Muhasebe | Faturalanabilir irsaliye, vergi, vade | `Issued` | `invoice.create` | Invoice, items, sequence | Yok | `CurrentTransaction(Debit)` | Fatura ve cari etkisi |
| Ödeme alma | Muhasebe | Müşteri, tutar, yöntem, referans | `Applied` | `payment.create` | Payment, allocation, transaction | Yok | `CurrentTransaction(Credit)`, balance update | Ödeme ve dağıtım |

### Satış invariants

- Onaylanmış sipariş olmadan irsaliye kesinleştirilemez.
- Sevk miktarı `AvailableQuantity` değerini aşamaz.
- Aynı irsaliye için birden fazla fatura ilişkisi policy tarafından engellenir.
- Ödeme idempotency/reference kontrolü olmadan ikinci kez cari hesaba uygulanamaz.
- Sipariş ret veya iptal durumundan onaylı duruma geri dönemez.

## 3. Üretimden stoğa workflow'u

```text
Production Plan
→ Production Order
→ Machine Assignment
→ Personnel Assignment
→ Production
→ Scrap / Downtime
→ Production Completion
→ Stock Receipt
```

| Geçiş | Aktör | Girdi | State | Permission | Database etkisi | Stok etkisi | Finansal etkisi | Audit |
|---|---|---|---|---|---|---|---|---|
| Plan oluşturma | Üretim planlama | Ürün, hedef, tarih, öncelik | Plan | `production-order.create` | ProductionOrder | Yok | Yok | Plan oluşturma |
| Serbest bırakma | Üretim sorumlusu | Plan doğrulama, makine uygunluğu | `Released` | `production-order.release` | Order state | Malzeme policy varsa rezervasyon | Yok | Serbest bırakma |
| Makine atama | Üretim | Makine, vardiya | Atanmış | `production-order.assign-machine` | Assignment | Yok | Yok | Makine değişimi |
| Personel atama | Üretim | Personel, rol, vardiya | Atanmış | `production-order.assign-personnel` | Personnel assignment | Yok | Çalışma ilişkilendirmesi | Atama |
| Üretimi başlatma | Operatör | Başlangıç, sayaç, iş emri | `InProgress` | `production.start` | ProductionRecord başlangıç | Yok | Yok | State transition |
| Gerçekleşme kaydı | Operatör | Miktar, süre, fire, not | `InProgress` | `production.record` | ProductionRecord, personnel time | Ara kayıt policy'ye göre yok | Yok | Kayıt |
| Duruş/fire | Operatör/sorumlu | Neden, miktar, süre | `Paused` veya active | `production.record-downtime` | Downtime, scrap | Sağlam stok artmaz | Yok | Fire/duruş |
| Tamamlama | Üretim sorumlusu | Hedef, fire, kalite, depo | `Completed` | `production.complete` | Completion | `StockMovement(ProductionReceipt)` | Maliyet policy'ye göre | Tamamlama |

### Üretim invariants

- İş emri iptal edilmişse üretim başlatılamaz.
- Gerçekleşen miktar, izin verilen aşım kuralı olmadan hedefi aşamaz.
- Fire sağlam ürün stoğuna eklenmez.
- Üretim tamamlanması, stok girişi ve audit olmadan başarılı sayılmaz.
- Makine çakışması backend'de kontrol edilir.
- Üretim personel çalışma süresi ve rolü ana kayda bağlı tutulur.

## 4. Personel workflow'u

```text
Employee
→ Attendance
→ Overtime / Leave
→ Approval
→ Production Assignment
→ Payroll Record
```

| Geçiş | Aktör | Girdi | State | Permission | Database etkisi | Stok etkisi | Finansal etkisi | Audit |
|---|---|---|---|---|---|---|---|---|
| Personel oluşturma | İK | Kimlik, departman, pozisyon, başlangıç | Active/Inactive | `employee.create` | Employee | Yok | Maaş bilgisi saklanır | Personel kartı |
| Puantaj kaydı | İK/sistem | Giriş, çıkış, tarih | Recorded | `attendance.create` | Attendance | Yok | Çalışma süresi | Giriş/çıkış |
| Mesai talebi | Personel/sorumlu | Tarih, süre, neden | Pending | `overtime.create` | Overtime | Yok | Maaş/mesai etkisi bekler | Talep |
| Mesai onayı | Yönetici/İK | Karar, açıklama | Approved/Rejected | `overtime.approve` | Overtime state | Yok | Maaş hesabına dahil edilebilir | Onay |
| İzin talebi | Personel | Tip, tarih aralığı, açıklama | Pending | `leave.create` | LeaveRequest | Yok | Yok | Talep |
| İzin onayı | Yönetici/İK | Karar, açıklama | Approved/Rejected | `leave.approve` | Leave state | Yok | Maaş/puantaj etkisi policy'ye göre | Onay |
| Üretime atama | Üretim | Personel, iş emri, rol, vardiya | Assigned | `production.assign-personnel` | ProductionPersonnel | Yok | Çalışma süreli maliyet raporu | Atama |
| Maaş kaydı | İK/muhasebe | Dönem, brüt/net, mesai, kesinti | Draft/Approved | `salary.create`, `salary.approve` | SalaryRecord | Yok | Personel finans kaydı | Maaş dönemi |

## 5. State transition güvenliği

Her belge state'i explicit enum veya state machine ile tanımlanır. Frontend durum badge'i yalnızca kullanıcı deneyimidir; geçiş izni ve iş kuralları backend'de doğrulanmalıdır.

| Belge | İzin verilen ana geçiş | Geri dönüş |
|---|---|---|
| SalesOrder | Draft → PendingApproval → Approved/Rejected → Preparing → PartiallyShipped/Completed | İptal dışı geri dönüş yok |
| DeliveryNote | Draft → Prepared → Issued → Shipped → Invoiced | Issued sonrası reversal/cancel policy |
| Shipment | Preparing → Ready → Shipped → Delivered | Teslim sonrası düzeltme kaydı |
| Invoice | Draft → Issued → PartiallyPaid/Paid/Overdue | İptal veya credit/reversal |
| ProductionOrder | Planned → Released → InProgress/Paused → Completed | Cancelled sonrası geri dönüş yok |
| LeaveRequest | Pending → Approved/Rejected | Geri çekme yalnızca policy ile |

## 6. Audit kapsamı

Aşağıdaki geçişler audit olmadan tamamlanamaz:

- Sipariş onayı/reddi/iptali.
- Stok rezervasyonu, stok çıkışı, düzeltme ve sayım farkı.
- İrsaliye kesinleştirme ve sevkiyat teslimi.
- Fatura oluşturma/iptal ve cari hareket.
- Ödeme oluşturma ve allocation.
- Üretim tamamlama, fire, duruş ve makine değişimi.
- Personel, maaş, izin ve yetki değişiklikleri.

## 7. Workflow kabul ölçütü

Bir workflow tasarımı; actor, input, state, transition, permission, database effect, stock effect, financial effect ve audit requirement alanlarının tamamı dolu değilse Design Gate'ten geçmez.
