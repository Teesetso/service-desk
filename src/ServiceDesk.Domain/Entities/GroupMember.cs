using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;

namespace ServiceDesk.Domain.Entities;

public sealed class GroupMember : ITenantOwned
{
    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Required]
    [Column("agent_user_id")]
    public Guid AgentUserId { get; set; }

    public Tenant? Tenant { get; set; }

    public ResolverGroup? Group { get; set; }
}
