# Önerilen Proje Klasör Yapısı

```text
factory-erp/
│
├── .claude/
│   └── skills/
│       ├── factory-erp-design-workflow/
│       │   └── SKILL.md
│       ├── factory-erp-architecture/
│       │   └── SKILL.md
│       ├── factory-erp-implementation/
│       │   └── SKILL.md
│       ├── factory-erp-qa-security/
│       │   └── SKILL.md
│       └── factory-erp-operations/
│           └── SKILL.md
│
├── apps/
│   ├── api/
│   ├── web/
│   └── mobile/
│
├── packages/
│   ├── shared-types/
│   ├── shared-config/
│   └── ui/                     # gerekiyorsa shared web components
│
├── database/
│   ├── migrations/
│   ├── seed/
│   └── scripts/
│
├── infrastructure/
│   ├── docker/
│   ├── nginx/
│   └── deployment/
│
├── docs/
│   ├── 00-project-brief/
│   ├── 01-design/             # UX, workflow, visual system, gate
│   ├── 02-architecture/       # domain and database source of truth
│   ├── 03-production-warehouse/
│   ├── 04-presentation/        # project slides and speaker notes
│   ├── 05-assets/              # mockups and other design assets
│   └── 06-process-skill/        # skills, rules and process notes
│
├── tests/
│   ├── unit/
│   ├── integration/
│   └── e2e/
│
├── scripts/
├── uploads/                    # runtime storage; git'e girmez
├── docker-compose.yml
├── .env.example
├── README.md
└── AGENTS.md                   # root agent instructions
```

## Skill klasörleri nasıl çalışır?

Her skill kendi uzmanlık alanında karar üretir. `AGENTS.md` veya kullanılan coding-agent platformunun ana talimat dosyası, agent'a skill'lerin hangi aşamada kullanılacağını söyler.

Önerilen sıra:

```text
factory-erp-design-workflow
        ↓
factory-erp-architecture
        ↓
factory-erp-implementation
        ↓
factory-erp-qa-security
        ↓
factory-erp-operations
```

## Root AGENTS.md önerisi

Root'ta `AGENTS.md` oluştur ve şu mantığı kullan:

1. Önce repository'yi ve kanonik `docs/00`–`docs/06` artefact'larını oku.
2. Yeni business feature'da önce `docs/01-design` ve `docs/02-architecture` etkisini değerlendir.
3. `docs/01-design/15-implementation-ready.md` yoksa büyük feature'a doğrudan başlama.
4. Kod değişikliğinden sonra test ve security skill'lerini çalıştır.
5. Deployment veya infrastructure değişikliğinde operations skill'ini kullan.
6. Tasarımla implementation çelişirse ilgili kanonik dokümanı ve `docs/01-design/13-decision-log.md`yi güncelle.

## Git'te tutulmaması gerekenler

```text
.env
.env.production
uploads/*
local database files
logs/*
secrets/*
build artifacts
node_modules/
bin/
obj/
.dart_tool/
```
