# LMS Web — MudBlazor Admin Preview

The web frontend for the Laboratory Management System (LMS).

## Technology
- Blazor WebAssembly
- .NET 8
- MudBlazor 8.6

## Included screens
- Dashboard
- Students
- Rooms & PCs
- Vouchers
- Lab Sessions
- Reports
- Audit Logs
- Notifications
- Settings

## Run locally
Install the .NET 8 SDK, then run from this project directory:

```bash
dotnet restore
dotnet run
```

## Current project status
This is a frontend design preview. It currently uses sample records and does not connect to the LMS API or database. Some create/export actions are placeholders and should not be treated as saved operations.

## Project safety
Do not commit build output or local IDE files such as `bin/`, `obj/`, or `.vs/`.
