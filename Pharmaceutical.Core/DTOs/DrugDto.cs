namespace Pharmaceutical.Core.DTOs;

public class DrugDto
{
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = null!;
    public string TradeName { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
    public string ApprovalNum { get; set; } = string.Empty;
    public string StorageCond { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal RetailPrice { get; set; }
    public int StockQuantity { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class DrugCreateDto
{
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = null!;
    public string? TradeName { get; set; }
    public string? Specification { get; set; }
    public string? DosageForm { get; set; }
    public string? ApprovalNum { get; set; }
    public string? StorageCond { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal RetailPrice { get; set; }
    public int StockQuantity { get; set; }
    public int SupplierId { get; set; }
}

public class DrugUpdateDto
{
    public string DrugName { get; set; } = null!;
    public string? TradeName { get; set; }
    public string? Specification { get; set; }
    public string? DosageForm { get; set; }
    public string? ApprovalNum { get; set; }
    public string? StorageCond { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal RetailPrice { get; set; }
    public int SupplierId { get; set; }
}
