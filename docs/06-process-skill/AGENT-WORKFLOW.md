# Factory ERP AI Agent Workflow

Bu repository için AI agent'ın temel çalışma sırası:

```text
1. DISCOVER
   ↓
2. DESIGN
   ↓
3. DESIGN GATE
   ↓
4. ARCHITECTURE
   ↓
5. IMPLEMENT
   ↓
6. TEST
   ↓
7. SECURITY REVIEW
   ↓
8. DEPLOY / OPERATE
   ↓
9. RELEASE GATE
```

## Skill sorumlulukları

| Aşama | Skill |
|---|---|
| UX / süreç / ekran tasarımı | factory-erp-design-workflow |
| Domain / DB / API / architecture | factory-erp-architecture |
| Kodlama | factory-erp-implementation |
| QA / security / release | factory-erp-qa-security |
| Docker / backup / deployment / operations | factory-erp-operations |

## Ana kural

Bir aşamanın çıktısı bir sonraki aşamanın girdisi olmalıdır.

Tasarım → implementation-ready → kod → test → release.

Agent tasarım aşamasında business logic uydurup implementation'a gömmemeli; implementation aşamasında ise tasarımla çelişen davranış eklememelidir. Gerekli değişiklikler `decision-log.md` veya ilgili architecture dokümanında güncellenmelidir.
