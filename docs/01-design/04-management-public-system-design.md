# Yönetim, Yetkilendirme, Bildirim, Audit, Ayarlar ve Public Alan Tasarımı

## 1. Yönetim modülü

Yönetim ekranları sistemin güvenlik ve işletim merkezidir. Bu bölüme yalnızca yönetici ve sistem yöneticisi rolleri erişir. Finansal veya stok etkisi olan yetkiler ayrı ayrı verilir; kullanıcıya yalnızca rol adını göstermek yerine izin kapsamı anlaşılır şekilde sunulur.

### Kullanıcı listesi

Tabloda ad soyad, kullanıcı adı, e-posta, roller, aktiflik, son giriş, son parola değişimi ve oturum durumu gösterilir. Filtreler rol, durum, departman ve son giriş tarihidir. Satır işlemleri kullanıcı detayı, geçici pasifleştirme, oturumları kapatma ve parola sıfırlamadır.

### Kullanıcı detayı

Sekmeler; Genel Bilgiler, Roller, İzinler, İzin İstisnaları, Oturumlar ve Audit Geçmişidir. Kullanıcı bazlı override yetkisi veriliyorsa ekranda rolün getirdiği izin ile kullanıcıya özel izin ayrı sütunlarda gösterilir. Böylece yöneticinin bir kullanıcının neden bir işlemi yapabildiğini anlaması sağlanır.

### Rol ve yetki ekranı

Rol detayı modül bazlı permission matrisi kullanır. Her satırda modül ve işlem, sütunlarda görüntüle, oluştur, düzenle, sil, onayla, iptal et izinleri bulunur. Tehlikeli izinlerde kısa açıklama ve “Bu yetki stok/cari/fatura üzerinde kalıcı etki oluşturur” uyarısı gösterilir.

## 2. Bildirim merkezi

Bildirim merkezi iki görünüm içerir: tüm bildirimler ve işlemi bekleyenler. Her bildirim olay, ilgili belge, zaman, önem seviyesi ve doğrudan işlem bağlantısını taşır.

| Bildirim | Öncelik | Aksiyon |
|---|---|---|
| Yeni teklif talebi geldi | Normal | Teklif talebi detayına git |
| Sipariş onayı bekliyor | Yüksek | Onay panelini aç |
| Kritik stok | Yüksek | Ürün/stok detayına git |
| Ödeme gecikti | Yüksek | Cari ekstreyi aç |
| Faturalaşmamış irsaliye | Yüksek | İrsaliye ve fatura ekranını aç |
| İzin onayı bekliyor | Normal | İzin talebini aç |
| Sevkiyat hazır | Normal | Sevkiyat detayını aç |
| Backup başarısız | Kritik | Yedekleme durumunu aç |

Okunmamış bildirim rozetle gösterilir. Bildirime tıklamak hem okunma durumunu değiştirir hem de ilgili kaydı açar. Kritik bildirimler yönetici dashboard'unda ayrıca görev kartı olarak görünür.

## 3. Audit log tasarımı

Audit log yalnızca teknik kayıt tablosu olmayacak, yöneticinin işlem geçmişini anlayabileceği bir inceleme ekranı olacaktır. Liste kolonları tarih/saat, kullanıcı, işlem, modül, kayıt, IP, sonuç ve önem seviyesidir.

Audit detayında kim, ne yaptı, hangi kayıt üzerinde yaptı, eski değer, yeni değer, işlem sonucu ve ilişkili belge bağlantısı gösterilir. Özellikle sipariş onayı, sipariş iptali, fatura oluşturma/iptali, cari hareket, ödeme, stok düzeltmesi, personel değişikliği ve yetki değişiklikleri vurgulanır.

Eski ve yeni değerler yan yana veya değişen alanlar işaretlenmiş biçimde gösterilir. Audit kaydı silinemez; filtreleme ve dışa aktarma yapılabilir.

## 4. Sistem ayarları

Ayarlar bölümü sekmeli bir yapıda tasarlanacaktır:

