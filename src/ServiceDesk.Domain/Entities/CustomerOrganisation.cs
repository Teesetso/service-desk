using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class CustomerOrganisation : ITenantOwned
{
    [Key]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    public CustomerOrganisationStatus Status { get; set; } = CustomerOrganisationStatus.Active;

    [Column("primary_contact_id")]
    public Guid? PrimaryContactId { get; set; }

    public Tenant? Tenant { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public ICollection<SlaPolicy> SlaPolicies { get; set; } = new List<SlaPolicy>();
}
