using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class SlaPolicy : ITenantOwned
{
    [Key]
    [Column("policy_id")]
    public Guid PolicyId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("service_id")]
    public Guid? ServiceId { get; set; }

    [Required]
    [Column("type")]
    public TicketType Type { get; set; }

    [Required]
    [Column("priority")]
    public TicketPriority Priority { get; set; }

    [Range(1, 525600)]
    [Column("response_minutes")]
    public int ResponseMinutes { get; set; }

    [Range(1, 525600)]
    [Column("resolution_minutes")]
    public int ResolutionMinutes { get; set; }

    [Required]
    [Column("paused_statuses_json")]
    public string PausedStatusesJson { get; set; } = "[]";

    public Tenant? Tenant { get; set; }

    public CustomerOrganisation? Customer { get; set; }

    public CatalogueItem? Service { get; set; }
}
