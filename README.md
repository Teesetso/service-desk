# Service Desk

A multi-tenant IT service desk demo built with ASP.NET Core and C#. It brings requester self-service and support-agent ticket workflows together, with tenant-scoped data access and SLA tracking.

**Tech:** .NET 8 · ASP.NET Core Razor Pages · Entity Framework Core · PostgreSQL/SQLite · Docker · Render

## What it does

- **Requester portal:** browse a service catalogue, search published knowledge articles, and submit incidents or service requests.
- **Agent workspace:** view assigned work, group queues, triage, and tickets with breached or approaching SLAs.
- **Ticket handling:** review activity and group history, transfer work, send public replies, and add internal notes.
- **SLA evaluation:** match policies by tenant, customer, service, ticket type, and priority; snapshot deadlines on tickets and support paused statuses.
- **Tenant-aware persistence:** use tenant-scoped EF Core query filters and tenant-aware relationships to help isolate tenant data.

Tickets without a matching SLA policy receive an explicit unconfigured evaluation and no SLA deadlines. SLA clocks currently use elapsed calendar minutes, not business hours.

## Screenshots

<<<<<<< HEAD
<img width="1351" height="635" alt="image" src="https://github.com/user-attachments/assets/0594b0aa-cc12-4539-8978-cd8bf067aeca" />
<img width="1357" height="635" alt="image" src="https://github.com/user-attachments/assets/d4234870-df0f-44ab-adad-4eae31810707" />
<img width="1349" height="630" alt="image" src="https://github.com/user-attachments/assets/8f36ac15-1045-4794-b40a-92b1f115da45" />
<img width="1357" height="637" alt="image" src="https://github.com/user-attachments/assets/be0e7ac0-ca7d-4177-bb6e-2d8fa70ce41e" />
<img width="1349" height="632" alt="image" src="https://github.com/user-attachments/assets/19646467-17fb-48cc-ad14-60ef0a45a118" />
<img width="1352" height="632" alt="image" src="https://github.com/user-attachments/assets/60eee642-0ed9-4614-98ee-ef71faf7c276" />

=======


>>>>>>> 12d129e (Update service desk application)
## Architecture

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
<<<<<<< HEAD

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

Resolve tenants server-side; do not accept a tenant ID from an untrusted form or query parameter.



=======
>>>>>>> 12d129e (Update service desk application)

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

Resolve tenants server-side; do not accept a tenant ID from an untrusted form or query parameter.
