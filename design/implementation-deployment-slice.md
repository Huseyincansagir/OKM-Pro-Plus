# Factory ERP-Lite Production Deployment Implementation Slice

**Tarih:** 2026-08-17
**Baseline:** `2526507`
**Durum:** API + PostgreSQL + controlled migrator + reverse-proxy baseline PASS; web/mobile/worker/backup implementation ayrı kapsam.

## 1. Amaç

Bu slice, L4-B6 sonrasında Docker Compose deployment zincirinin çalıştırılabilir ilk production baseline’ını ekler. API image’ı, PostgreSQL private data network’ü, controlled migration profile, API readiness healthcheck’i ve internal API’yi edge network’e taşıyan Nginx reverse proxy aynı Compose dosyası içinde tanımlanmıştır.

Bu slice mevcut repository’de web, Flutter mobile, worker ve backup implementation’ı bulunmadığı için tam şirket ERP production topolojisinin tüm servislerini iddia etmez. Eksik servisler sonraki deployment slice’larında eklenmelidir.

## 2. Eklenen dosyalar

| Dosya | Amaç |
|---|---|
| `deploy/Dockerfile` | API ve migrator için .NET 8 multi-stage image; non-root `app` user |
| `deploy/compose.prod.yaml` | PostgreSQL, migration profile, API ve reverse-proxy services |
| `deploy/nginx.prod.conf` | API health ve `/api/v1/` internal proxy route’ları; default route 404 |
| `deploy/.env.prod.example` | Secret içermeyen production environment contract |
| `.dockerignore` | Build context ve secret/artifact dışlama |

## 3. Compose servisleri ve ağ politikası

| Service | Network | Host exposure | Sağlık/başlatma |
|---|---|---|---|
| `postgres` | `data` internal | Host port yok | `pg_isready`, `service_healthy` |
| `migrator` | `data` internal | Host port yok | `migration` profile, one-shot |
| `api` | `internal`, `data` | `expose` 8080; host port yok | `/health/ready`, `service_healthy` |
| `reverse-proxy` | `edge`, `internal` | Configurable HTTP port | API healthy sonrası start |

PostgreSQL production Compose’ta host interface’e bind edilmez. API yalnızca internal reverse proxy ve private data network üzerinden erişilir. Reverse proxy PostgreSQL network’üne bağlı değildir. API internal health route’ları ve `/api/v1/` route’ları dışında default `/` path’ini 404 döndürür.

## 4. Release çalışma sırası

Production’da application startup’ı migration çalıştırmaz. Release operator aşağıdaki sıralamayı kullanmalıdır:

```bash
cp deploy/.env.prod.example .env.prod
chmod 600 .env.prod
# .env.prod içindeki tüm placeholder secret değerlerini değiştir

sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml build api migrator
sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml up -d postgres
sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml --profile migration run --rm migrator
sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml up -d api reverse-proxy
sudo docker compose --env-file .env.prod -f deploy/compose.prod.yaml ps
```

Migration job başarıyla tamamlanmadan API/reverse-proxy release’i kabul edilmemelidir. Destructive migration için otomatik rollback uygulanmaz; backup restore veya forward-fix migration kullanılmalıdır.

## 5. Smoke test sonuçları

Temiz bir Compose PostgreSQL volume’unda aşağıdaki kontroller yürütüldü:

| Kontrol | Sonuç |
|---|---|
| `docker compose config --quiet` | PASS |
| API image build | PASS |
| Migrator image build | PASS |
| PostgreSQL 16.4 start/health | PASS |
| 18 migration clean apply | PASS |
| B6 permission ve seed apply | PASS |
| API `service_healthy` | PASS |
| `/health/live` | HTTP 200 |
| `/health/ready` | HTTP 200 / Healthy |
| `/health/startup` | HTTP 200 / Healthy |
| Admin `/api/v1/auth/login` | HTTP 200 |
| Anonymous B6 dispatch | HTTP 401 |
| Authenticated admin B6 dispatch probe | HTTP 422; 401/403 değil, handler katmanına ulaştı |
| Nginx `nginx -t` | PASS |
| Compose cleanup / volume removal | PASS |

API image testinde `USER app` ile non-root runtime çalıştı. Migration ve API aynı Compose `postgres` service adına bağlandı; host port mapping gerektirmeden internal network üzerinde doğrulandı.

## 6. Sandbox ve acceptance sınırlamaları

Sandbox kernel’inde Docker iptables `raw` tablosu bulunmadığı için edge network’ün reverse-proxy container endpoint’i oluşturulamadı. Bu nedenle reverse proxy’nin Nginx syntax testi başarıyla çalıştırıldı; gerçek proxy request smoke testi şirket Ubuntu LTS sunucusunda yapılmalıdır.

Ayrıca repository’de şu production servisleri henüz bulunmamaktadır:

| Eksik servis | Durum |
|---|---|
| Next.js `web` image | BLOCKED — web source/package yok |
| Flutter/mobile runtime | BLOCKED — mobile source/package yok |
| Worker/outbox consumer | BLOCKED — worker project yok |
| Backup/restore service | BLOCKED — backup script/service yok |
| HTTPS certificate/secret provisioning | BLOCKED — deployment host configuration gerekli |
| LAN/public catalog route | BLOCKED — web/reverse-proxy route implementation sonraki slice |

Bu nedenle bu commit **API + database + controlled migration + reverse proxy deployment baseline’ını** tamamlar; tam production ERP deployment acceptance için yukarıdaki servislerin ayrıca eklenmesi gerekir.

## References

[1]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/deploy/Dockerfile "Factory ERP .NET API and migrator Dockerfile"

[2]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/deploy/compose.prod.yaml "Factory ERP production Compose baseline"

[3]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/deploy/nginx.prod.conf "Factory ERP Nginx reverse proxy configuration"

[4]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/docker-compose-deployment-plan.md "Factory ERP deployment architecture baseline"
