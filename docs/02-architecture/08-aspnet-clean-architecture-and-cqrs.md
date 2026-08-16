# Factory ERP — ASP.NET Core Clean Architecture ve CQRS Tasarımı

**Aşama:** ARCHITECTURE

**Durum:** Klasör, dependency, CQRS ve handler tasarımı; production source code değildir.

**Baseline:** ASP.NET Core Web API, EF Core, PostgreSQL, JWT + refresh token, modüler monolith ve O-001–O-014 kabul edilmiş kararları.

## 1. Mimari hedef

ERP MVP, tek deploy edilen fakat bounded context sınırları açık olan bir modular monolith olarak tasarlanır. Clean Architecture dependency yönü dış katmandan iç katmana doğrudur:

```text
API / Presentation
        ↓
Application / Use Cases
        ↓
Domain / Business Rules
        ↑
Infrastructure / Persistence, Auth, Files, Notifications
```

`Domain` hiçbir ASP.NET Core, EF Core, PostgreSQL, Docker veya external provider tipine bağımlı olmaz. `Application` use-case contract’larını ve port/interface’leri tanımlar. `Infrastructure` bu portları uygular. `API` HTTP DTO/route/policy adapter’ıdır.

## 2. Solution klasör yapısı

```text
src/
├─ FactoryErp.sln
├─ FactoryErp.Api/
│  ├─ Controllers/
│  │  ├─ AuthController.cs
│  │  ├─ ProductsController.cs
│  │  ├─ OrdersController.cs
│  │  ├─ DeliveryNotesController.cs
│  │  ├─ InvoicesController.cs
│  │  ├─ ShipmentsController.cs
│  │  ├─ PaymentsController.cs
│  │  └─ PublicCatalogController.cs
│  ├─ Middleware/
│  │  ├─ ExceptionMappingMiddleware.cs
│  │  ├─ CorrelationIdMiddleware.cs
│  │  └─ RequestLoggingMiddleware.cs
│  ├─ Filters/
│  ├─ Authorization/
│  │  ├─ PermissionPolicyProvider.cs
│  │  └─ PermissionAuthorizationHandler.cs
│  ├─ Contracts/
│  │  ├─ Common/
│  │  ├─ Orders/
│  │  ├─ DeliveryNotes/
│  │  ├─ Invoices/
│  │  ├─ Shipments/
│  │  └─ PublicCatalog/
│  ├─ OpenApi/
│  ├─ Program.cs
│  └─ appsettings.*.json
│
├─ FactoryErp.Application/
│  ├─ Abstractions/
│  │  ├─ Persistence/
│  │  ├─ Identity/
│  │  ├─ Clock/
│  │  ├─ Idempotency/
│  │  ├─ Files/
│  │  ├─ Notifications/
│  │  └─ Integration/
│  ├─ Common/
│  │  ├─ Behaviors/
│  │  │  ├─ ValidationBehavior.cs
│  │  │  ├─ AuthorizationBehavior.cs
│  │  │  ├─ TransactionBehavior.cs
│  │  │  ├─ IdempotencyBehavior.cs
│  │  │  └─ AuditBehavior.cs
│  │  ├─ Errors/
│  │  ├─ Models/
│  │  ├─ Pagination/
│  │  └─ Mapping/
│  ├─ Products/
│  │  ├─ Commands/
│  │  ├─ Queries/
│  │  └─ Dtos/
│  ├─ Sales/
│  │  ├─ Commands/
│  │  │  ├─ CreateOrder/
│  │  │  ├─ SubmitOrder/
│  │  │  ├─ ApproveOrder/
│  │  │  └─ CancelOrder/
│  │  ├─ Queries/
│  │  └─ Dtos/
│  ├─ Warehouse/
│  ├─ Shipping/
│  │  ├─ Commands/
│  │  │  ├─ CreateDeliveryNote/
│  │  │  ├─ IssueDeliveryNote/
│  │  │  ├─ ReverseDeliveryNote/
│  │  │  ├─ EvaluateVehicleFit/
│  │  │  └─ LockLoadPlan/
│  │  ├─ Queries/
│  │  └─ Dtos/
│  ├─ Invoicing/
│  │  ├─ Commands/
│  │  │  ├─ CreateInvoice/
│  │  │  ├─ IssueInvoice/
│  │  │  └─ ReverseInvoice/
│  │  ├─ Queries/
│  │  └─ Dtos/
│  ├─ Payments/
│  ├─ Production/
│  ├─ Employees/
│  └─ Reporting/
│
├─ FactoryErp.Domain/
│  ├─ Common/
│  │  ├─ Entity.cs
│  │  ├─ AggregateRoot.cs
│  │  ├─ DomainEvent.cs
│  │  ├─ Result.cs
│  │  └─ BusinessRuleException.cs
│  ├─ Products/
│  ├─ Customers/
│  ├─ Sales/
│  │  ├─ SalesOrder.cs
│  │  ├─ SalesOrderItem.cs
│  │  ├─ SalesOrderStatus.cs
│  │  └─ Events/
│  ├─ Warehouse/
│  ├─ Shipping/
│  │  ├─ DeliveryNote.cs
│  │  ├─ DeliveryNoteItem.cs
│  │  ├─ DeliveryNoteItemAllocation.cs
│  │  ├─ Shipment.cs
│  │  ├─ LoadPlan.cs
│  │  └─ VehicleFitEvaluation.cs
│  ├─ Invoicing/
│  │  ├─ Invoice.cs
│  │  ├─ InvoiceItem.cs
│  │  └─ InvoiceItemAllocation.cs
│  ├─ CurrentAccounts/
│  ├─ Payments/
│  ├─ Production/
│  └─ Employees/
│
├─ FactoryErp.Infrastructure/
│  ├─ Persistence/
│  │  ├─ FactoryErpDbContext.cs
│  │  ├─ Configurations/
│  │  ├─ Interceptors/
│  │  ├─ Migrations/
│  │  ├─ Repositories/
│  │  └─ Seed/
│  ├─ Identity/
│  ├─ Idempotency/
│  ├─ Files/
│  ├─ Notifications/
│  ├─ Integrations/
│  │  └─ EInvoice/
│  └─ DependencyInjection.cs
│
├─ FactoryErp.Migrator/
│  ├─ Program.cs
│  ├─ MigrationRunner.cs
│  └─ SeedRunner.cs
│
└─ FactoryErp.Worker/
   ├─ Program.cs
   ├─ BackupVerificationJob.cs
   ├─ NotificationJob.cs
   └─ ReportExportJob.cs

tests/
├─ FactoryErp.Domain.UnitTests/
├─ FactoryErp.Application.UnitTests/
├─ FactoryErp.Persistence.IntegrationTests/
├─ FactoryErp.Api.IntegrationTests/
├─ FactoryErp.ApiContractTests/
├─ FactoryErp.SecurityTests/
└─ FactoryErp.ArchitectureTests/

deploy/
├─ compose.yaml
├─ compose.dev.yaml
├─ compose.prod.yaml
├─ api.Dockerfile
├─ migrator.Dockerfile
├─ worker.Dockerfile
├─ reverse-proxy/
├─ scripts/
└─ evidence/
```

