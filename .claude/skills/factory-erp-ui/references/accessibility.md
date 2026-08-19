# Accessibility

Sonradan eklenmez. Sertifika iddiası yok; aşağıdaki minimumlar zorunlu.

## Klavye

- Tüm aksiyon Tab / Shift+Tab ile ulaşılır
- Görünür `:focus-visible` (teal halka, mevcut global kural)
- Dialog/Drawer: Tab tuzağı, Escape kapatır, kapanınca tetikleyiciye focus dönüşü
- Radiogroup (quantity toggle) ok tuşlarıyla gezilir

## Semantik

- Gerçek `button`, `nav`, `main`, `table`, `label`
- İkon-only kontrolde `aria-label`
- Dialog: `role="dialog"`, `aria-modal="true"`, `aria-labelledby` veya `aria-label`
- Hata: `aria-invalid` + `aria-describedby`
- Toast/notification: uygun `aria-live` (kritik olmayan `polite`)

## Kontrast ve disabled

Metin/zemin `design/visual-design-system.md` açık yüzey + lacivert/teal kombinasyonunu bozma.
Disabled tıklanamaz ve nedeni okunur; yalnızca soluk renk yetmez.

## Status

Renk körlüğünde okunabilir: etiket + ikon şart. `design-principles.md`.
