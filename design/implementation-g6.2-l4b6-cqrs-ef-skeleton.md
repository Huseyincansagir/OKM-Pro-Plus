# L4-B6 DispatchRun CQRS / EF Core Implementation

**Tarih:** 2026-08-17
**Durum:** Production implementation tamamlandı; full verification PASS
**Baseline:** L4-B5 commit `352983c`

## 1. Uygulanan kapsam

L4-B6 bounded slice, `Departure / Vehicle Dispatch / Route Execution` akışını gerçek transactional CQRS handler olarak tamamlar. Bu slice; LoadPlan–Shipment–RoutePlan handoff’unu, araç ve şoför rezervasyonunu, departure state transition’larını, stop sequencing’i, route event timeline’ını, idempotency replay/mismatch davranışını, audit yazımını ve gerçek-login authorization boundary’lerini kapsar.

Stock ledger, GPS provider, telemetri, offline queue, müşteri teslimat kanıtı, partial shipment ve replan bu slice’ın kapsamı dışındadır.

## 2. Production dosyaları

| Dosya | Uygulanan rol |
|---|---|
| `src/FactoryErp.Domain/Shipping/DispatchRun.cs` | DispatchRun aggregate, state machine, stop guard’ları ve `Rehydrate` API’si |
| `src/FactoryErp.Application/Shipping/DispatchRunCqrs.cs` | Explicit CQRS command, DTO ve handler sözleşmeleri |
| `src/FactoryErp.Infrastructure/Shipping/DispatchRunCommandHandler.cs` | Sekiz command için transaction, row lock, precondition, projection, event, audit ve idempotency implementation’ı |
| `src/FactoryErp.Api/Controllers/DispatchRunsController.cs` | `/api/v1/route-plans/{id}/dispatch` ve `/api/v1/dispatch-runs/{id}/...` endpoint’leri |
| `src/FactoryErp.Api/Authorization/PermissionPolicies.cs` | `shipment.dispatch`, `shipment.depart`, `shipment.route-execute`, `shipment.route-exception` policy sabitleri |
| `src/FactoryErp.Api/Program.cs` | B6 authorization policy registration |
| `src/FactoryErp.Api/Idempotency/IdempotencyKeyMiddleware.cs` | `/api/v1/dispatch-runs` critical mutation prefix’i |
| `src/FactoryErp.Infrastructure/DependencyInjection.cs` | `IDispatchRunCommandHandler` scoped registration |
| `src/FactoryErp.Infrastructure/Authentication/IdentitySeeder.cs` | Permission IDs `58–61` ve system-admin role assignment |
| `src/FactoryErp.Infrastructure/Sales/SalesSeeder.cs` | Deterministik integration DeliveryNote/SalesOrder fixture seed’i |
| `tests/FactoryErp.Infrastructure.UnitTests/Shipping/DispatchRunIntegrationTests.cs` | PostgreSQL transaction, concurrency, sequencing, idempotency ve projection testleri |
| `tests/FactoryErp.Infrastructure.UnitTests/Shipping/LogisticsSecurityIntegrationTests.cs` | Gerçek `/auth/login` sonrası B6 permission ve 403 boundary testleri |

## 3. Transaction ve lock order

Tüm B6 mutation command’ları açık transaction içinde çalışır. Handler, `SKIP LOCKED` kullanmaz. Kaynak başka bir işlem tarafından tutuluyorsa PostgreSQL row lock beklemesi sonrasında active-run guard veya row-version conflict üzerinden açık hata döner.

```text
LoadPlan FOR UPDATE
→ Shipment FOR UPDATE
→ RoutePlan FOR UPDATE
→ DispatchRun / active-run lookup FOR UPDATE
→ Vehicle FOR UPDATE
→ Driver FOR UPDATE
→ RouteStops ORDER BY sequence_no, id FOR UPDATE
→ B5 Completed verification guard
→ state / ownership / overlap / license / maintenance checks
→ domain aggregate command
→ Shipment / RoutePlan / Vehicle / Driver / RouteStop projections
→ RouteExecutionEvent + audit + idempotency
→ SaveChanges + commit
```

`PrepareDispatchRun` sırasında Shipment, LoadPlan ve RoutePlan ownership zinciri; LoadPlan `Locked` state’i, vehicle/capacity/snapshot alanları, tamamlanmış B5 session, Shipment `Loaded`, vehicle availability, maintenance tarihi, driver active state’i ve license expiry birlikte doğrulanır. Vehicle, driver, shipment ve route plan üzerinde aktif bir DispatchRun varsa `DISPATCH_ACTIVE_RUN_EXISTS` döndürülür.

## 4. State ve projection kararları

DispatchRun aggregate state machine aşağıdaki gibidir:

```text
CreatePrepared → Prepared
Prepared → Dispatched          ConfirmDispatch
Dispatched → InTransit         Depart
InTransit → InTransit          ArriveAtStop / DepartStop / SkipStop
InTransit → Completed          CompleteRoute
Prepared / Dispatched → Cancelled
```

Shipment persistence status kataloğunda `Dispatched` değeri bulunmadığı için departure projection’ında Shipment `Loaded → InTransit` yapılır. `Dispatched`, DispatchRun aggregate state’idir. Departure sırasında RoutePlan `Locked → InProgress`, Vehicle `Available/Assigned → InTransit` olur. Route tamamlandığında RoutePlan `Completed`, Vehicle `Available` ve `CurrentRoutePlanId = null` olur. Shipment’ın `Delivered` state’i müşteri teslimat kanıtı kapsamı dışında bırakıldığı için B6 complete işleminde Shipment `InTransit` olarak kalır.

