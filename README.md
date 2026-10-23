# LMS Web — MudBlazor Admin Preview

The web frontend for the Laboratory Management System (LMS).

## Technology
- Blazor WebAssembly
- .NET 8
- MudBlazor 8.6

## Included screens
- Admin login (Company ID + password)
- Dashboard (live counts, active sessions, quick actions)
- User Management (list, search, suspend / unsuspend, reset MFA) and Add New Student
- Computer Management (PC cards with live status) and Add New Computer
- Vouchers (generate batch, copy, cancel, CSV export)
- Lab Sessions (end an active session)
- Log Management, Notifications, Reports (attendance / usage / vouchers, CSV export)
- Settings (profile and password)
- Logout confirmation

## Run locally
Install the .NET 8 SDK, then run from this project directory:

```bash
dotnet restore
dotnet run
```

## Current project status
All screens above read from and write to the LMS API (`ApiBaseUrl` in `wwwroot/appsettings.json`). Start the API with its https profile, sign in as an admin, then run this project.

## Project safety
Do not commit build output or local IDE files such as `bin/`, `obj/`, or `.vs/`.
