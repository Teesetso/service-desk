using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;

namespace ServiceDesk.Domain.Entities;

public sealed class ResolverGroup : ITenantOwned
{
    [Key]
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("is_triage_group")]
    public bool IsTriageGroup { get; set; }

    public Tenant? Tenant { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public ICollection<CatalogueItem> CatalogueItems { get; set; } = new List<CatalogueItem>();
}
