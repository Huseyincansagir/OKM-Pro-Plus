# Factory ERP Agent Instructions

Bu repository'de substantial görevler aşağıdaki sırayla yürütülür:

```text
DISCOVER → DESIGN → DESIGN GATE → ARCHITECTURE → IMPLEMENTATION → TEST → SECURITY REVIEW → OPERATIONS / DEPLOYMENT → RELEASE GATE
```

## Canonical kaynaklar

- Design ve Design Gate: `/design/`
- Runtime skill paketi: `/.claude/skills/`
- Dokümantasyon, sunum ve asset arşivi: `/docs/`
- Skill dokümantasyon kopyası: `/docs/06-process-skill/`

Aynı domain veya ekran iki yerde bulunuyorsa `/design` canonical kabul edilir. Numaralı docs kopyaları canonical değişiklikten sonra senkronize edilmelidir.

## Implementation kuralı

`/design/implementation-ready.md` `READY` durumuna geçmeden production business feature, migration, API endpoint veya frontend/mobile implementation başlatma. Design Gate başarısızsa önce `/design/decision-log.md` içindeki açık kararları çöz ve etkilenen artefact'ları güncelle.

## İş bütünlüğü

Ürün, müşteri, stok, belge ve cari hareketleri için duplicate source of truth oluşturma. Stok hareketlerini `StockMovement` ile izle; finansal ve stok kayıtlarında fiziksel silme yerine iptal/ters kayıt kullan; kritik state transition'ları transaction ve audit ile koru. Yetkilendirmeyi yalnızca UI'da bırakma.

## UX ve dil

Kullanıcı arayüzü Türkçe, entity/property/API isimleri İngilizce olmalıdır. Web yoğun operasyon tablolarına, mobil barkod/stok/sevkiyat/üretim görevlerine, public katalog ise iç maliyet/risk/operasyon bilgilerinin gizlenmesine göre tasarlanır.
