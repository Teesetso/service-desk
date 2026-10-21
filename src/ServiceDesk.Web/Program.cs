using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Data;
using ServiceDesk.Infrastructure.Tickets;
using ServiceDesk.Web.Authentication;
using ServiceDesk.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "This starter host is development-only. Configure production authentication, tenant context, " +
        "requester/agent identity services, and deployment settings before running outside Development.");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=service-desk.db";

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddRazorPages();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = LocalDevelopmentAuthHandler.SchemeName;
        options.DefaultChallengeScheme = LocalDevelopmentAuthHandler.SchemeName;
        options.DefaultForbidScheme = LocalDevelopmentAuthHandler.SchemeName;
    })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        LocalDevelopmentAuthHandler>(LocalDevelopmentAuthHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("Agents", policy => policy.RequireAuthenticatedUser()));

builder.Services.AddScoped<DevelopmentRequestContext>();
builder.Services.AddScoped<ITenantContext>(services =>
    services.GetRequiredService<DevelopmentRequestContext>());
builder.Services.AddScoped<ICurrentAgent>(services =>
    services.GetRequiredService<DevelopmentRequestContext>());
builder.Services.AddScoped<ICurrentRequester>(services =>
    services.GetRequiredService<DevelopmentRequestContext>());
builder.Services.AddTicketOperations();

var app = builder.Build();

await InitializeDevelopmentDatabaseAsync(app);

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/Portal"));
app.MapRazorPages();

await app.RunAsync();

static async Task InitializeDevelopmentDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    if (await dbContext.Tenants.AnyAsync())
    {
        return;
    }

    var tenant = new Tenant
    {
        TenantId = DevelopmentRequestContext.TenantIdValue,
        Name = "Local Development Tenant",
        Status = TenantStatus.Active,
        TimeZone = "UTC",
        Settings = "{}"
    };
    dbContext.Tenants.Add(tenant);
    await dbContext.SaveChangesAsync();

    var customer = new CustomerOrganisation
    {
        CustomerId = DevelopmentRequestContext.CustomerIdValue,
        TenantId = tenant.TenantId,
        Name = "Demo Customer",
        Status = CustomerOrganisationStatus.Active,
        PrimaryContactId = DevelopmentRequestContext.UserIdValue
    };
    var triageGroup = new ResolverGroup
    {
        GroupId = Guid.Parse("50d56e8d-2d35-4f2a-aac0-e6bfad1c2663"),
        TenantId = tenant.TenantId,
        Name = "Triage",
        Active = true,
        IsTriageGroup = true
    };
    var fulfilmentGroup = new ResolverGroup
    {
        GroupId = Guid.Parse("50d56e8d-2d35-4f2a-aac0-e6bfad1c2664"),
        TenantId = tenant.TenantId,
        Name = "Workplace Services",
        Active = true
    };

    dbContext.CustomerOrganisations.Add(customer);
    dbContext.ResolverGroups.AddRange(triageGroup, fulfilmentGroup);
    await dbContext.SaveChangesAsync();

    dbContext.GroupMembers.AddRange(
        new GroupMember
        {
            TenantId = tenant.TenantId,
            GroupId = triageGroup.GroupId,
            AgentUserId = DevelopmentRequestContext.UserIdValue
        },
        new GroupMember
        {
            TenantId = tenant.TenantId,
            GroupId = fulfilmentGroup.GroupId,
            AgentUserId = DevelopmentRequestContext.UserIdValue
        });
    dbContext.CatalogueItems.Add(new CatalogueItem
    {
        ItemId = Guid.Parse("60d56e8d-2d35-4f2a-aac0-e6bfad1c2663"),
        TenantId = tenant.TenantId,
        Title = "Request a new laptop",
        Description = "Request a standard laptop for a new starter or replacement.",
        Eligibility = "For active employees whose manager has approved the request.",
        Status = CatalogueItemStatus.Published,
        FulfilmentGroupId = fulfilmentGroup.GroupId,
        FormSchemaJson =
            "[{\"name\":\"employeeName\",\"label\":\"Employee name\",\"type\":\"text\",\"required\":true,\"maxLength\":150}," +
            "{\"name\":\"startDate\",\"label\":\"Required date\",\"type\":\"text\",\"required\":true,\"maxLength\":30}," +
            "{\"name\":\"managerEmail\",\"label\":\"Manager email\",\"type\":\"email\",\"required\":true,\"maxLength\":320}]"
    });
    dbContext.KnowledgeArticles.Add(new KnowledgeArticle
    {
        ArticleId = Guid.Parse("70d56e8d-2d35-4f2a-aac0-e6bfad1c2663"),
        TenantId = tenant.TenantId,
        Title = "Getting started with the service desk",
        Content = "Search the knowledge base, submit incidents, or select a published catalogue item to request a service.",
        Status = KnowledgeArticleStatus.Published,
        PublishedAt = DateTimeOffset.UtcNow
    });
    dbContext.SlaPolicies.Add(new SlaPolicy
    {
        PolicyId = Guid.Parse("80d56e8d-2d35-4f2a-aac0-e6bfad1c2663"),
        TenantId = tenant.TenantId,
        CustomerId = customer.CustomerId,
        ServiceId = null,
        Type = TicketType.Incident,
        Priority = TicketPriority.Normal,
        ResponseMinutes = 240,
        ResolutionMinutes = 2880,
        PausedStatusesJson = "[\"WaitingForRequester\"]"
    });
    dbContext.SlaPolicies.Add(new SlaPolicy
    {
        PolicyId = Guid.Parse("80d56e8d-2d35-4f2a-aac0-e6bfad1c2664"),
        TenantId = tenant.TenantId,
        CustomerId = customer.CustomerId,
        ServiceId = Guid.Parse("60d56e8d-2d35-4f2a-aac0-e6bfad1c2663"),
        Type = TicketType.ServiceRequest,
        Priority = TicketPriority.Normal,
        ResponseMinutes = 480,
        ResolutionMinutes = 5760,
        PausedStatusesJson = "[\"WaitingForRequester\"]"
    });
    await dbContext.SaveChangesAsync();
}
