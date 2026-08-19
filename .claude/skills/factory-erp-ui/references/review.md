# UI review

Her önemli UI slice sonrası çalıştır. Kod yazma; kanıtlı bulgu üret.

## Eksenler

VISUAL · UX · ACCESSIBILITY · RESPONSIVE · DATA INTEGRITY · CONSISTENCY

## Karar

| Overall | Koşul |
|---|---|
| PASS | BLOCKER / CRITICAL / MAJOR yok |
| PASS WITH ISSUES | CRITICAL/BLOCKER yok; MINOR veya sınırlı MAJOR |
| BLOCKED | Güvenilir inceleme yapılamıyor |

CRITICAL data-integrity veya security varsa PASS yok.
`QuantityViewToggle` quantity / `quantityBase` / `operationPackagingId` değiştiriyorsa CRITICAL + FAIL.

Madde statüsü: PASS / PARTIAL / FAIL / BLOCKED.

Severity: BLOCKER, CRITICAL, MAJOR, MINOR, INFO.

## Issue alanları

- ID, Severity, Status, Area
- Route / screen, File, Component
- Evidence, Expected, Actual
- Recommendation, Impact, Verification

Gözlemlenmemiş şeyi doğru yazma. Çalıştırılamayan kontrol `BLOCKED`.

## NEXT ACTION

`PASS → proceed` · `ISSUES → fix before next slice` · `BLOCKED → stop`
