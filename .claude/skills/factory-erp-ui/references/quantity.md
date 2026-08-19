# Quantity UI

Canonical sözleşme: `design/mobile-toggle-api-and-schema.md`.
UX: `design/mobile-barcode-and-quantity-ux.md`.

## Sert ayrım

```text
viewMode            ≠  operationPackagingId
görüntüleme         ≠  işlem ambalajı
QuantityViewToggle  ≠  miktar / ledger mutation
```

`QuantityViewMode` canonical değerleri:

| Kod | Türkçe |
|---|---|
| `BaseUnit` | Temel Birim |
| `Packaging` | Ambalaj |
| `Breakdown` | Kırılım |

Agent promptundaki `base | transaction | packaging` kullanılmaz. Design belgesi geçerli kaynaktır.

## QuantityViewToggle

Yalnızca `viewMode` günceller.

Değiştiremez:

- `operationPackagingId`
- transaction / entered quantity
- `quantityBase`
- stock quantity
- conversion sonucu
- submit payload

`onViewModeChange` imzası yalnızca yeni `viewMode` alır.

## QuantityEntryPreview

Backend’in canonical sonucunu gösterir. Client çarpma, bölme, yuvarlama, `quantityBase` üretmez.

Gösterilecek alanlar props olarak gelir: `displayQuantity`, `displayUnit`, `baseQuantity`, `baseUnit`, `conversionLabel`, `isLoading`, `error`.

Kritik belge/finans ekranında temel karşılık gizlenmez.

## Packaging filter

`Tümü / Palet / Koli / Paket / Temel Birim` liste filtresidir. Master katsayıyı veya `operationPackagingId` değerini değiştirmez.

## Test

Toggle öncesi/sonrası `operationQuantity`, `quantityBase`, `operationPackagingId`, `stockQuantity` eşit kalmalı. Hesap uyduran preview FAIL’dir.
