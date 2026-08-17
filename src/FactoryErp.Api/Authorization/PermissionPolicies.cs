namespace FactoryErp.Api.Authorization;

public static class PermissionPolicies
{
    public const string SystemRead = "permission:system.read";
    public const string OrderCreate = "permission:order.create";
    public const string OrderRead = "permission:order.read";
    public const string OrderSubmit = "permission:order.submit";
    public const string OrderApprove = "permission:order.approve";
    public const string OrderReject = "permission:order.reject";
    public const string QuoteRequestSubmit = "permission:quote-request.submit";
    public const string QuoteRequestRead = "permission:quote-request.read";
    public const string QuoteRequestReview = "permission:quote-request.review";
    public const string DeliveryNoteCreate = "permission:delivery-note.create";
    public const string DeliveryNoteRead = "permission:delivery-note.read";
    public const string DeliveryNoteIssue = "permission:delivery-note.issue";
    public const string InvoiceCreate = "permission:invoice.create";
    public const string InvoiceRead = "permission:invoice.read";
    public const string InvoiceIssue = "permission:invoice.issue";
    public const string PaymentApply = "permission:payment.apply";
    public const string CurrentAccountRead = "permission:current-account.read";
    public const string ProductionCreate = "permission:production.create";
    public const string ProductionRead = "permission:production.read";
    public const string ProductionStart = "permission:production.start";
    public const string ProductionRecord = "permission:production.record";
    public const string ProductionComplete = "permission:production.complete";
    public const string StockTransferCreate = "permission:stock-transfer.create";
    public const string StockTransferRead = "permission:stock-transfer.read";
    public const string StockTransferComplete = "permission:stock-transfer.complete";
    public const string StockTransferCancel = "permission:stock-transfer.cancel";
    public const string VehicleTypeRead = "permission:vehicle-type.read";
    public const string VehicleTypeManage = "permission:vehicle-type.manage";
    public const string VehicleRead = "permission:vehicle.read";
    public const string VehicleManage = "permission:vehicle.manage";
    public const string VehicleStatusUpdate = "permission:vehicle.status-update";
    public const string DriverRead = "permission:driver.read";
    public const string DriverManage = "permission:driver.manage";
    public const string ShipmentCreate = "permission:shipment.create";
    public const string ShipmentRead = "permission:shipment.read";
    public const string ShipmentRouteManage = "permission:shipment.route-manage";
    public const string ShipmentRouteLock = "permission:shipment.route-lock";
    public const string ShipmentPlanReplan = "permission:shipment.plan-replan";
    public const string PhysicalProfileRead = "permission:physical-profile.read";
    public const string PhysicalProfileManage = "permission:physical-profile.manage";
    public const string PalletTypeRead = "permission:pallet-type.read";
    public const string PalletTypeManage = "permission:pallet-type.manage";
}
