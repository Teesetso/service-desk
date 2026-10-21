using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class Tenant
{
    [Key]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    [Required]
    [StringLength(100)]
    [Column("time_zone")]
    public string TimeZone { get; set; } = "UTC";

    [Required]
    [Column("settings")]
    public string Settings { get; set; } = "{}";

    public ICollection<CustomerOrganisation> CustomerOrganisations { get; set; } =
        new List<CustomerOrganisation>();

    public ICollection<ResolverGroup> ResolverGroups { get; set; } = new List<ResolverGroup>();

    public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public ICollection<TicketActivity> TicketActivities { get; set; } = new List<TicketActivity>();

    public ICollection<CatalogueItem> CatalogueItems { get; set; } = new List<CatalogueItem>();

    public ICollection<KnowledgeArticle> KnowledgeArticles { get; set; } =
        new List<KnowledgeArticle>();

    public ICollection<SlaPolicy> SlaPolicies { get; set; } = new List<SlaPolicy>();
}
