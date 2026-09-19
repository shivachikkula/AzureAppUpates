# Azure Environment Variable Manager

A tool for developers to update connection strings on their assigned Azure App
Services, with a manager-approval gate for production, plus a live log viewer.

- **Frontend:** Angular 21 (standalone components, Angular Material, MSAL for
  Entra ID sign-in), in [`frontend/`](frontend).
- **Backend:** ASP.NET Core (.NET 8) Web API, in [`backend/`](backend).

## Features

- **My Apps** — each developer sees only the Azure App Services assigned to
  them.
- **Connection Strings** — pick an app from a dropdown, enter a key/value,
  and save.
  - Development/Staging apps are updated in Azure immediately.
  - Production apps instead create a **pending change request**.
- **Approvals** (Manager role) — review pending production change requests
  and approve (applies the change to Azure) or reject them. Scoped per
  team: a manager only sees/decides requests for teams they're assigned to
  manage; Admins see and decide everything.
- **Logs** — a separate page with an app dropdown that live-tails the
  selected App Service's log stream over SignalR.
- **Admin** (Admin role) — manage teams, the applications each team owns,
  which developers have access to which app, and which managers approve for
  which team.

## Architecture

```
frontend/                 Angular app
  src/app/core/            auth (MSAL), guards, HTTP services, layout shell
  src/app/features/        my-apps, connection-strings, approvals, logs, admin

backend/
  src/AzureEnvManager.Api/
    Controllers/           Applications, ConnectionStrings, Approvals, Admin
    Hubs/LogStreamHub.cs    SignalR hub clients subscribe to for live logs
    Services/
      AzureAppServiceClient    talks to Azure Resource Manager
      AppServiceLogStreamBroker  relays each app's Kudu log stream to its SignalR group
      ChangeRequestService       apply-now vs. pending-approval workflow, team-scoped decisions
      AppAccessService           who (developer) can see/edit which app
      TeamAccessService          who (manager) can decide requests for which team
      AdminService                CRUD for teams, applications, and both kinds of assignment
    Data/                   EF Core (SQLite) DbContext + local dev seed data
    Models/                 Domain entities + API DTOs
  tests/AzureEnvManager.Api.Tests/   xUnit tests (EF Core InMemory + mocked Azure client)
```

**Identity & access:** sign-in is via Microsoft Entra ID (Azure AD) using
MSAL on the frontend and JWT bearer validation (`Microsoft.Identity.Web`) on
the API. "Which apps can this developer touch" is modeled with an
`AppAssignment` table (per user, per app) in the API's own database — this
is metadata about *who may operate the tool*, separate from the Azure
resources themselves. A `Manager`/`Admin` Entra ID App Role gates the
Approvals endpoints; the `Admin` role alone gates `/api/admin/*`.

**Teams & per-team approval scoping:** every tracked `AzureApplication`
belongs to a `Team`. A `ManagerTeamAssignment` row grants a manager
(identified by Entra object id) the ability to see and decide production
change requests for one team's apps. `ChangeRequestService.GetPendingAsync`
filters to the caller's managed teams (via `ITeamAccessService`), and
`ApproveAsync`/`RejectAsync` re-check team membership before acting —
a manager who isn't on the owning team gets a 403, not just a hidden list
item. Anyone with the `Admin` role bypasses team scoping and can see/decide
every pending request.

**Admin UI:** `/admin` (Admin role only) has four tabs — Teams,
Applications, Developer Access, and Manager Access — backed by
`AdminController`/`AdminService`. This replaces hand-editing the database:
create a team, add an application to it, grant a developer access to an
app, and grant a manager approval rights over a team, all from the browser.

**Applying changes to Azure:** the API uses `Azure.ResourceManager.AppService`
with `DefaultAzureCredential`, so in Azure it runs as the API's managed
identity (grant it "Website Contributor," or a narrower custom role scoped
to `Microsoft.Web/sites/config/*`, on every app it manages). Locally it
falls back to your `az login` session.

**Approval workflow:** `POST /api/connection-strings` either applies the
change immediately (non-prod) or creates a `ChangeRequest` row with status
`PendingApproval` (prod). A manager of the owning team (or an Admin) calls
`POST /api/approvals/{id}/approve` (which then calls Azure and marks the
request `Applied` or `Failed`) or `.../reject`.

**Live logs:** the hub asks Azure for the app's SCM (Kudu) publishing
credentials and opens `https://{app}.scm.azurewebsites.net/api/logstream`,
then relays each line to the SignalR group named after the application id.
One stream per app runs while at least one browser is subscribed.

## Prerequisites

- Node.js 22.12+ and npm (frontend)
- .NET 8 SDK (backend) — **not installed in this sandbox**, so the backend
  and its tests could not be compiled or run here; run
  `dotnet test backend/AzureEnvManager.sln` locally before deploying.
- An Entra ID (Azure AD) tenant with two app registrations:
  1. **API app** — exposes a scope (e.g. `access_as_user`) and defines the
     `Developer`, `Manager` and `Admin` app roles.
  2. **SPA app** — the Angular app, with a redirect URI for your dev/prod
     hosting and `access_as_user` as an API permission.
- An Azure subscription with the App Services you want to manage, and a
  managed identity (or your `az login` principal for local dev) granted
  rights to read/write their config.

## Configuration

Frontend — fill in `frontend/src/environments/environment.ts` and
`environment.development.ts`:

```ts
entraId: {
  clientId: '<SPA app client id>',
  tenantId: '<tenant id>',
  apiScope: 'api://<API app id>/access_as_user',
  redirectUri: window.location.origin,
}
```

Backend — fill in `backend/src/AzureEnvManager.Api/appsettings.json`:

```json
"AzureAd": {
  "TenantId": "<tenant id>",
  "ClientId": "<API app client id>",
  "Audience": "api://<API app id>"
}
```

Sign in as a user with the `Admin` app role and use the **Admin** page to
create your teams, add your real App Services (with resource group,
subscription id, and environment), and grant developer/manager access.
`Data/SeedData.cs` only seeds one example team/app set for local dev.

## Running locally

```bash
# Backend (https://localhost:5001)
cd backend/src/AzureEnvManager.Api
dotnet run

# Backend tests
cd backend
dotnet test

# Frontend (http://localhost:4200)
cd frontend
npm install --legacy-peer-deps   # npm's resolver currently needs this flag for this dependency set
npm start
```

## Known gaps / next steps

- Team deletion isn't exposed in the Admin UI (a team with applications
  can't be deleted anyway, by design — reassign its apps first); this is a
  reasonable follow-up if teams need to be retired.
- Entra object ids are entered by hand in the Admin UI's assignment forms;
  wiring up Microsoft Graph people-search would make that friendlier.
- Backend tests cover `ChangeRequestService`, `AppAccessService`,
  `TeamAccessService`, and `AdminService` against an EF Core InMemory
  database with a mocked Azure client; none could be run in this sandbox
  without the .NET SDK — run `dotnet test` locally to verify before
  relying on this in production. There are no controller-level/integration
  tests yet (e.g. `WebApplicationFactory`), which would be the next layer
  to add.
