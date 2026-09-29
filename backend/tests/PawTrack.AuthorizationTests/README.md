# PawTrack.AuthorizationTests

This project is the local authorization baseline. It uses xUnit and the existing ASP.NET Core test host pattern, with a unique EF Core InMemory database per factory instance. It does not connect to production, Azure or a shared database.

## Current coverage

- Anonymous access to an administrative endpoint is rejected with `401`.
- The public map route is inspected as an explicit public exception.
- Endpoint discovery is the foundation for the complete role, policy, ownership, tenant and plan matrix.

## Run

```powershell
dotnet test backend/tests/PawTrack.AuthorizationTests/PawTrack.AuthorizationTests.csproj
```

Do not add `Skip` to hide an authorization failure and do not remove authorization metadata to make this project pass. SQL Server-specific behavior is not certified by this InMemory fixture; a disposable local SQL Server fixture is a separate follow-up.
