namespace Pharmaceutical.Core;

public static class StockTransactionTypes
{
    public const string In = "IN";
    public const string Out = "OUT";
    public const string Adjust = "ADJUST";
    public const string Return = "RETURN";
    public const string Damage = "DAMAGE";
}

public static class PurchaseOrderStatus
{
    public const string Draft = "Draft";
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Received = "Received";
    public const string Cancelled = "Cancelled";
}
