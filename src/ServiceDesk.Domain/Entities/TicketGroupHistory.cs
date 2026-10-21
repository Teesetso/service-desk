using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;

namespace ServiceDesk.Domain.Entities;

public sealed class TicketGroupHistory : ITenantOwned
{
    [Key]
    [Column("history_id")]
    public Guid HistoryId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Column("from_group_id")]
    public Guid? FromGroupId { get; set; }

    [Required]
    [Column("to_group_id")]
    public Guid ToGroupId { get; set; }

    [Required]
    [Column("changed_by")]
    public Guid ChangedBy { get; set; }

    [Required]
    [Column("changed_at")]
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    public Ticket? Ticket { get; set; }

    public ResolverGroup? FromGroup { get; set; }

    public ResolverGroup? ToGroup { get; set; }
}
