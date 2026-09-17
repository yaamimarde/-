using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pharmaceutical.Core;

[Table("purchase_orders")]
public class PurchaseOrderEntity
{
    [Key]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [Column("status")]
    public string Status { get; set; } = PurchaseOrderStatus.Draft;

    [Column("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    public SupplierEntity? Supplier { get; set; }
    public List<PurchaseOrderLineEntity> Lines { get; set; } = new();
}

[Table("purchase_order_lines")]
public class PurchaseOrderLineEntity
{
    [Key]
    [Column("line_id")]
    public int LineId { get; set; }

    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("drug_id")]
    public string DrugId { get; set; } = null!;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("received_quantity")]
    public int ReceivedQuantity { get; set; }

    public PurchaseOrderEntity? Order { get; set; }
    public DrugCatalogEntity? Drug { get; set; }
}
