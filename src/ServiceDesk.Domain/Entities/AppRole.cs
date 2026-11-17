using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;

namespace ServiceDesk.Domain.Entities;

public sealed class AppRole : ITenantOwned
{
    [Key]
    [Column("role_id")]
    public Guid RoleId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;
}