## 3. Project dependency matrisi

| Project | Referans verebilir | Referans veremez |
|---|---|---|
| `FactoryErp.Domain` | BCL ve domain utility | ASP.NET, EF Core, PostgreSQL, API, Infrastructure |
| `FactoryErp.Application` | Domain, application abstractions | API, EF Core implementation, Docker |
| `FactoryErp.Infrastructure` | Application, Domain, EF Core, Npgsql, providers | API controller |
| `FactoryErp.Api` | Application, Infrastructure composition root | Domain persistence internals |
| `FactoryErp.Migrator` | Infrastructure persistence, application seed contract | API runtime |
| `FactoryErp.Worker` | Application, Infrastructure | Controller/HTTP presentation |
| Unit tests | İlgili unit project | Diğer test database’ine zorunlu bağımlılık |
| Integration tests | API/Application/Infrastructure test host | Production secret veya production database |

Architecture testleri dependency yönünü compile/reflection veya approved architecture rules ile doğrular. Domain assembly’nin Infrastructure namespace’ine referansı release gate’i kırar.

## 4. CQRS sözleşmesi

CQRS burada read/write model ayrımını zorunlu mikroservis ayrımı olarak değil, use-case ve query projection ayrımı olarak kullanır.

```csharp
public interface ICommand<TResult> { }

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> Handle(
        TCommand command,
        CancellationToken cancellationToken);
}

public interface IQuery<TResult> { }

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(
        TQuery query,
        CancellationToken cancellationToken);
}
```

