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
├── design/
│   ├── master-screen-inventory.md
│   ├── web-ux-architecture.md
│   ├── production-warehouse-deep-dive.md
│   ├── database-technical-architecture.md
│   ├── mobile-design.md
│   ├── public-catalog-design.md
│   ├── visual-design-system.md
│   ├── decision-log.md
│   └── implementation-ready.md
│
├── presentation/
│   ├── project-management-slides.md
│   └── slide_notes.md
│
├── docs/
│   ├── architecture/
│   ├── api/
│   ├── operations/
│   ├── security/
│   └── user-guides/
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

1. Önce repository'yi ve `/design` artefact'larını oku.
2. Yeni business feature'da önce design/architecture etkisini değerlendir.
3. `implementation-ready.md` yoksa büyük feature'a doğrudan başlama.
4. Kod değişikliğinden sonra test ve security skill'lerini çalıştır.
5. Deployment veya infrastructure değişikliğinde operations skill'ini kullan.
6. Tasarımla implementation çelişirse tasarımı ve `decision-log.md`yi güncelle.

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
