using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Data;

public sealed class ApplicationDbContext : DbContext
{
    private readonly Guid _currentTenantId;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);
        var tenantId = tenantContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("The current tenant ID must be set.", nameof(tenantContext));
        }

        _currentTenantId = tenantId;
    }

    private Guid CurrentTenantId => _currentTenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<CustomerOrganisation> CustomerOrganisations => Set<CustomerOrganisation>();
    public DbSet<ResolverGroup> ResolverGroups => Set<ResolverGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketActivity> TicketActivities => Set<TicketActivity>();
    public DbSet<TicketGroupHistory> TicketGroupHistories => Set<TicketGroupHistory>();
    public DbSet<CatalogueItem> CatalogueItems => Set<CatalogueItem>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<TicketStatusHistory> TicketStatusHistories => Set<TicketStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>()
            .HasQueryFilter(tenant => tenant.TenantId == CurrentTenantId);

        ConfigureTenantFilter<CustomerOrganisation>(modelBuilder);
        ConfigureTenantFilter<ResolverGroup>(modelBuilder);
        ConfigureTenantFilter<GroupMember>(modelBuilder);
        ConfigureTenantFilter<Ticket>(modelBuilder);
        ConfigureTenantFilter<TicketActivity>(modelBuilder);
        ConfigureTenantFilter<TicketGroupHistory>(modelBuilder);
        ConfigureTenantFilter<CatalogueItem>(modelBuilder);
        ConfigureTenantFilter<KnowledgeArticle>(modelBuilder);
        ConfigureTenantFilter<SlaPolicy>(modelBuilder);
        ConfigureTenantFilter<TicketStatusHistory>(modelBuilder);

        modelBuilder.Entity<CustomerOrganisation>()
            .HasAlternateKey(customer => new { customer.TenantId, customer.CustomerId });

        modelBuilder.Entity<ResolverGroup>()
            .HasAlternateKey(group => new { group.TenantId, group.GroupId });

        modelBuilder.Entity<CatalogueItem>()
            .HasAlternateKey(item => new { item.TenantId, item.ItemId });

        modelBuilder.Entity<SlaPolicy>()
            .HasAlternateKey(policy => new { policy.TenantId, policy.PolicyId });

        modelBuilder.Entity<Ticket>()
            .HasIndex(ticket => new { ticket.TenantId, ticket.Reference })
            .IsUnique();

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(member => new { member.TenantId, member.GroupId, member.AgentUserId });
            entity.HasOne(member => member.Tenant)
                .WithMany(tenant => tenant.GroupMembers)
                .HasForeignKey(member => member.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(member => member.Group)
                .WithMany(group => group.Members)
                .HasForeignKey(member => new { member.TenantId, member.GroupId })
                .HasPrincipalKey(group => new { group.TenantId, group.GroupId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasAlternateKey(ticket => new { ticket.TenantId, ticket.TicketId });

            entity.HasOne(ticket => ticket.Tenant)
                .WithMany(tenant => tenant.Tickets)
                .HasForeignKey(ticket => ticket.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.Customer)
                .WithMany(customer => customer.Tickets)
                .HasForeignKey(ticket => new { ticket.TenantId, ticket.CustomerId })
                .HasPrincipalKey(customer => new { customer.TenantId, customer.CustomerId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.Group)
                .WithMany(group => group.Tickets)
                .HasForeignKey(ticket => new { ticket.TenantId, ticket.GroupId })
                .HasPrincipalKey(group => new { group.TenantId, group.GroupId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.CatalogueItem)
                .WithMany(item => item.Tickets)
                .HasForeignKey(ticket => new { ticket.TenantId, ticket.CatalogueItemId })
                .HasPrincipalKey(item => new { item.TenantId, item.ItemId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(ticket => ticket.StatusHistory)
                .WithOne(history => history.Ticket)
                .HasForeignKey(history => new { history.TenantId, history.TicketId })
                .HasPrincipalKey(ticket => new { ticket.TenantId, ticket.TicketId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerOrganisation>()
            .HasOne(customer => customer.Tenant)
            .WithMany(tenant => tenant.CustomerOrganisations)
            .HasForeignKey(customer => customer.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResolverGroup>()
            .HasOne(group => group.Tenant)
            .WithMany(tenant => tenant.ResolverGroups)
            .HasForeignKey(group => group.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketActivity>(entity =>
        {
            entity.HasOne(activity => activity.Ticket)
                .WithMany(ticket => ticket.Activities)
                .HasForeignKey(activity => new { activity.TenantId, activity.TicketId })
                .HasPrincipalKey(ticket => new { ticket.TenantId, ticket.TicketId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(activity => activity.Tenant)
                .WithMany(tenant => tenant.TicketActivities)
                .HasForeignKey(activity => activity.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(activity => new { activity.TenantId, activity.TicketId, activity.OccurredAt });
        });

        modelBuilder.Entity<TicketGroupHistory>(entity =>
        {
            entity.HasOne(history => history.Ticket)
                .WithMany(ticket => ticket.GroupHistory)
                .HasForeignKey(history => new { history.TenantId, history.TicketId })
                .HasPrincipalKey(ticket => new { ticket.TenantId, ticket.TicketId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(history => history.FromGroup)
                .WithMany()
                .HasForeignKey(history => new { history.TenantId, history.FromGroupId })
                .HasPrincipalKey(group => new { group.TenantId, group.GroupId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(history => history.ToGroup)
                .WithMany()
                .HasForeignKey(history => new { history.TenantId, history.ToGroupId })
                .HasPrincipalKey(group => new { group.TenantId, group.GroupId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(history => new { history.TenantId, history.TicketId, history.ChangedAt });
        });

        modelBuilder.Entity<CatalogueItem>(entity =>
        {
            entity.HasOne(item => item.Tenant)
                .WithMany(tenant => tenant.CatalogueItems)
                .HasForeignKey(item => item.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.FulfilmentGroup)
                .WithMany(group => group.CatalogueItems)
                .HasForeignKey(item => new { item.TenantId, item.FulfilmentGroupId })
                .HasPrincipalKey(group => new { group.TenantId, group.GroupId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(item => new { item.TenantId, item.Status, item.Title });
        });

        modelBuilder.Entity<KnowledgeArticle>()
            .HasOne<Tenant>()
            .WithMany(tenant => tenant.KnowledgeArticles)
            .HasForeignKey(article => article.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SlaPolicy>(entity =>
        {
            entity.HasOne(policy => policy.Tenant)
                .WithMany(tenant => tenant.SlaPolicies)
                .HasForeignKey(policy => policy.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(policy => policy.Customer)
                .WithMany(customer => customer.SlaPolicies)
                .HasForeignKey(policy => new { policy.TenantId, policy.CustomerId })
                .HasPrincipalKey(customer => new { customer.TenantId, customer.CustomerId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(policy => policy.Service)
                .WithMany(item => item.SlaPolicies)
                .HasForeignKey(policy => new { policy.TenantId, policy.ServiceId })
                .HasPrincipalKey(item => new { item.TenantId, item.ItemId })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(policy => new
            {
                policy.TenantId,
                policy.CustomerId,
                policy.ServiceId,
                policy.Type,
                policy.Priority
            }).IsUnique();
        });

        modelBuilder.Entity<TicketStatusHistory>()
            .HasIndex(history => new { history.TenantId, history.TicketId, history.EnteredAt });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantOwnership();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateTenantOwnership();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ConfigureTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantOwned
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
    }

    private void ValidateTenantOwnership()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId == Guid.Empty)
                {
                    entry.Entity.TenantId = CurrentTenantId;
                }
                else if (entry.Entity.TenantId != CurrentTenantId)
                {
                    throw new InvalidOperationException("Cannot add an entity for a different tenant.");
                }

                continue;
            }

            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                var originalTenantId = (Guid)entry.Property(nameof(ITenantOwned.TenantId)).OriginalValue!;
                if (originalTenantId != CurrentTenantId || entry.Entity.TenantId != originalTenantId)
                {
                    throw new InvalidOperationException(
                        "Cannot modify or delete an entity outside the current tenant.");
                }
            }
        }

        foreach (var entry in ChangeTracker.Entries<Tenant>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId != CurrentTenantId)
                {
                    throw new InvalidOperationException("Cannot modify a different tenant.");
                }
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                var originalTenantId = entry.Property(tenant => tenant.TenantId).OriginalValue;
                if (originalTenantId != CurrentTenantId || entry.Entity.TenantId != originalTenantId)
                {
                    throw new InvalidOperationException("Cannot modify a different tenant.");
                }
            }
        }
    }
}