Gerçek implementation’da MediatR kullanılacaksa bu interface’ler `IRequest<TResult>`/`IRequestHandler<TRequest,TResponse>` adapter’ı olabilir. Domain’in MediatR’a bağlanması zorunlu değildir.

## 5. Pipeline behavior sırası

Critical command’ler aşağıdaki sırayla çalışır:

```text
Correlation
→ Request validation
→ Authorization/permission
→ Idempotency lookup
→ Transaction begin
→ Handler/domain rule
→ Audit/domain event registration
→ SaveChanges
→ Idempotency result persist
→ Commit
→ Notification/outbox dispatch
```

Validation ve authorization transaction başlamadan yapılır. Miktar, allocation, stock, current account veya state guard gibi database ile yarışabilecek kontroller transaction içinde tekrar edilir.

### 5.1 Behavior taslakları

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        Func<Task<TResponse>> next)
    {
        // Fluent validators or equivalent application validators.
        // Do not perform stock/quantity read here; those belong inside handler transaction.
        await ValidateRequest(request, cancellationToken);
        return await next();
    }
}
```

```csharp
public sealed class TransactionBehavior<TRequest, TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        Func<Task<TResponse>> next)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await next();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

Bu taslaklar yalnızca Architecture örneğidir. Gerçek pipeline exception mapping, cancellation ve retry behavior’ları application framework seçimi kesinleştirildikten sonra yazılacaktır.

## 6. Örnek CQRS — ApproveOrder

### 6.1 Command ve result

```csharp
public sealed record ApproveOrderCommand(
    Guid OrderId,
    string? Comment,
    long ExpectedRowVersion
) : ICommand<ApproveOrderResult>;

public sealed record ApproveOrderResult(
    Guid OrderId,
    string Status,
    IReadOnlyCollection<Guid> ReservationIds,
    long RowVersion
);
```

### 6.2 Handler taslağı

```csharp
public sealed class ApproveOrderHandler
    : ICommandHandler<ApproveOrderCommand, ApproveOrderResult>
{
    private readonly ICurrentUser _currentUser;
    private readonly ISalesOrderRepository _orders;
    private readonly IStockReservationService _reservations;
    private readonly IRiskPolicy _riskPolicy;
    private readonly IAuditWriter _audit;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<ApproveOrderResult> Handle(
        ApproveOrderCommand command,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetForApprovalAsync(
            command.OrderId,
            command.ExpectedRowVersion,
            cancellationToken);

        if (order is null)
            throw Problem.NotFound("ORDER_NOT_FOUND");

        if (order.Status != SalesOrderStatus.PendingApproval)
            throw Problem.Conflict("STATE_TRANSITION_CONFLICT");

        var risk = await _riskPolicy.EvaluateAsync(
            order.CustomerId,
            cancellationToken);

        if (risk.HardBlock)
            throw Problem.Unprocessable("RISK_HARD_BLOCK");

        if (risk.SoftBlock && !_currentUser.HasPermission("risk.override"))
            throw Problem.Forbidden("RISK_OVERRIDE_PERMISSION_REQUIRED");

        var reservationResult = await _reservations.ReserveAsync(
            order,
            cancellationToken);

        order.Approve(
            _currentUser.UserId,
            command.Comment,
            risk.Snapshot,
            reservationResult.ReservationIds);

        await _audit.WriteAsync(
            AuditEvent.OrderApproved(order.Id, risk.Snapshot),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApproveOrderResult(
            order.Id,
            order.Status.ToString(),
            reservationResult.ReservationIds,
            order.RowVersion);
    }
}
```

Handler, `SalesOrder` aggregate’in private transition method’unu çağırır. Controller doğrudan `order.Status = Approved` yapamaz. Risk override varsa permission, comment ve audit zorunludur.

## 7. Örnek CQRS — IssueDeliveryNote