Route stop’ları yalnızca sıradaki `Pending` stop üzerinden ilerler. `ArriveAtStop` sonrası `Arrived`, ardından `DepartStop` ile `Departed`; istisna akışında `SkipStop` ile non-empty reason içeren `Skipped` status yazılır. Tamamlanmamış veya out-of-order stop’lar route completion’ı reddeder.

## 5. Idempotency ve audit

Her mutation command için normalize edilmiş command payload’ının SHA-256 hash’i alınır. Aynı scope ve key ile aynı payload geldiğinde kaydedilmiş response replay edilir. Aynı key farklı payload ile kullanılırsa `IDEMPOTENCY_PAYLOAD_MISMATCH` döner. RouteExecutionEvent kayıtları `dispatch_run_id + idempotency_key` ve `dispatch_run_id + sequence_no` unique index’leriyle ikinci state ilerlemesine karşı korunur.

Başarılı state değişimlerinde audit entry ve idempotency response aynı transaction kapsamında kaydedilir. Route execution event’i yalnızca response listesine değil, `route_execution_events` DbSet’ine de eklenir; böylece sonraki command’ların aggregate rehydration’ı eksiksiz timeline üzerinden yapılır.

## 6. API ve security sözleşmesi

| İşlem | Endpoint | Policy |
|---|---|---|
| Prepare | `POST /api/v1/route-plans/{routePlanId}/dispatch` | `shipment.dispatch` |
| Confirm | `POST /api/v1/dispatch-runs/{dispatchRunId}/confirm` | `shipment.dispatch` |
| Depart | `POST /api/v1/dispatch-runs/{dispatchRunId}/depart` | `shipment.depart` |
| Arrive | `POST /api/v1/dispatch-runs/{dispatchRunId}/stops/{routeStopId}/arrive` | `shipment.route-execute` |
| Depart stop | `POST /api/v1/dispatch-runs/{dispatchRunId}/stops/{routeStopId}/depart` | `shipment.route-execute` |
| Skip | `POST /api/v1/dispatch-runs/{dispatchRunId}/stops/{routeStopId}/skip` | `shipment.route-exception` |
| Complete | `POST /api/v1/dispatch-runs/{dispatchRunId}/complete` | `shipment.route-execute` |
| Cancel | `POST /api/v1/dispatch-runs/{dispatchRunId}/cancel` | `shipment.route-exception` |

Permission IDs `58–61` aşağıdaki kodlarla seed edilir: `shipment.dispatch`, `shipment.depart`, `shipment.route-execute`, `shipment.route-exception`. Gerçek `/api/v1/auth/login` testinde full kullanıcı dispatch endpoint’inde `403` almaz; read-only kullanıcı dispatch ve skip endpoint’lerinde `403` alır.

## 7. Migration ve constraint forward-fix

Ana migration:

```text
20260817144536_AddDispatchRunsAndRouteExecution
```

Bu migration `dispatch_runs`, `route_execution_events`, RouteStop execution kolonları, FK’ler, CHECK constraint’ler, filtered unique active-run index’leri ve timeline index’lerini ekler.

İlk migration’daki `ck_dispatch_runs_departed_pair` ifadesi `Dispatched` state’inde `actual_departed_at` zorunlu kıldığı için state machine ile çelişiyordu. Bu, Confirm ile Depart arasındaki geçerli ara state’i reddediyordu. Forward-fix migration ile aşağıdaki ifade uygulanır:

```sql
status in ('Prepared', 'Dispatched', 'Cancelled')
OR actual_departed_at IS NOT NULL
```

Forward-fix migration:

```text
20260817155400_FixDispatchRunDepartedPairConstraint
```

Canonical SQL betiği de aynı düzeltmeyle güncellenmiştir. Production geçmişi silinmeden constraint forward-fix uygulanmalıdır; production `Down` çalıştırılmamalıdır.

## 8. Verification sonuçları

| Gate | Sonuç |
|---|---|
| `dotnet restore` | PASS |
| `dotnet build FactoryErp.sln --configuration Release` | PASS |
| Build warning | 0 |
| Build error | 0 |
| Domain unit tests | PASS |
| Architecture tests | PASS |
| EF model tests | PASS |
| B6 PostgreSQL integration tests | PASS — 6 test |
| Gerçek-login B6 security testleri | PASS |
| Full `dotnet test FactoryErp.sln` | PASS — 78 infrastructure test; solution test project’leri başarılı |
| `git diff --check` | Pending final gate |

## 9. Kalan riskler ve B6 dışı konular

B6 handler canlı transaction sınırını ve persistence projection’larını tamamlar; ancak PostgreSQL integration fixture’ı development seed verisine dayanır ve production data migration’ında mevcut aktif/bozuk DispatchRun kayıtları için ayrı operational backfill kontrolü gerekir. Shipment `Delivered` state’i, teslimat kanıtı ve müşteri teslim alma akışı B6 sonrasına bırakılmıştır. GPS, telemetri, offline queue, notification ve stock ledger entegrasyonları bu slice içinde yapılmamıştır.

## 10. Sonraki slice

B6 completion gate sonrasında bir sonraki bounded slice’a ancak bu commit remote’a push edildikten ve deployment ortamında migration smoke test’i yapıldıktan sonra geçilmelidir. Önerilen sonraki konu, B6 dışı teslimat kanıtı veya stock ledger entegrasyonunun bağımsız bir karar ve migration planı olarak ele alınmasıdır.
