using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class TicketActivity : ITenantOwned
{
    [Key]
    [Column("activity_id")]
    public Guid ActivityId { get; set; }

    [Required]
    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [Column("actor_id")]
    public Guid ActorId { get; set; }

    [Required]
    [Column("occurred_at")]
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    [Column("kind")]
    public TicketActivityKind Kind { get; set; }

    [Required]
    [Column("visibility")]
    public TicketActivityVisibility Visibility { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    public Tenant? Tenant { get; set; }

    public Ticket? Ticket { get; set; }
}