### 7.1 Request model ve command

```csharp
public sealed record IssueDeliveryItem(
    Guid DeliveryNoteItemId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    decimal ClientQuantityBase,
    Guid WarehouseId,
    Guid LocationId,
    object? PackagingBreakdown
);

public sealed record IssueDeliveryNoteCommand(
    Guid DeliveryNoteId,
    IReadOnlyCollection<IssueDeliveryItem> Items,
    Guid? ShipmentId,
    string IdempotencyKey,
    string PayloadHash,
    long ExpectedRowVersion
) : ICommand<IssueDeliveryNoteResult>;

public sealed record IssueDeliveryNoteResult(
    Guid DeliveryNoteId,
    string DeliveryNoteStatus,
    string SalesOrderStatus,
    IReadOnlyCollection<Guid> AllocationIds,
    IReadOnlyCollection<Guid> StockMovementIds,
    long RowVersion
);
```

### 7.2 Handler taslağı

```csharp
public sealed class IssueDeliveryNoteHandler
    : ICommandHandler<IssueDeliveryNoteCommand, IssueDeliveryNoteResult>
{
    private readonly IDeliveryNoteRepository _deliveryNotes;
    private readonly IQuantityCalculator _quantityCalculator;
    private readonly IStockLedger _stockLedger;
    private readonly IReservationService _reservations;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly ICurrentUser _currentUser;

    public async Task<IssueDeliveryNoteResult> Handle(
        IssueDeliveryNoteCommand command,
        CancellationToken cancellationToken)
    {
        var previous = await _idempotency.TryGetAsync(
            "delivery-note.issue",
            command.IdempotencyKey,
            cancellationToken);

        if (previous is not null)
        {
            if (previous.PayloadHash != command.PayloadHash)
                throw Problem.Conflict("IDEMPOTENCY_PAYLOAD_MISMATCH");

            return previous.GetResult<IssueDeliveryNoteResult>();
        }

        var delivery = await _deliveryNotes.GetForIssueWithLocksAsync(
            command.DeliveryNoteId,
            command.ExpectedRowVersion,
            cancellationToken);

        if (delivery is null)
            throw Problem.NotFound("DELIVERY_NOTE_NOT_FOUND");

        delivery.AssertCanIssue();

        var resolvedItems = new List<ResolvedDeliveryItem>();
        foreach (var item in command.Items)
        {
            var source = delivery.GetItem(item.DeliveryNoteItemId);
            var calculation = await _quantityCalculator.CalculateAsync(
                source.ProductId,
                item.EnteredQuantity,
                item.EnteredPackagingId,
                item.PackagingBreakdown,
                cancellationToken);

            if (calculation.QuantityBase != item.ClientQuantityBase)
                throw Problem.Unprocessable(
                    "QUANTITY_BASE_MISMATCH",
                    new { serverQuantityBase = calculation.QuantityBase });

            source.AssertCanShip(calculation.QuantityBase);
            resolvedItems.Add(new ResolvedDeliveryItem(
                source, calculation, item.WarehouseId, item.LocationId));
        }

        foreach (var item in resolvedItems)
        {
            await _stockLedger.AssertAvailableAndLockAsync(
                item.ProductId,
                item.WarehouseId,
                item.LocationId,
                item.QuantityBase,
                cancellationToken);
        }

        var allocations = delivery.Issue(
            resolvedItems,
            _currentUser.UserId);

        var movements = await _stockLedger.PostShipmentAsync(
            allocations,
            command.IdempotencyKey,
            cancellationToken);

        await _reservations.ConsumeOrReleaseAsync(
            allocations,
            cancellationToken);

        await _audit.WriteAsync(
            AuditEvent.DeliveryIssued(delivery.Id, allocations),
            cancellationToken);

        var result = new IssueDeliveryNoteResult(
            delivery.Id,
            delivery.Status.ToString(),
            delivery.SalesOrderStatus.ToString(),
            allocations.Select(x => x.Id).ToArray(),
            movements.Select(x => x.Id).ToArray(),
            delivery.RowVersion);

        await _idempotency.StoreAsync(
            "delivery-note.issue",
            command.IdempotencyKey,
            command.PayloadHash,
            result,
            cancellationToken);

        return result;
    }
}
```

