# Factory ERP — MVP GitHub Actions CI/CD Planı

**Aşama:** ARCHITECTURE

**Durum:** CI/CD workflow ve release otomasyon tasarımı; aktif production workflow dosyası değildir.

**Baseline:** O-001–O-014 kabul edilmiş; Ubuntu LTS + Docker Compose + PostgreSQL + LAN HTTPS deployment; implementation henüz başlamamıştır.

## 1. Amaç ve sınır

MVP pipeline, her değişikliğin build edilebildiğini, domain kurallarını geçirdiğini, PostgreSQL migration’larının temiz database’de çalıştığını, API contract’larının bozulmadığını ve güvenlik kapılarının geçildiğini doğrular. Production deployment otomatik ve koşulsuz yapılmaz.

Şirket içi server’a deployment, GitHub-hosted runner’ın doğrudan LAN’a açılmasıyla değil, proje sahibi onaylı `self-hosted` runner veya iç ağdan kontrollü pull/release script’i ile yapılır. Production secret’ları GitHub repository’ye yazılmaz; deployment host’ta root-readable olmayan `.env.production` veya host secret store’da tutulur.

## 2. Pipeline akışları

```text
Pull Request
  → format/lint/static analysis
  → backend build
  → domain/application unit tests
  → PostgreSQL integration tests
  → migration 0001–0018 validation
  → API contract/security tests
  → frontend/mobile checks
  → artifact + coverage report

Main branch
  → all PR checks
  → Docker build
  → image vulnerability/SBOM check
  → GHCR push veya internal registry publish
  → release candidate metadata

Version tag / manual dispatch
  → production environment approval
  → backup freshness check
  → controlled migration job
  → Compose deployment
  → health/ready/login/public/mobile smoke
  → release evidence
```

## 3. Workflow dosyaları

Implementation repository’sine solution scaffold’u eklendiğinde aşağıdaki dosyalar oluşturulacaktır:

```text
.github/
  CODEOWNERS
  dependabot.yml
  workflows/
    pull-request.yml
    main.yml
    release.yml
    security.yml
    backup-restore-schedule.yml
```

Bu repository’de henüz `.sln`, `.csproj`, `compose.yaml` veya production source tree bulunmadığı için aktif workflow dosyaları bu aşamada çalıştırılabilir şekilde eklenmemiştir. Aşağıdaki YAML’ler Architecture contract’ıdır; gerçek path ve project adları solution scaffold’u oluşturulurken sabitlenecektir.

## 4. Pull Request workflow taslağı

```yaml
name: pull-request

on:
  pull_request:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  checks: write
  pull-requests: write

concurrency:
  group: pr-${{ github.event.pull_request.number || github.ref }}
  cancel-in-progress: true

env:
  DOTNET_VERSION: '8.0.x'
  CONFIGURATION: Release

jobs:
  repository-guard:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Reject secrets and generated artifacts
        shell: bash
        run: |
          ! grep -RInE '(BEGIN PRIVATE KEY|POSTGRES_PASSWORD=|JWT_SIGNING_KEY=|refresh_token|password_hash)' \
            --exclude-dir=.git --exclude='*.md' .
      - name: Check markdown whitespace
        shell: bash
        run: git diff --check

  backend-unit:
    needs: repository-guard
    if: ${{ hashFiles('**/*.sln', '**/*.csproj') != '' }}
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - name: Restore
        run: dotnet restore
      - name: Format check
        run: dotnet format --verify-no-changes --no-restore
      - name: Build
        run: dotnet build --no-restore --configuration ${{ env.CONFIGURATION }}
      - name: Unit tests
        run: >-
          dotnet test tests/FactoryErp.UnitTests/FactoryErp.UnitTests.csproj
          --no-build --configuration ${{ env.CONFIGURATION }}
          --logger "trx;LogFileName=unit.trx"
          --collect:"XPlat Code Coverage"
      - name: Upload unit test evidence
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: unit-test-evidence-${{ github.sha }}
          path: |
            **/*.trx
            **/TestResults/**/coverage.cobertura.xml

  postgres-integration:
    needs: repository-guard
    if: ${{ hashFiles('**/*.sln', '**/*.csproj') != '' }}
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_DB: factory_erp_test
          POSTGRES_USER: factory_erp_test
          POSTGRES_PASSWORD: factory_erp_test_only
        ports: ['5432:5432']
        options: >-
          --health-cmd "pg_isready -U factory_erp_test -d factory_erp_test"
          --health-interval 5s --health-timeout 5s --health-retries 20
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - run: dotnet restore
      - name: Apply migrations to clean PostgreSQL
        env:
          ConnectionStrings__Default: Host=localhost;Port=5432;Database=factory_erp_test;Username=factory_erp_test;Password=factory_erp_test_only
        run: dotnet run --project src/FactoryErp.Migrator -- --validate-only
      - name: Integration tests
        env:
          ConnectionStrings__Default: Host=localhost;Port=5432;Database=factory_erp_test;Username=factory_erp_test;Password=factory_erp_test_only
        run: >-
          dotnet test tests/FactoryErp.IntegrationTests/FactoryErp.IntegrationTests.csproj
          --logger "trx;LogFileName=integration.trx"
      - name: Upload integration evidence
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: integration-test-evidence-${{ github.sha }}
          path: |
            **/*.trx
            artifacts/schema-snapshot/**

  api-contract-security:
    needs: repository-guard
    if: ${{ hashFiles('**/*.sln', '**/*.csproj') != '' }}
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - run: dotnet restore
      - name: API contract tests
        run: dotnet test tests/FactoryErp.ApiContractTests/FactoryErp.ApiContractTests.csproj --configuration Release
      - name: Security and authorization tests
        run: dotnet test tests/FactoryErp.SecurityTests/FactoryErp.SecurityTests.csproj --configuration Release
```

