using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class Ticket : ITenantOwned
{
    [Key]
    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("reference")]
    public string Reference { get; set; } = string.Empty;

    [Required]
    [Column("type")]
    public TicketType Type { get; set; }

    [Required]
    [StringLength(300)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    public TicketStatus Status { get; set; } = TicketStatus.New;

    [Required]
    [Column("priority")]
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("group_id")]
    public Guid? GroupId { get; set; }

    [Column("assignee_user_id")]
    public Guid? AssigneeUserId { get; set; }

    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("sla_due_at")]
    public DateTimeOffset? SlaDueAt { get; set; }

    [Column("resolved_at")]
    public DateTimeOffset? ResolvedAt { get; set; }

    [StringLength(4000)]
    [Column("resolution_summary")]
    public string? ResolutionSummary { get; set; }

    [Column("catalogue_item_id")]
    public Guid? CatalogueItemId { get; set; }

    [StringLength(200)]
    [Column("catalogue_item_title_snapshot")]
    public string? CatalogueItemTitleSnapshot { get; set; }

    [Column("request_answers_json")]
    public string? RequestAnswersJson { get; set; }

    [Column("sla_policy_id")]
    public Guid? SlaPolicyId { get; set; }

    [Column("response_due_at")]
    public DateTimeOffset? ResponseDueAt { get; set; }

    [Column("resolution_due_at")]
    public DateTimeOffset? ResolutionDueAt { get; set; }

    [Column("sla_paused_at")]
    public DateTimeOffset? SlaPausedAt { get; set; }

    [Required]
    [Column("sla_paused_total_ticks")]
    public long SlaPausedTotalTicks { get; set; }

    [Required]
    [Column("sla_pause_statuses_json")]
    public string SlaPauseStatusesJson { get; set; } = "[]";

    [Column("first_response_at")]
    public DateTimeOffset? FirstResponseAt { get; set; }

    [Column("response_breached_at")]
    public DateTimeOffset? ResponseBreachedAt { get; set; }

    [Column("resolution_breached_at")]
    public DateTimeOffset? ResolutionBreachedAt { get; set; }

    public Tenant? Tenant { get; set; }

    public CustomerOrganisation? Customer { get; set; }

    public ResolverGroup? Group { get; set; }

    public CatalogueItem? CatalogueItem { get; set; }

    public ICollection<TicketActivity> Activities { get; set; } = new List<TicketActivity>();

    public ICollection<TicketGroupHistory> GroupHistory { get; set; } = new List<TicketGroupHistory>();

    public ICollection<TicketStatusHistory> StatusHistory { get; set; } =
        new List<TicketStatusHistory>();
}
