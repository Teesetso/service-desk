using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;

namespace ServiceDesk.Domain.Entities;

public sealed class AppUser : ITenantOwned
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(100)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(400)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Column("role")]
    public string Role { get; set; } = string.Empty;

    [Column("active")]
    public bool Active { get; set; } = true;
}
