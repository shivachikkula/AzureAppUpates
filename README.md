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
  and approve (applies the change to Azure) or reject them.
- **Logs** — a separate page with an app dropdown that live-tails the
  selected App Service's log stream over SignalR.

## Architecture

```
frontend/                 Angular app
  src/app/core/            auth (MSAL), guards, HTTP services, layout shell
  src/app/features/        my-apps, connection-strings, approvals, logs

backend/
  src/AzureEnvManager.Api/
    Controllers/           Applications, ConnectionStrings, Approvals
    Hubs/LogStreamHub.cs    SignalR hub clients subscribe to for live logs
    Services/
      AzureAppServiceClient    talks to Azure Resource Manager
      AppServiceLogStreamBroker  relays each app's Kudu log stream to its SignalR group
      ChangeRequestService       apply-now vs. pending-approval workflow
      AppAccessService           who can see/edit which app
    Data/                   EF Core (SQLite) DbContext + local dev seed data
    Models/                 Domain entities + API DTOs
```

**Identity & access:** sign-in is via Microsoft Entra ID (Azure AD) using
MSAL on the frontend and JWT bearer validation (`Microsoft.Identity.Web`) on
the API. "Which apps can this developer touch" is modeled with an
`AppAssignment` table (per user, per app) in the API's own database — this
is metadata about *who may operate the tool*, separate from the Azure
resources themselves. A `Manager`/`Admin` Entra ID App Role gates the
Approvals endpoints.

**Applying changes to Azure:** the API uses `Azure.ResourceManager.AppService`
with `DefaultAzureCredential`, so in Azure it runs as the API's managed
identity (grant it "Website Contributor," or a narrower custom role scoped
to `Microsoft.Web/sites/config/*`, on every app it manages). Locally it
falls back to your `az login` session.

**Approval workflow:** `POST /api/connection-strings` either applies the
change immediately (non-prod) or creates a `ChangeRequest` row with status
`PendingApproval` (prod). A manager calls
`POST /api/approvals/{id}/approve` (which then calls Azure and marks the
request `Applied` or `Failed`) or `.../reject`.

**Live logs:** the hub asks Azure for the app's SCM (Kudu) publishing
credentials and opens `https://{app}.scm.azurewebsites.net/api/logstream`,
then relays each line to the SignalR group named after the application id.
One stream per app runs while at least one browser is subscribed.

## Prerequisites

- Node.js 22.12+ and npm (frontend)
- .NET 8 SDK (backend) — **not installed in this sandbox**, so the backend
  could not be compiled or run here; review it locally with
  `dotnet build` before deploying.
- An Entra ID (Azure AD) tenant with two app registrations:
  1. **API app** — exposes a scope (e.g. `access_as_user`) and defines the
     `Developer`, `Manager` (and optionally `Admin`) app roles.
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

Applications and per-developer assignments are rows in the `Applications` /
`Assignments` tables — add your real App Service names, resource groups and
subscription id there (a small admin UI or seed script is a natural next
step; for now `Data/SeedData.cs` shows the shape with placeholder apps).

## Running locally

```bash
# Backend (https://localhost:5001)
cd backend/src/AzureEnvManager.Api
dotnet run

# Frontend (http://localhost:4200)
cd frontend
npm install --legacy-peer-deps   # npm's resolver currently needs this flag for this dependency set
npm start
```

## Known gaps / next steps

- Applications and assignments are managed directly in the database; there's
  no admin UI yet for a manager to assign apps to developers.
- The Manager role is global (any Manager can approve any pending request);
  scoping approvers to specific resource groups/teams is a reasonable
  follow-up.
- No automated tests were added for the backend (none could be run in this
  sandbox without the .NET SDK); add unit tests for `ChangeRequestService`
  and an integration test for the approval flow before relying on this in
  production.