`if: hashFiles(...) != ''` yalnızca transition döneminde kullanılır. Solution scaffold’u repository’ye geldiğinde job’ların bu koşulu kaldırılır ve gerçek project path’leri zorunlu hale getirilir; boş repository’nin yanlışlıkla başarılı CI göstermesi istenmez.

## 5. Main workflow ve Docker image taslağı

```yaml
name: main

on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  packages: write
  security-events: write

jobs:
  checks:
    uses: ./.github/workflows/pull-request.yml

  image:
    needs: checks
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Set image metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ghcr.io/${{ github.repository }}/api
          tags: |
            type=sha
            type=raw,value=main
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/build-push-action@v6
        with:
          context: .
          file: deploy/api.Dockerfile
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
      - name: Record immutable image digest
        run: docker buildx imagetools inspect ghcr.io/${{ github.repository }}/api:main
```

Web ve worker image’ları aynı pattern ile ayrı image olarak publish edilir. `latest` production release tag’i olarak kullanılmaz; Git SHA veya semver tag immutable referans olur. Image publish job’ı yalnızca test job’ları yeşil ise çalışır.

## 6. Release workflow ve şirket içi deployment

Production environment `factory-erp-production` için required reviewers ve branch/tag restriction uygulanır. Release workflow otomatik olarak production’a geçmez; `v*` tag’i veya `workflow_dispatch` ve yönetim onayı gerekir.

```yaml
name: release

on:
  workflow_dispatch:
    inputs:
      image_tag:
        description: Immutable Git SHA or release tag
        required: true
        type: string

permissions:
  contents: read
  packages: read

env:
  RELEASE_TAG: ${{ inputs.image_tag }}

jobs:
  preflight:
    environment: factory-erp-production
    runs-on: [self-hosted, linux, factory-erp-deploy]
    steps:
      - uses: actions/checkout@v4
      - name: Verify host tooling
        run: |
          docker version
          docker compose version
          pg_dump --version
      - name: Verify recent backup
        run: ./deploy/scripts/check-backup-freshness.sh
      - name: Verify target images
        run: ./deploy/scripts/check-image-digest.sh "${RELEASE_TAG}"

  migrate:
    needs: preflight
    environment: factory-erp-production
    runs-on: [self-hosted, linux, factory-erp-deploy]
    steps:
      - uses: actions/checkout@v4
      - name: Run controlled migration job
        run: |
          docker compose --env-file /etc/factory-erp/.env.production \
            run --rm migrator --validate-and-apply
      - name: Verify schema and seed version
        run: ./deploy/scripts/check-schema-version.sh

  deploy:
    needs: migrate
    environment: factory-erp-production
    runs-on: [self-hosted, linux, factory-erp-deploy]
    steps:
      - uses: actions/checkout@v4
      - name: Deploy pinned Compose images
        run: |
          RELEASE_TAG="${RELEASE_TAG}" \
          docker compose --env-file /etc/factory-erp/.env.production \
            up -d --remove-orphans
      - name: Health checks
        run: ./deploy/scripts/health-and-smoke.sh
      - name: Upload deployment evidence
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: deployment-evidence-${{ github.run_id }}
          path: deploy/evidence/
```

Migration API startup’ında otomatik çalışmaz. `migrator` job’ı backup freshness, schema version ve operator approval sonrası çalışır. Migration başarısızsa `deploy` job’ı çalışmaz.

## 7. Security workflow taslağı

```yaml
name: security

on:
  schedule:
    - cron: '17 2 * * 1'
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  security-events: write

jobs:
  codeql:
    if: ${{ hashFiles('**/*.sln', '**/*.csproj') != '' }}
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: github/codeql-action/init@v3
        with:
          languages: csharp, javascript-typescript
      - uses: github/codeql-action/autobuild@v3
      - uses: github/codeql-action/analyze@v3

  dependency-review:
    if: github.event_name == 'pull_request'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/dependency-review-action@v4
```

