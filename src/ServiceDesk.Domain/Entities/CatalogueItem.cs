using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class CatalogueItem : ITenantOwned
{
    [Key]
    [Column("item_id")]
    public Guid ItemId { get; set; }

    [NotMapped]
    public Guid ServiceId
    {
        get => ItemId;
        set => ItemId = value;
    }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    [Column("eligibility")]
    public string Eligibility { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    public CatalogueItemStatus Status { get; set; } = CatalogueItemStatus.Draft;

    [Required]
    [Column("fulfilment_group_id")]
    public Guid FulfilmentGroupId { get; set; }

    [Required]
    [Column("form_schema_json")]
    public string FormSchemaJson { get; set; } = "[]";

    public Tenant? Tenant { get; set; }

    public ResolverGroup? FulfilmentGroup { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public ICollection<SlaPolicy> SlaPolicies { get; set; } = new List<SlaPolicy>();
}
