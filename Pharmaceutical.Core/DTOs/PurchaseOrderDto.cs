namespace Pharmaceutical.Core.DTOs;

public class PurchaseOrderDto
{
    public int OrderId { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Status { get; set; } = null!;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
}

public class PurchaseOrderLineDto
{
    public int LineId { get; set; }
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
}

public class CreatePurchaseOrderDto
{
    public int SupplierId { get; set; }
    public List<PurchaseOrderLineCreateDto> Lines { get; set; } = new();
}

public class PurchaseOrderLineCreateDto
{
    public string DrugId { get; set; } = null!;
    public int Quantity { get; set; }
}

public class ReceivePurchaseOrderDto
{
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class DashboardDto
{
    public decimal TotalInventoryValue { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiryAlertCount { get; set; }
    public int ActiveDrugCount { get; set; }
    public int PendingPurchaseOrders { get; set; }
    public List<DrugMovementDto> TopMovingDrugs { get; set; } = new();
    public List<ReorderSuggestionDto> ReorderSuggestions { get; set; } = new();
}

public class DrugMovementDto
{
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = null!;
    public int TotalOutQuantity { get; set; }
}

public class ReorderSuggestionDto
{
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = null!;
    public int CurrentStock { get; set; }
    public double DailyAverageOut { get; set; }
    public int SuggestedReorderQuantity { get; set; }
    public int DaysUntilStockout { get; set; }
}

public class AuditLogDto
{
    public long AuditId { get; set; }
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string UserName { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime CreatedAt { get; set; }
}