Bu handler’da quantity preview yalnızca UX yardımcısıdır. Issue transaction’ında packaging conversion yeniden hesaplanır, source item kilitlenir, stock/reservation/allocation sınırları tekrar kontrol edilir ve başarısız transaction rollback olur.

## 8. Örnek CQRS — IssueInvoice

```csharp
public sealed record IssueInvoiceCommand(
    Guid InvoiceId,
    string IdempotencyKey,
    string PayloadHash,
    long ExpectedRowVersion
) : ICommand<IssueInvoiceResult>;

public sealed record IssueInvoiceResult(
    Guid InvoiceId,
    string Status,
    decimal GrandTotal,
    Guid CurrentTransactionId,
    IReadOnlyCollection<Guid> AllocationIds,
    long RowVersion
);
```

```csharp
public sealed class IssueInvoiceHandler
    : ICommandHandler<IssueInvoiceCommand, IssueInvoiceResult>
{
    private readonly IInvoiceRepository _invoices;
    private readonly IDeliveryAllocationReader _deliveryAllocations;
    private readonly ICurrentAccountLedger _currentAccount;
    private readonly IInvoiceIntegrationService _invoiceIntegration;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;

    public async Task<IssueInvoiceResult> Handle(
        IssueInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var replay = await _idempotency.TryGetAsync(
            "invoice.issue", command.IdempotencyKey, cancellationToken);

        if (replay is not null)
        {
            if (replay.PayloadHash != command.PayloadHash)
                throw Problem.Conflict("IDEMPOTENCY_PAYLOAD_MISMATCH");
            return replay.GetResult<IssueInvoiceResult>();
        }

        var invoice = await _invoices.GetForIssueWithLocksAsync(
            command.InvoiceId,
            command.ExpectedRowVersion,
            cancellationToken);

        if (invoice is null)
            throw Problem.NotFound("INVOICE_NOT_FOUND");

        invoice.AssertCanIssue();

        var sourceAllocations = await _deliveryAllocations
            .GetInvoiceableIssuedItemsWithLocksAsync(invoice.Id, cancellationToken);

        invoice.AssertAllocationsWithinRemaining(sourceAllocations);
        invoice.CalculateTotalsFromSnapshots();

        var adapterResult = await _invoiceIntegration.ValidateOrPrepareAsync(
            invoice,
            cancellationToken);

        invoice.Issue(adapterResult.ProviderReference);

        var debit = await _currentAccount.PostInvoiceDebitAsync(
            invoice,
            command.IdempotencyKey,
            cancellationToken);

        await _audit.WriteAsync(
            AuditEvent.InvoiceIssued(invoice.Id, debit.Id),
            cancellationToken);

        var result = new IssueInvoiceResult(
            invoice.Id,
            invoice.Status.ToString(),
            invoice.GrandTotal,
            debit.Id,
            invoice.AllocationIds,
            invoice.RowVersion);

        await _idempotency.StoreAsync(
            "invoice.issue", command.IdempotencyKey,
            command.PayloadHash, result, cancellationToken);

        return result;
    }
}
```

`IssueInvoiceHandler` stok ledger’ına dokunmaz. Sadece invoice allocation, tax/price snapshot, current debit, adapter/stub sonucu, audit ve idempotency result aynı transaction’da commit edilir.

## 9. Örnek CQRS query — Order detail

Query tarafı aggregate command gibi tüm entity graph’ını yüklemez. Projection ile client DTO üretir.

```csharp
public sealed record GetOrderByIdQuery(Guid OrderId)
    : IQuery<OrderDetailDto?>;

public sealed class GetOrderByIdHandler
    : IQueryHandler<GetOrderByIdQuery, OrderDetailDto?>
{
    private readonly IOrderReadDb _readDb;
    private readonly ICurrentUser _currentUser;

    public Task<OrderDetailDto?> Handle(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken)
    {
        return _readDb.Orders
            .Where(x => x.Id == query.OrderId)
            .Where(x => _currentUser.CanReadCustomer(x.CustomerId))
            .Select(x => new OrderDetailDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                CustomerId = x.CustomerId,
                Status = x.Status,
                Items = x.Items
                    .OrderBy(i => i.LineNo)
                    .Select(i => new OrderItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        OrderedQtyBase = i.OrderedQty,
                        ShippedQtyBase = i.ShippedQty,
                        RemainingQtyBase = i.RemainingQty,
                        BaseUomCode = i.BaseUomCode
                    })
                    .ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
```

