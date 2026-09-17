using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pharmaceutical.Core;

[Table("suppliers")]
public class SupplierEntity
{
    [Key]
    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("contact_person")]
    public string ContactPerson { get; set; } = string.Empty;

    [Column("phone")]
    public string Phone { get; set; } = string.Empty;

    [Column("address")]
    public string Address { get; set; } = string.Empty;
}