| Sekme | Alanlar |
|---|---|
| Şirket | Unvan, logo, adres, telefon, e-posta, vergi bilgileri |
| Belge numaraları | SIP, IRS, FAT, TEK, URE, SEV sıra yapısı |
| Para ve tarih | TRY, tarih formatı, timezone, çalışma takvimi |
| Vergi | Vergi oranları, varsayılan değer, yuvarlama |
| Bildirim | Sistem bildirimleri, alıcı roller, kritik eşikler |
| Dosyalar | Maksimum boyut, izin verilen tipler, klasör bilgisi |
| Risk | Risk eşikleri, gecikme günleri, puan ağırlıkları |
| Depolar | Varsayılan depo, rezervasyon davranışı, konum kullanımı |
| Yedekleme | Günlük zaman, retention, son durum, manuel yedek |

Risk ayarlarında skor algoritması sabit kod davranışı gibi görünmeyecek; yöneticinin eşikleri ve faktörleri açıklanabilir biçimde düzenleyebileceği bir yapı bulunacaktır.

## 5. Yedekleme ve sistem sağlık ekranı

Yedekleme ekranında son başarılı yedek, son başarısız deneme, yedek boyutu, saklama süresi ve planlanan sonraki çalışma gösterilir. “Şimdi yedekle” butonu onay penceresiyle çalışır. Başarısız yedek durumunda hata açıklaması ve yönetici bildirimi bulunur.

Sistem sağlık ekranında API, database, dosya depolama, bildirim servisi ve background jobs kartları yer alır. Her kart kullanılabilir, gecikmeli, uyarı veya çalışmıyor durumundadır. Teknik log kullanıcıya doğrudan gösterilmez; yöneticinin destek ekibine iletebileceği özet bilgi sunulur.

## 6. Public alan tasarımı

Public ürün kataloğu şirket içi ERP menüsünden ayrı bir müşteri yüzeyidir. Üst navigasyonda şirket logosu, ürünler, kurumsal, iletişim ve teklif sepeti bulunur. Ürünler; fotoğraf, ürün adı, kod, ölçü, paket/koli bilgisi, açıklama ve “Teklife Ekle” butonu ile gösterilir.

Public akışın ekranları şunlardır:

```text
Ana sayfa
→ Ürün kataloğu
→ Ürün detayı
→ Teklif sepeti
→ Firma bilgileri
→ Talep özeti
→ Başarılı gönderim
```

Public kullanıcıya stok, maliyet, şirket içi fiyat, depo veya risk bilgisi gösterilmez. Teklif talebi gönderildiğinde şirket içi sistemde `NEW` durumlu kayıt, satış ekibine bildirim ve talep numarası oluşturulur.

## 7. Güven, gizlilik ve hata ekranları

Public formda telefon ve e-posta doğrulaması yapılır. Form gönderiminde rate limit ve gerektiğinde CAPTCHA gibi kötüye kullanım önlemleri devreye alınabilir. Kullanıcıdan ödeme bilgisi veya parola istenmez.

Hata halinde form verileri korunur. Boş katalogda ürünleri görmeye yönlendiren CTA; boş sepette “Kataloğa Git”; erişim engelinde “Bu alana erişim yetkiniz bulunmuyor” mesajı gösterilir. Teknik exception kullanıcıya gösterilmez.

## 8. Yönetim tasarımının kabul senaryosu

```text
Yönetici giriş yapar
→ Kullanıcılar ekranını açar
→ Bir kullanıcıya Satış rolü verir
→ order.approve izninin kapalı olduğunu görür
→ Kullanıcıya özel override tanımlar veya reddeder
→ Audit log'da yetki değişikliğini görür
→ Kullanıcının aktif oturumlarını kapatır
→ Sistem sağlık ve backup durumunu kontrol eder
```

Bu akış, yetki, güvenlik, audit ve işletim ekranlarının birbirinden kopuk değil, aynı yönetim bağlamında tasarlandığını doğrular.
