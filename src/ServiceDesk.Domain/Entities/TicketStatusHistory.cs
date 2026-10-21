using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class TicketStatusHistory : ITenantOwned
{
    [Key]
    [Column("status_history_id")]
    public Guid StatusHistoryId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Required]
    [Column("status")]
    public TicketStatus Status { get; set; }

    [Required]
    [Column("entered_at")]
    public DateTimeOffset EnteredAt { get; set; }

    [Required]
    [Column("changed_by")]
    public Guid ChangedBy { get; set; }

    public Ticket? Ticket { get; set; }
}