Container vulnerability scanner ve SBOM üretimi image publish sonrasında yapılır. Critical vulnerability, leaked secret veya unsigned/untraceable image bulunursa release gate kırmızı kalır. Scanner seçimi implementation başında repository policy ile sabitlenir; sonuçları yok sayan `continue-on-error` kullanılmaz.

## 8. Backup/restore scheduled workflow

Backup ve restore işlemi GitHub-hosted runner’da yapılmaz. Scheduled workflow, şirket içi self-hosted backup runner’a yalnızca kontrollü script tetikleme görevi verir.

```yaml
name: backup-restore-schedule

on:
  schedule:
    - cron: '23 1 * * *'
  workflow_dispatch:

permissions:
  contents: read

jobs:
  backup:
    runs-on: [self-hosted, linux, factory-erp-backup]
    steps:
      - uses: actions/checkout@v4
      - run: ./deploy/scripts/run-backup-and-checksum.sh
      - run: ./deploy/scripts/check-backup-retention.sh

  monthly-restore:
    if: github.event_name == 'workflow_dispatch' || startsWith(github.event.schedule, '0 3')
    runs-on: [self-hosted, linux, factory-erp-backup]
    steps:
      - uses: actions/checkout@v4
      - run: ./deploy/scripts/restore-to-isolated-db.sh
      - run: ./deploy/scripts/run-restore-acceptance.sh
```

Gerçek backup target credential’ı runner host secret store’da kalır. Workflow log’unda database password, connection string ve restored data gösterilmez.

## 9. Branch protection ve repository policy

`main` branch için aşağıdaki korumalar uygulanır:

| Policy | MVP kuralı |
|---|---|
| Direct push | Yasak; emergency bypass yalnızca owner audit’i ile |
| Pull request | En az bir reviewer; kritik domain değişikliğinde ilgili CODEOWNER |
| Required checks | Unit, integration, migration, API/security ve repository guard |
| Linear history | Squash merge veya yönetimce seçilen tek yöntem |
| Signed release | Tag/image digest release evidence ile eşleşir |
| Secret scanning | GitHub secret scanning/push protection aktif |
| CODEOWNERS | `src/Domain`, `src/Application`, `deploy`, `.github/workflows` ayrı review sahipleri |
| Dependency updates | Dependabot PR; production dependency otomatik merge edilmez |

## 10. CI/CD kalite kapıları

```text
PR Gate
  = repository guard
  + build/format
  + unit
  + PostgreSQL integration
  + migration 0001–0018
  + API contract/security

Main Gate
  = PR Gate
  + Docker image build
  + SBOM/vulnerability check
  + immutable image metadata

Release Gate
  = Main Gate
  + production reviewer approval
  + recent backup
  + controlled migration
  + health/ready
  + login/public/mobile smoke
  + deployment evidence
```

MVP için “green pipeline” production implementation veya release garantisi değildir. Release evidence; test sonuçları, migration version, image digest, backup freshness, smoke sonuçları ve operator approval ile birlikte saklanır.

## 11. Rollback ve failure davranışı

- Unit/integration/security gate kırılırsa image publish edilmez.
- Image build geçip migration başarısız olursa deployment yapılmaz.
- Deployment sonrası health fail olursa previous compatible image’e dönülebilir; schema destructive ise otomatik database rollback yapılmaz.
- Veri bozulması veya RPO/RTO ihlali varsa operator backup restore/runbook başlatır.
- Forward-fix migration, destructive `Down` migration’dan önce tercih edilir.
- Self-hosted runner offline ise release başarısız görünür; GitHub-hosted runner’dan internal LAN’a alternatif gizli tünel açılmaz.

## 12. Architecture acceptance checklist

- PR, main, release, security ve backup workflow sınırları yazılıdır.
- GitHub-hosted ve company self-hosted runner görevleri ayrılmıştır.
- Production secret’ları repository/GitHub log’larına girmemektedir.
- Migration API startup’ında otomatik çalışmamaktadır.
- Backup freshness release öncesi kontrol edilmektedir.
- `0001–0018` migration validation CI’da temiz PostgreSQL ile çalışmaktadır.
- Unit, integration, API contract, security, concurrency ve smoke test gate’leri MVP test stratejisiyle eşleşmektedir.
- Docker image tag’i immutable Git SHA veya release tag’idir.
- Production environment manual approval ve restricted runner kullanır.
- Deployment sonrası health, login, public catalog ve mobile LAN smoke kanıtı saklanır.
- Rollback ve forward-fix davranışı release evidence içinde kayıtlıdır.

Bu belge aktif workflow dosyaları veya deploy script’leri değildir. Solution scaffold’u oluşturulduğunda gerçek `.github/workflows`, `deploy/scripts`, Dockerfile ve Compose path’leri bu contract’tan türetilecektir.
