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
}
