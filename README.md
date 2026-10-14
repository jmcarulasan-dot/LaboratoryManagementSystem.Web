# LMS Web Admin

Blazor WebAssembly admin client for the Laboratory Management System. It follows the SVS separation of screens, state/data access, and services in the form that fits Blazor: Razor pages and components call a registered `ApiClient`, which sends requests to the LMS API. The API alone reads and writes the SQL Server database.

## Technology

- Blazor WebAssembly and .NET 8
- MudBlazor 8
- LMS ASP.NET Core API with JWT bearer authentication

## Run locally

1. Configure the API database connection and private `Jwt:Key` as described in the API repository README.
2. Start the LMS API.
3. Confirm `wwwroot/appsettings.json` has the API URL for the local API.
4. Run `dotnet restore` and `dotnet run` from this project directory.
5. Sign in with an existing admin account.

The login page calls `POST /api/login/admin`. The JWT is kept in browser session storage and sent as a Bearer token on protected requests. The API validates the signature, issuer, audience, expiry, and Admin role. A 401 clears the session and returns the user to sign in.

## Database-backed screens

The dashboard, student list, rooms, PCs, vouchers, sessions, reports, audit logs, notifications, and admin profile load from API endpoints backed by the LMS database. Pages show empty tables and empty-state messages when the database returns no records; the web client does not seed or substitute sample records. Dashboard totals come from the API summary and are zero when the database has no rows.

Read views are connected to the API. Management actions not represented by a working API form remain unavailable in the web client.

## Deployment

Set `ApiBaseUrl` in the web app's `wwwroot/appsettings.json` to the deployed API URL. Configure the API's `Cors:AllowedOrigins` entries for the deployed web origin and provide the JWT signing key through the hosting provider's secret configuration. Do not commit private signing keys.
