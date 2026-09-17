using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pharmaceutical.Core;

[Table("audit_logs")]
public class AuditLogEntity
{
    [Key]
    [Column("audit_id")]
    public long AuditId { get; set; }

    [Column("entity_type")]
    public string EntityType { get; set; } = null!;

    [Column("entity_id")]
    public string EntityId { get; set; } = null!;

    [Column("action")]
    public string Action { get; set; } = null!;

    [Column("user_name")]
    public string UserName { get; set; } = string.Empty;

    [Column("old_values")]
    public string? OldValues { get; set; }

    [Column("new_values")]
    public string? NewValues { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
