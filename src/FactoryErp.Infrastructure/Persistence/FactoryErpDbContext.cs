using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Persistence;

public sealed class FactoryErpDbContext(DbContextOptions<FactoryErpDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<RoleRecord> Roles => Set<RoleRecord>();
    public DbSet<PermissionRecord> Permissions => Set<PermissionRecord>();
    public DbSet<UserRoleRecord> UserRoles => Set<UserRoleRecord>();
    public DbSet<RolePermissionRecord> RolePermissions => Set<RolePermissionRecord>();
    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();
    public DbSet<AuditLogRecord> AuditLogs => Set<AuditLogRecord>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<SystemSettingRecord> SystemSettings => Set<SystemSettingRecord>();
    public DbSet<DocumentSequenceRecord> DocumentSequences => Set<DocumentSequenceRecord>();
    public DbSet<OutboxMessageRecord> OutboxMessages => Set<OutboxMessageRecord>();
    public DbSet<UnitOfMeasureRecord> UnitsOfMeasure => Set<UnitOfMeasureRecord>();
    public DbSet<ProductCategoryRecord> ProductCategories => Set<ProductCategoryRecord>();
    public DbSet<ProductRecord> Products => Set<ProductRecord>();
    public DbSet<ProductPackagingRecord> ProductPackagings => Set<ProductPackagingRecord>();
    public DbSet<ProductBarcodeRecord> ProductBarcodes => Set<ProductBarcodeRecord>();
    public DbSet<ProductImageRecord> ProductImages => Set<ProductImageRecord>();
    public DbSet<WarehouseRecord> Warehouses => Set<WarehouseRecord>();
    public DbSet<WarehouseLocationRecord> WarehouseLocations => Set<WarehouseLocationRecord>();
    public DbSet<StockRecord> Stocks => Set<StockRecord>();
    public DbSet<StockMovementRecord> StockMovements => Set<StockMovementRecord>();
    public DbSet<StockTransferRecord> StockTransfers => Set<StockTransferRecord>();
    public DbSet<ProductionOrderRecord> ProductionOrders => Set<ProductionOrderRecord>();
    public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();
    public DbSet<CustomerRecord> Customers => Set<CustomerRecord>();
    public DbSet<CustomerAddressRecord> CustomerAddresses => Set<CustomerAddressRecord>();
    public DbSet<CustomerContactRecord> CustomerContacts => Set<CustomerContactRecord>();
    public DbSet<PriceListRecord> PriceLists => Set<PriceListRecord>();
    public DbSet<CustomerPriceGroupRecord> CustomerPriceGroups => Set<CustomerPriceGroupRecord>();
    public DbSet<CustomerPriceGroupMemberRecord> CustomerPriceGroupMembers => Set<CustomerPriceGroupMemberRecord>();
    public DbSet<ProductPriceRecord> ProductPrices => Set<ProductPriceRecord>();
    public DbSet<QuoteRequestRecord> QuoteRequests => Set<QuoteRequestRecord>();
    public DbSet<QuoteRequestItemRecord> QuoteRequestItems => Set<QuoteRequestItemRecord>();
    public DbSet<StockReservationRecord> StockReservations => Set<StockReservationRecord>();
    public DbSet<SalesOrderRecord> SalesOrders => Set<SalesOrderRecord>();
    public DbSet<SalesOrderItemRecord> SalesOrderItems => Set<SalesOrderItemRecord>();
    public DbSet<SalesOrderApprovalRecord> SalesOrderApprovals => Set<SalesOrderApprovalRecord>();
    public DbSet<DeliveryNoteRecord> DeliveryNotes => Set<DeliveryNoteRecord>();
    public DbSet<DeliveryNoteItemRecord> DeliveryNoteItems => Set<DeliveryNoteItemRecord>();
    public DbSet<DeliveryNoteItemAllocationRecord> DeliveryNoteItemAllocations => Set<DeliveryNoteItemAllocationRecord>();
    public DbSet<TaxCodeRecord> TaxCodes => Set<TaxCodeRecord>();
    public DbSet<InvoiceRecord> Invoices => Set<InvoiceRecord>();
    public DbSet<InvoiceItemRecord> InvoiceItems => Set<InvoiceItemRecord>();
    public DbSet<InvoiceItemAllocationRecord> InvoiceItemAllocations => Set<InvoiceItemAllocationRecord>();
    public DbSet<CurrentAccountRecord> CurrentAccounts => Set<CurrentAccountRecord>();
    public DbSet<CurrentTransactionRecord> CurrentTransactions => Set<CurrentTransactionRecord>();
    public DbSet<PaymentMethodRecord> PaymentMethods => Set<PaymentMethodRecord>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<PaymentAllocationRecord> PaymentAllocations => Set<PaymentAllocationRecord>();
    public DbSet<ShipmentRecord> Shipments => Set<ShipmentRecord>();
    public DbSet<ShipmentItemRecord> ShipmentItems => Set<ShipmentItemRecord>();
    public DbSet<ShipmentPackageRecord> ShipmentPackages => Set<ShipmentPackageRecord>();
    public DbSet<LoadPlanRecord> LoadPlans => Set<LoadPlanRecord>();
    public DbSet<LoadUnitRecord> LoadUnits => Set<LoadUnitRecord>();
    public DbSet<LoadUnitItemRecord> LoadUnitItems => Set<LoadUnitItemRecord>();
    public DbSet<LoadUnitStopAllocationRecord> LoadUnitStopAllocations => Set<LoadUnitStopAllocationRecord>();
    public DbSet<VehicleFitEvaluationRecord> VehicleFitEvaluations => Set<VehicleFitEvaluationRecord>();
    public DbSet<LoadPlanValidationResultRecord> LoadPlanValidationResults => Set<LoadPlanValidationResultRecord>();
    public DbSet<LoadPlanManualChangeRecord> LoadPlanManualChanges => Set<LoadPlanManualChangeRecord>();
    public DbSet<VehicleTypeRecord> VehicleTypes => Set<VehicleTypeRecord>();
    public DbSet<VehicleCapacityRecord> VehicleCapacities => Set<VehicleCapacityRecord>();
    public DbSet<VehicleRecord> Vehicles => Set<VehicleRecord>();
    public DbSet<DriverRecord> Drivers => Set<DriverRecord>();
    public DbSet<RoutePlanRecord> RoutePlans => Set<RoutePlanRecord>();
    public DbSet<RouteStopRecord> RouteStops => Set<RouteStopRecord>();
    public DbSet<ProductPhysicalProfileRecord> ProductPhysicalProfiles => Set<ProductPhysicalProfileRecord>();
    public DbSet<PackagingPhysicalProfileRecord> PackagingPhysicalProfiles => Set<PackagingPhysicalProfileRecord>();
    public DbSet<PalletTypeRecord> PalletTypes => Set<PalletTypeRecord>();
    public DbSet<VehicleCapacityPalletTypeRecord> VehicleCapacityPalletTypes => Set<VehicleCapacityPalletTypeRecord>();
    public DbSet<VehicleCapacityZoneRecord> VehicleCapacityZones => Set<VehicleCapacityZoneRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FactoryErpDbContext).Assembly);
    }
}
