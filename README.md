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

## Public demo on Render

This repository includes a Render Blueprint and Dockerfile for a public, non-sensitive demo:

1. Sign in to [Render](https://render.com/) and connect your GitHub account.
2. Create a new Blueprint instance and select this repository.
3. Review the `service-desk-demo` service from `render.yaml` and deploy it. Render will provide the public URL when deployment completes.

The Blueprint explicitly enables `ServiceDesk:PublicDemo` while keeping ASP.NET Core in Production mode. This uses the shared demo identity; it is **not authentication**. Anyone with the URL can act as the demo agent/requester, so do not enter real or private information.

The free Render service can sleep when idle, and its filesystem is ephemeral. The SQLite database may be reset after sleep, restart, or redeploy. Use this setup only to preview the app, not for production or data that must persist.

## Project structure

```text
src/
  ServiceDesk.Domain/          Domain entities, enums, and identity abstractions
  ServiceDesk.Infrastructure/  EF Core context and ticket, portal, and SLA services
  ServiceDesk.Web/             ASP.NET Core host and Razor Pages
```

The separation keeps domain concepts and application services distinct from the web host and persistence concerns.

## Run locally

**Requirements:** .NET 8 SDK

From the repository root:

```powershell
Copy-Item .env.example .env
# Edit .env and paste your Neon connection values
dotnet run --project .\src\ServiceDesk.Web\ServiceDesk.Web.csproj
```

Neon is the recommended hosted database for this application because it provides standard PostgreSQL, which is supported by the Npgsql EF Core provider. On first start, the app creates the schema and seeds demo tenants, users, groups, catalogue items, knowledge articles, and SLA data. If no PostgreSQL connection string is configured, it falls back to the local SQLite database.

Use the pooled Neon connection string from the Neon dashboard. It should include `SSL Mode=Require`.

Sign in at <http://localhost:5080/Login> with one of these demo accounts:

| Workspace | Username | Password | URL |
| --- | --- | --- | --- |
| Customer portal | `client` | `client123` | <http://localhost:5080/Portal> |
| Service agent | `agent` | `agent123` | <http://localhost:5080/Tickets> |
| Administrator | `admin` | `admin123` | <http://localhost:5080/Admin> |

Administrators can create roles, create additional users, and update user roles from the administrator dashboard. These credentials are for local demonstration only.

Build the web project with:

```powershell
dotnet build .\src\ServiceDesk.Web\ServiceDesk.Web.csproj
```
docker build -t service-desk .
docker run -p 10000:10000 service-desk
## Main pages

| Route | Purpose |
| --- | --- |
| `/Portal` | Requester catalogue, knowledge search, incident submission, and service requests |
| `/Tickets/Index` | Agent queues for assigned tickets, group work, triage, and SLA status |
| `/Tickets/Details?id={ticketId}` | Ticket activity, group history, transfers, public replies, and internal notes |

## Public demo deployment

The repository includes a [Render Blueprint](./render.yaml) and [Dockerfile](./Dockerfile) for deploying a public demo. Connect the repository to Render and create a Blueprint instance to deploy it.

> **Demo only:** The public demo uses a shared identity and does not authenticate individual users. Anyone with the URL can act as the demo agent or requester. Do not enter real or private information. The free Render service may sleep while idle, and its filesystem is ephemeral, so the SQLite database can be reset after a restart, sleep, or redeploy. This deployment is for previewing the app, not production use.

## Security and production notes

The local Development authentication handler signs requests in as a seeded demo user. The public demo has an explicit opt-in while ASP.NET Core remains in Production mode, but it still uses the shared demo identity described above.

Before using this application with real users or outside the public demo:

1. Replace demo authentication with an identity provider and appropriate authorization policies.
2. Implement `ITenantContext`, `ICurrentAgent`, and `ICurrentRequester` using authenticated claims and trusted tenant membership data.
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
<img width="1366" height="768" alt="image" src="https://github.com/user-attachments/assets/df40096f-fdc9-479f-93ce-492fe033a654" />
<img width="1366" height="768" alt="image" src="https://github.com/user-attachments/assets/ccd4e5fc-13fc-4af7-89dc-736de63f66e3" />

