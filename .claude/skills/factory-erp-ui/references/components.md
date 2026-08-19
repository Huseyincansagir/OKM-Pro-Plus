# Ortak bileşenler

Sözleşme: `design/ui-reference/COMPONENT-IMPLEMENTATION.md`.
İlkeler: `design-principles.md`.

Ortak props (uygunsa): `className`, `disabled`, `loading`, `size`, `variant`.
Controlled ve uncontrolled karıştırma; seçilen modeli dosya başında belirt.

## Button / IconButton

- `primary` teal, `secondary` çizgili, `ghost` sakin, `danger` yıkıcı
- Loading iken disabled + görünür spinner; çift submit yok
- `IconButton` görünür metin yoksa `aria-label` zorunlu
- Desktop min-height ~37px, mobile-web ~43px

## Form kontrolleri

`Input`, `Select`, `Checkbox`: görünen `label`, `required` işareti, `hint`, `error` (alanla `aria-describedby` / `aria-invalid`).
Kullanıcı hem “hangi bilgi?” hem “bu neyi değiştirir?” sorusunu okuyabilmeli.

## Card / Divider

Kart: açık yüzey, ince sınır, hafif gölge, orta radius. Kart başına bir birincil amaç.

## Badge / StatusBadge

`Badge` nötr sayaç veya kategori.
`StatusBadge` durum içindir; metin + renk + ikon. `references/design-principles.md`.

## DataTable foundation

Props tabanlı; API çağırmaz. Zorunlu durumlar: columns, rows, loading skeleton, empty, error, sort, selection, pagination, sticky header.

Ayrıntı: `data-heavy-erp.md`.

## Dialog / Drawer

- Dialog: onay, red, kritik stok/finans teyidi
- Drawer: hızlı düzenleme, önizleme, kısa detay
- Açılışta ilk odak, Escape kapatır, kapanınca tetikleyiciye dönüş, `role="dialog"` + `aria-modal`, backdrop scroll kilidi
- Kritik dialog etki özetini içerir

## Tabs / Tooltip / DropdownMenu

Sekmeler klavye ile gezilir. Tooltip yalnızca yardımcı metin; zorunlu bilgi tooltip’e hapsolmaz. Dropdown menü öğeleri gerçek `button`/`a`.

## Feedback

`Toast`: sonuç + kayıt no + sonraki adım. Yalnızca renk yetmez.
`Alert`: sayfa içi kalıcı uyarı.
`Skeleton` / `Spinner`: loading; boş beyaz alan bırakma.

## States

`EmptyState`, `ErrorState`, `PermissionDenied` ortak bileşenlerdir. Kopya boş sayfa yazma.
`references/interaction-and-states.md`.
