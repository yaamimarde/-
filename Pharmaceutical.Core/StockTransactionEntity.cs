using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pharmaceutical.Core;

[Table("stock_transactions")]
public class StockTransactionEntity
{
    [Key]
    [Column("transaction_id")]
    public int TransactionId { get; set; }

    [Required]
    [Column("drug_id")]
    public string DrugId { get; set; } = null!;

    [Required]
    [Column("transaction_type")]
    public string TransactionType { get; set; } = null!;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("operator_name")]
    public string Operator { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("remark")]
    public string? Remark { get; set; }

    public DrugCatalogEntity? Drug { get; set; }
}
