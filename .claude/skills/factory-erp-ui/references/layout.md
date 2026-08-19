# Layout ve responsive

Canonical bilgi mimarisi: `design/web-ux-architecture.md`.
Ölçü sözleşmesi: `design/ui-reference/COMPONENT-IMPLEMENTATION.md`.

## App shell

İç web:

- Sol navigasyon: derin lacivert, ~248px, ikon + Türkçe metin
- Aktif öğe teal zemin
- Bekleyen iş sayısı rozeti (sayı, yalnızca renk değil)
- Topbar ~73px: arama, bağlam (depo), bildirim, kullanıcı
- İçerik açık yüzey; yatay gutter ~31px

Public katalog ayrı shell kullanır (sidebar yok). Native Flutter ayrı mimaridir.

## Sayfa iskeleti

Liste:

```text
Breadcrumb
Başlık + açıklama + primary action
İsteğe bağlı KPI
Arama + filtre + dışa aktarma
DataTable
Sayfalama
```

Detay:

```text
Belge no + StatusBadge + primary action
Özet kartları
Sekmeler
Sağda sonraki adım / geçmiş
Kritik aksiyonlar görünür ve onaylı
```

## Navigasyon

Menü bölümleri `design/web-ux-architecture.md` §1 dışına çıkmaz. Yeni modül adı uydurma. Yetkisiz bölümü gizle veya `PermissionDenied` göster; sahte dolu ekran gösterme.

## Responsive

Kontrol kırılımları: 320, 768, 1024, 1280+.

| Genişlik | Navigasyon | İçerik |
|---|---|---|
| ≥1024 | Expanded sidebar | Yoğun tablo, çok kolon |
| 768–1023 | Collapsible sidebar | Tablo yatay scroll veya öncelikli kolonlar |
| <768 | Drawer sidebar + compact header | Kart/stack, form tek kolon, touch hedef ≥43px |

Desktop-first ERP kabul; responsive zorunlu. Tabloyu orantılı küçülterek okunamaz hale getirme.

`prefers-reduced-motion` varsa zorunlu olmayan geçişleri kapat.
