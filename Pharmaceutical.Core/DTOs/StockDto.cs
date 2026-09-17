namespace Pharmaceutical.Core.DTOs;

public class StockTransactionDto
{
    public int TransactionId { get; set; }
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = null!;
    public int Quantity { get; set; }
    public string Operator { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Remark { get; set; }
}

public class StockInDto
{
    public string DrugId { get; set; } = null!;
    public int Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Remark { get; set; }
}

public class StockOutDto
{
    public string DrugId { get; set; } = null!;
    public int Quantity { get; set; }
    public string? Remark { get; set; }
}

public class StockAdjustDto
{
    public string DrugId { get; set; } = null!;
    public int Quantity { get; set; }
    public string? Remark { get; set; }
}

public class StockTransactionQueryDto
{
    public string? DrugId { get; set; }
    public string? TransactionType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class DrugBatchDto
{
    public int BatchId { get; set; }
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = null!;
    public DateTime? ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class ExpiryAlertDto
{
    public int BatchId { get; set; }
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = null!;
    public DateTime ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string AlertLevel { get; set; } = string.Empty;
}
