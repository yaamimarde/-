using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pharmaceutical.Core;

[Table("drug_batches")]
public class DrugBatchEntity
{
    [Key]
    [Column("batch_id")]
    public int BatchId { get; set; }

    [Required]
    [Column("drug_id")]
    public string DrugId { get; set; } = null!;

    [Required]
    [Column("batch_number")]
    public string BatchNumber { get; set; } = null!;

    [Column("manufacture_date")]
    public DateTime? ManufactureDate { get; set; }

    [Column("expiry_date")]
    public DateTime ExpiryDate { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    public DrugCatalogEntity? Drug { get; set; }
}
