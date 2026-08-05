# Production readiness

## Required configuration

Copy the values from `backend/appsettings.Production.example.json` into protected server configuration. Do not commit production credentials. Prefer environment variables or a server-only `appsettings.Production.json` whose access is limited to the application identity.

The application deliberately refuses to start outside Development when the database connection, deployed CORS origin, or HTTPS public URL is missing or still points to localhost.

## Database deployment

Never rely on automatic migrations in production. Build and review the generated migration, back up the database, and then run:

```powershell
dotnet ef database update --project backend/Theatre.Api.csproj --startup-project backend/Theatre.Api.csproj --configuration Release
```

For repeatable deployments, generate an EF migration bundle in CI and run that approved bundle once during deployment.

## Reverse proxy and HTTPS

Terminate HTTPS at IIS or the approved reverse proxy and forward `X-Forwarded-For` and `X-Forwarded-Proto`. Production enables HSTS, HTTPS redirection and secure admin cookies. Restrict the proxy so clients cannot directly spoof forwarded headers.

## Health monitoring

- `/health/live` confirms the API process is responding.
- `/health/ready` also verifies the database connection.

Monitor the readiness endpoint externally. The Super Admin System page shows safe recent-error summaries and correlation IDs; detailed exception logs remain server-side.

## Release checklist

1. Review and commit every required source file and migration.
2. Run backend and frontend release builds and all tests.
3. Run a dependency vulnerability scan.
4. Back up and verify both SQL Server and `backend/wwwroot/uploads`.
5. Apply migrations during a controlled deployment window.
6. Verify login, role restrictions, upload, edit, publish and public rendering in staging.
7. Test `/health/live` and `/health/ready`.
8. Perform a restore drill before launch and at least quarterly afterward.