Query response’larında internal cost, risk input detail, employee salary, audit raw JSON ve private file storage key’i permission olmadan dönmez.

## 10. Controller adapter taslağı

Controller use-case başlatır; domain rule uygulamaz.

```csharp
[ApiController]
[Route("api/v1/delivery-notes")]
public sealed class DeliveryNotesController : ControllerBase
{
    [HttpPost("{id:guid}/issue")]
    [HasPermission("delivery-note.issue")]
    public async Task<ActionResult<IssueDeliveryNoteResponse>> Issue(
        Guid id,
        IssueDeliveryNoteRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(
            id,
            Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers["If-Match"].ToString());

        var result = await _sender.Send(command, cancellationToken);
        return Ok(IssueDeliveryNoteResponse.From(result));
    }
}
```

ProblemDetails middleware exception types’i HTTP contract’a map eder. Controller `try/catch` ile her endpoint’te farklı hata response’u üretmez.

## 11. EF Core repository/port sınırları

Application yalnızca port/interface bilir:

```csharp
public interface ISalesOrderRepository
{
    Task<SalesOrder?> GetForApprovalAsync(
        Guid orderId,
        long expectedRowVersion,
        CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
```

Infrastructure implementasyonu EF Core query, `AsNoTracking` query projection, locked command query ve PostgreSQL transaction isolation detaylarını bilir. Domain/Application PostgreSQL SQL string’lerini doğrudan yazmaz.

## 12. CQRS handler test mapping

| Handler | Unit test | Integration test |
|---|---|---|
| `ApproveOrderHandler` | State/risk/permission/reservation port çağrısı | Reservation + order state + audit transaction |
| `IssueDeliveryNoteHandler` | Quantity mismatch/idempotency orchestration | Stock lock, allocation trigger, movement and rollback |
| `IssueInvoiceHandler` | Issued source/over-allocation/current debit orchestration | Invoice allocation, debit, no stock movement |
| `LockLoadPlanHandler` | Hard error/manual approval/override policy | Load plan state, audit, candidate result persistence |
| `GetOrderByIdHandler` | Projection and permission filter | DTO field isolation and pagination |
| `ExportSalaryHandler` | Masking/permission/export audit | File metadata, audit and forbidden fields |

## 13. Architecture testleri

`FactoryErp.ArchitectureTests` şu kuralları doğrular:

- Domain project hiçbir Infrastructure/API namespace’i kullanmıyor.
- Application yalnızca Domain ve kendi abstraction’larına bağlı.
- Controller’lar `DbContext` veya EF entity’sini doğrudan expose etmiyor.
- Command handler’lar transaction/idempotency/audit policy’sini bypass etmiyor.
- Query handler’lar server-side projection kullanıyor.
- `IssueDeliveryNote` ve `IssueInvoice` dışında allocation child update endpoint’i bulunmuyor.
- O-004 BOM/hammadde ve O-005 lot/seri MVP dışı sınırı source tree/migration’da korunuyor.
- Sensitive salary/risk/current-account response’ları permission policy olmadan dönmüyor.

## 14. Implementation’a geçmeden önce üretilecek dosyalar

Architecture acceptance sonrasında implementation şu sırayla başlar:

```text
1. FactoryErp.sln ve project files
2. Domain common + aggregate/value object skeleton
3. Application command/query contracts ve validators
4. Infrastructure DbContext/configurations
5. Migrator ve 0001–0018 EF migrations
6. API composition root, auth ve ProblemDetails middleware
7. Critical handlers: approve order, issue delivery, issue invoice
8. Persistence/API/security tests
9. Dockerfiles, Compose ve CI workflow’ları
```

Bu belge örnek kod blokları içerir; bloklar doğrudan production’a kopyalanmadan namespace, framework, serialization, DI, transaction ve error mapping kararları implementation scaffold’unda sabitlenmelidir.
