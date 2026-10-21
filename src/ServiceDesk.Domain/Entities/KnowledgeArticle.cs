using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

public sealed class KnowledgeArticle : ITenantOwned
{
    [Key]
    [Column("article_id")]
    public Guid ArticleId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(250)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    public KnowledgeArticleStatus Status { get; set; } = KnowledgeArticleStatus.Draft;

    [Column("published_at")]
    public DateTimeOffset? PublishedAt { get; set; }
}
