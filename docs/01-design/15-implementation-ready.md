# Factory ERP — Implementation Ready Gate

Bu dosya, `factory-erp-design-workflow` skill'inin implementation başlangıç işaretidir.

```text
IMPLEMENTATION:
NOT READY
```

`implementation-readiness.md` içindeki Design Gate tablosu kapsamın tasarım açısından büyük ölçüde tamamlandığını, ancak schema ve domain davranışını etkileyen `OPEN DECISION` maddeleri bulunduğunu gösterir. Bu nedenle bu dosya Architecture aşaması tamamlanana kadar `NOT READY` olarak kalmalıdır.

Implementation'a geçmeden önce aşağıdaki artefact'lar aynı commit veya izlenebilir karar seti içinde güncellenmelidir:

- `decision-log.md`: Açık kararlar çözülmüş olmalı.
- `domain-model.md`: Entity, bounded context ve source of truth güncel olmalı.
- `business-workflows.md`: State transition ve effect'ler seçilmiş kurallarla uyumlu olmalı.
- `docs/02-architecture/01-database-technical-architecture.md`: Seçilen vergi, partial shipment/invoice, BOM, lot ve bordro kapsamını yansıtmalı.
- `master-screen-inventory.md`: Yeni state, alan ve permission değişiklikleriyle uyumlu olmalı.
- `mobile-design.md` ve `public-catalog-design.md`: Kritik operasyon ve public erişim kararlarını yansıtmalı.

Bu dosya `READY` yapılmadan `factory-erp-implementation` skill'i ile business feature üretme.
