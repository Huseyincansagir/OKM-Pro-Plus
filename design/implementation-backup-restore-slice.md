# Factory ERP-Lite Backup / Restore Implementation Slice

**Tarih:** 2026-08-18
**Baseline:** `9057456`
**Durum:** Backup role, custom-format dump, SHA-256 checksum, 14-day retention, restore role ve isolated restore smoke akışı PASS.

## 1. Amaç

Bu slice, production Compose baseline’ına uygulama database kullanıcısından ayrılmış backup ve restore rollerini ekler. Backup job yalnızca PostgreSQL’den okur ve ayrı named volume’a custom-format dump yazar. Her dump için SHA-256 checksum üretilir, checksum hemen doğrulanır ve son başarılı dump `latest.dump` pointer’ı ile işaretlenir. Restore smoke job checksum’ı doğruladıktan sonra dump’ı temporary database’e restore eder ve migration history tablosunu kontrol eder.

## 2. Eklenen artefact’lar

| Dosya | Sorumluluk |
|---|---|
| `deploy/backup/Dockerfile` | PostgreSQL 16.4 client image; backup, restore ve role bootstrap scriptleri |
| `deploy/backup/backup.sh` | `pg_dump --format=custom`, checksum, latest pointer ve retention |
| `deploy/backup/restore-smoke.sh` | Checksum doğrulama, temporary database create/drop, `pg_restore`, migration count |
| `deploy/backup/bootstrap-backup-role.sql` | Idempotent read-only backup role ve SELECT grant’leri |
| `deploy/backup/bootstrap-restore-role.sql` | Idempotent CREATEDB restore smoke role |
| `deploy/backup/bootstrap-backup-role.sh` | Backup role psql wrapper |
| `deploy/backup/bootstrap-restore-role.sh` | Restore role psql wrapper |
| `deploy/compose.prod.yaml` | `backup`, `backup-role-bootstrap`, `backup-volume-init`, `restore-role-bootstrap`, `restore-smoke` profiles |
| `deploy/.env.prod.example` | Backup/restore role secret contract’ı |

## 3. Güvenlik ve volume politikası

API connection string’de kullanılan `factory_erp_app` rolü backup job’ında kullanılmaz. `factory_erp_backup` rolü yalnızca target database’e connect, public schema usage, mevcut table/sequence SELECT ve default privilege SELECT haklarına sahiptir. Restore smoke için `factory_erp_restore` rolü ayrı tutulur ve yalnızca isolated temporary restore database oluşturabilmesi için `CREATEDB` alır.

Backup files ayrı `backup_data` named volume’unda tutulur; PostgreSQL data volume’u ile aynı volume değildir. Backup job `data` network üzerinden PostgreSQL’e, `backup` internal network üzerinden backup volume service’ine bağlanır. Backup volume init container `postgres:16.4` image’ındaki UID/GID `999:999` ile owner ve `0700` directory permission’ı kurar.

## 4. Compose çalışma sırası

```bash
sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml up -d postgres
sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml --profile migration run --rm migrator
sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml --profile backup-setup run --rm backup-role-bootstrap
sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml --profile backup run --rm backup
sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml --profile restore run --rm restore-role-bootstrap
sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml --profile restore run --rm restore-smoke
```

Günlük backup job host scheduler veya kontrollü worker trigger ile çalıştırılmalıdır. Backup service API request transaction’ına bağlanmaz. Backup başarısızlığı sessiz başarı olarak raporlanmamalı; scheduler/monitoring katmanı job exit code ve backup age kontrolü yapmalıdır.

## 5. Smoke test sonuçları

Temiz `factory_erp_backup_smoke` PostgreSQL database’inde migration ve seed sonrası şu kontroller yürütüldü:

| Kontrol | Sonuç |
|---|---|
| Backup image build | PASS |
| Shell syntax checks | PASS |
| Compose config validation | PASS |
| Separate backup role bootstrap | PASS |
| Backup role can connect/read database | PASS — `pg_dump` başarılı |
| Custom-format dump | PASS |
| SHA-256 checksum creation and immediate verification | PASS |
| `latest.dump` and `latest.dump.sha256` pointers | PASS |
| 14-day retention | PASS — 30-day fixture removed |
| Separate restore role bootstrap | PASS |
| Checksum-guarded `pg_restore` | PASS |
| Temporary restore database | PASS — `factory_erp_restore_smoke` |
| Restored migration history | PASS — 18 migration rows |
| Volume/network cleanup | PASS |

Restore sonucu temporary database adı ve migration count’i şu şekilde doğrulandı:

```text
factory_erp_restore_smoke
"__EFMigrationsHistory"
18
```

## 6. Kalan operasyon kapsamı

Bu implementation slice backup/restore job’larını ve kontrollü Compose profile’larını ekler. Aşağıdaki production işletim adımları henüz host-specific configuration olarak kalır: günlük scheduler/alert wiring, ayrı fiziksel/NAS backup target mount’u, 14 günlük gerçek backup availability ölçümü, aylık restore takvimi, checksum notification ve RPO/RTO süre ölçümü. Bu değerler application repository içinde sabit varsayılmamalıdır.

Web, Flutter mobile, worker/outbox consumer ve public catalog implementation’ı bu slice’ın dışındadır. Bu slice tamamlandıktan sonra arayüz implementation’ına geçilebilir; ancak production release gate için host üzerinde HTTPS/LAN, backup target ve scheduler acceptance ayrıca çalıştırılmalıdır.

## References

[1]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/deploy/compose.prod.yaml "Factory ERP production Compose with backup and restore profiles"

[2]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/deploy/backup/backup.sh "Factory ERP PostgreSQL backup job"

[3]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/deploy/backup/restore-smoke.sh "Factory ERP isolated restore smoke job"

[4]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/docker-compose-deployment-plan.md "Factory ERP deployment architecture plan"
