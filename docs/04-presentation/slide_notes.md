# 1 - Uçtan Uca Değer Akışı

Following our functional scope, this value stream map shows how information flows seamlessly from an external customer request all the way to final payment and reporting. By connecting public catalog requests directly to draft orders, management approvals, and warehouse reservations, we eliminate manual handoff delays. On the parallel manufacturing side, work orders tie directly into machine and personnel allocation, real-time output tracking, and automatic warehouse stock replenishment. This integration ensures that sales, production, and finance always operate on the exact same reality. Let us now look at the modular architecture that powers these workflows.

# 2 - Kapsam ve Hedeflenen Operasyon

To deliver this vision effectively, we divided the operational scope across three distinct user touchpoints without breaking our unified data model. The internal web application handles heavy daily operations across management, sales, warehouse, production, accounting, and HR. Meanwhile, the public catalog lets external customers submit standardized quote requests without logging in, and the mobile application gives floor and warehouse staff direct tools for barcode scanning, stock checks, and production tracking. Every surface shares the exact same customer, product, and inventory core. Let us now examine how these surfaces connect in our end-to-end value stream.

# 3 - Modül Haritası

Building directly on our value stream, this module map outlines the core functional pillars of the system. We cover revenue and sales, physical logistics, manufacturing, finance, human resources, and management analytics as interconnected domains. Crucially, none of these modules operate in silos; they all draw from and write to the same unified database for customers, products, documents, inventory, personnel, and accounts. This eliminates data duplication and ensures full operational transparency across departments. Let us now examine the specific user roles and decision points that govern these modules.

# 4 - Proje Vizyonu

We are looking at the foundational vision for the Fabrika ERP Lite project. This platform unites production, warehouse, sales, current accounts, and human resources into a single collaborative workspace. We built this design package to eliminate fragmented tools and bring all factory operations under a common data model and role-based web and mobile experience. The design phase is now complete, and our immediate next step is establishing the shared component system and coding priority workflows. Let us move to the executive summary to review the core impacts of this architecture.

# 5 - Ürün, Public Katalog ve Teklif Sepeti

Dış müşterilerimiz için hesap açma zorunluluğunu ortadan kaldırarak süreci tamamen hızlandırıyoruz. Public katalog üzerinden ürünleri inceleyen müşterilerimiz, sepet aracılığıyla kolayca teklif talebinde bulunabiliyor. İki aşamalı form yapısıyla toplanan bu talepler, arka planda doğrudan yeni durum etiketiyle iç sisteme aktarılıyor. Böylece satış ekibimiz dışarıdan gelen taleplere anında müdahale etme şansı yakalıyor. Bu talep akışının içeride üretim ve depo operasyonlarına nasıl yansıdığını görelim.

# 6 - Kullanıcı Rolleri ve Karar Noktaları

Güvenlik ve veri bütünlüğü için rol ve işlem bazlı yetkilendirme kritik önem taşıyor. Sistemde kullanıcıların sadece rolleri değil, gerçekleştirdikleri kritik işlemlerin izinleri de ayrı ayrı tanımlanıyor. Bu yaklaşım sayesinde sipariş onayından stok düzeltmeye kadar tüm kritik operasyonlar denetim altında tutuluyor. Şimdi, bu operasyonel yapının arkasındaki ortak UX ve görsel tasarım standartlarına bakalım.

# 7 - Ortak UX ve Görsel Tasarım Sistemi

Arayüz tasarımında amacımız asla dekoratif bir görünüm elde etmek değil. Bilgi yoğun, hızlı taranabilir ve departmanlar arası tutarlı bir ekran deneyimi yaratıyoruz. Derin lacivert ve teal tonlarıyla odaklanmayı sağlarken, amber ve kırmızı durum renkleriyle riskleri anında görünür kılıyoruz. Her detay ekranında aynı zihinsel modeli koruyarak kullanıcıların hata yapma payını en aza indiriyoruz. Bu görsel standartların web satış süreçlerinde nasıl hayat bulduğunu inceleyelim.

# 8 - Web Satış ve Onay Deneyimi

Satış operasyonlarında hız kadar kontrol de bizim için esastır. Teklif aşamasından kesin siparişe uzanan süreçte, siparişler doğrudan işleme alınmıyor ve görünür bir onay panelinden geçiriliyor. Stok uygunluğu, ödeme şartları ve risk etkenleri tek ekranda değerlendirilerek onay veya ret kararları tam bir izlenebilirlikle kaydediliyor. Bu akış, finansal sürprizleri ve hatalı sevkiyatları kökten engelliyor. Şimdi bu sürecin dış müşteri tarafındaki ilk adımı olan public kataloğa geçelim.

# 9 - Yönetici Özeti

Building on our project vision, this executive summary highlights why we structured the interface the way we did. We focused on data accuracy, transaction traceability, secure approvals, and end-to-end document linkage rather than superficial visuals. By moving away from fragmented departmental spreadsheets, we are giving every team the exact operational data they need through a unified information architecture. This common language directly reduces departmental friction and speeds up daily tasks. Next, let us break down the exact operational scope across our user touchpoints.

# 10 - Üretim, Depo ve Mobil Operasyon

Üretim ve saha operasyonlarında en kritik hedefimiz veri girişini hızlandırmak ve hatasız hale getirmektir. Üretim iş emri ekranında hedef, gerçekleşen ve fire oranları anlık olarak takip edilirken, süreç bittiğinde miktar doğrudan depoya işlenir. Mobil uygulamayı masaüstünün basit bir kopyası olarak tasarlamadık. Saha çalışanları için barkod tarama ve stok sayımı gibi işlemleri odak noktası yaptık. Ağ kesintilerinde ise veri kaybını önlemek için kullanıcıyı açıkça uyaran güvenli bir mekanizma kurduk. Şimdi gelin, bu fiziksel ve üretim süreçlerinin finans ve insan kaynakları tarafına nasıl yansıdığına bakalım.

# 11 - Sevkiyat, Cari ve Personel Görünümü

Operasyonel süreçlerin mali karşılığını ve insan kaynağını tek ekrandan yönetebilmek büyük bir esneklik sağlar. Sevkiyat ekranında irsaliye, araç ve yükleme detayları şeffaf bir şekilde izlenirken, cari ekranda bakiye ve geciken tutarlar anında görülür. Ödeme kaydı alırken sistemin yeni bakiyeyi önceden göstermesi muhasebe hatalarının önüne geçer. Benzer şekilde, personel ekranında puantaj ve izinler tek bir dashboard altında toplanarak İK süreçlerini hızlandırır. Bu bütünleşik yapıyı hayata geçirmek için izleyeceğimiz adımları ve vermemiz gereken kararları inceleyelim.

# 12 - Yol Haritası ve Proje Yönetimi Kararları

Önümüzdeki dönemi net bir sırayla planladık ve geliştirme yol haritasını dört ana faza ayırdık. Altyapı ve kimlik yönetiminden başlayarak satış, depo, üretim ve son olarak raporlama ile mobil uygulamaya geçeceğiz. Bu süreçte marka renkleri, onay kademeleri ve e-belge entegrasyonu gibi kritik başlıkları netleştirmemiz gerekiyor. En kısa sürede tasarım paketini geliştirme backlog'una dönüştürerek ilk uçtan uca satış akışını kodlamaya başlamalıyız. Bu projeyi başarıyla tamamlamak için kararlarımızı hızla alıp sprint planlamamızı hayata geçireceğiz.
