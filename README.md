# Service Desk

A multi-tenant IT service desk starter built with ASP.NET Core Razor Pages, C#, Entity Framework Core, and SQLite. It includes tenant-scoped ticket operations, a self-service portal, a service catalogue, knowledge article search, and SLA evaluation.

## Requirements

- .NET 8 SDK
- Windows, macOS, or Linux

## Run locally

From your project folder (`C:\Users\User\Desktop\CVTS\cvts project\service-desk`), run:

```powershell
Set-Location 'C:\Users\User\Desktop\CVTS\cvts project\service-desk'
dotnet run --project '.\src\ServiceDesk.Web\ServiceDesk.Web.csproj'
```

If `dotnet` is not on your `PATH` on Windows, run it with its full path:

```powershell
Set-Location 'C:\Users\User\Desktop\CVTS\cvts project\service-desk'
& 'C:\Program Files\dotnet\dotnet.exe' run --project '.\src\ServiceDesk.Web\ServiceDesk.Web.csproj'
```

The Development launch profile sets `ASPNETCORE_ENVIRONMENT=Development` and listens at <http://localhost:5080>. On first start, the host creates `service-desk.db` in the web project's content directory and inserts sample tenant, user context, groups, catalogue, knowledge, and SLA data.

## Pages

- `/Portal` — browse published catalogue items, search published knowledge articles, submit an incident, or submit a catalogue request.
- `/Tickets/Index` — agent queues for assigned tickets, group work, triage, and breached/approaching SLA.
- `/Tickets/Details?id={ticketId}` — ticket activity, group history, transfers, public replies, and internal notes.

The local Development authentication handler signs every request in as the seeded demo user. It is intended only to make the starter immediately runnable; it does not authenticate real users.

## Project structure

```text
src/
  ServiceDesk.Domain/          Domain entities, enums, and identity abstractions
  ServiceDesk.Infrastructure/  EF Core context and ticket, portal, and SLA services
  ServiceDesk.Web/             ASP.NET Core host and Razor Pages
```

## Configuration and host integration

The local database connection string is in `src/ServiceDesk.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=service-desk.db"
  }
}
```

The runnable host is deliberately Development-only. Before using this application outside a local development environment:

1. Replace the demo authentication handler with your identity provider and authorization policies.
2. Implement `ITenantContext`, `ICurrentAgent`, and `ICurrentRequester` from authenticated claims and trusted tenant membership data.
3. Configure a production database provider and protected connection string.
4. Use EF Core migrations and a deployment migration process instead of the development host's `EnsureCreated` initialization.
5. Review and configure tenant-specific catalogue, knowledge, triage, and SLA data.

The global EF Core query filters and tenant-aware foreign keys help isolate tenant data. Keep tenant resolution server-side; do not accept a tenant ID from an untrusted form or query parameter.

## SLA behavior

SLA policies match exactly on tenant (through the scoped context), customer, service/catalogue item, ticket type, and priority. A policy's `PausedStatusesJson` stores the statuses during which its clock pauses, for example:

```json
["WaitingForRequester"]
```

Tickets snapshot the matched policy and deadlines. Tickets without a matching policy return an explicit unconfigured evaluation and have no SLA deadlines or breach state. The current implementation uses elapsed calendar minutes, not business-hour calendars.

## Build

```powershell
dotnet build .\src\ServiceDesk.Web\ServiceDesk.Web.csproj
```
