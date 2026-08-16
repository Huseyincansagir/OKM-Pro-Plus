namespace FactoryErp.Domain.Sales;

public enum SalesOrderStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Preparing = 3,
    PartiallyShipped = 4,
    Fulfilled = 5,
    Completed = 6,
    Cancelled = 7
}
