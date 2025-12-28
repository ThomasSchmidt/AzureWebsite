## Purpose

This file tells AI coding agents how this repository is organized, how to build/run it, and which project-specific patterns to follow when changing code.

## Big picture

- **What:** An ASP.NET Core MVC site primarily in `src/Website` with server-rendered Razor views, controllers, and simple app settings. The solution file is `AzureWebsite.sln`.
- **Runtime:** Uses minimal `Program.cs` (WebApplication builder). Key middleware: Application Insights, HealthChecks (`/healthcheck`), Output Cache, Authentication/Authorization, Static Files, and default controller routing.
- **Tests:** Unit tests live in `test/AzureWebsite.Tests` and use a separate test project (`Website.Tests.csproj`).

## Key files to inspect (examples)

- Startup and middleware: [src/Website/Program.cs](src/Website/Program.cs#L1-L120)
- Example controller + caching: [src/Website/Controllers/HomeController.cs](src/Website/Controllers/HomeController.cs#L1-L120)
- Config model: [src/Website/Infrastructure/Settings.cs](src/Website/Infrastructure/Settings.cs#L1-L200)
- Solution README: [README.md](README.md#L1-L20)
- Tests: [test/AzureWebsite.Tests/Website.Tests.csproj](test/AzureWebsite.Tests/Website.Tests.csproj)

## Project-specific conventions and gotchas

- Namespace inconsistencies: files use both `AzureWebsite` and `Website` namespaces. Match the surrounding files' namespace when adding new files to avoid compile-time surprises.
- Configuration: environment-specific files live in `src/Website` as `appsettings*.json`. Prefer reading configuration via bound POCOs (see `Settings` and `IOptions<Settings>` usage in controllers).
- Caching: Output caching is enabled globally and used via attribute `[OutputCache(Duration = 6000)]` on controllers. Be mindful when changing dynamic content.
- Routing: The app uses the default controller route (`MapDefaultControllerRoute`). Assume conventional MVC routes unless modifying `Program.cs` routing.
- Telemetry/CI: Application Insights is registered in startup; do not remove telemetry registration without reason.

## Build / Run / Test workflows

- Build solution: `dotnet build AzureWebsite.sln`
- Build website only: `dotnet build src/Website/Website.csproj`
- Run locally (watch): `dotnet watch run --project src/Website/Website.csproj` or use the workspace task labeled `watch`.
- Publish: `dotnet publish src/Website/Website.csproj -c Release -o ./out` or use the workspace `publish` task.
- Run tests: `dotnet test test/AzureWebsite.Tests/Website.Tests.csproj` or `dotnet test AzureWebsite.sln`.
- Note: VS Code tasks exist for build/publish/watch (check the workspace `Tasks` panel).

## When you modify code

- Update both `src/Website` and tests in `test/AzureWebsite.Tests` where appropriate. Run `dotnet test` after changing controller logic or configuration binding.
- Preserve middleware ordering in `Program.cs` (StaticFiles -> Routing -> OutputCache -> Auth -> AuthZ -> Endpoints). Changes to ordering can change behavior.
- Respect existing views under `src/Website/Views` and `Views/Shared` for layout and partials.

## Integration points / external dependencies

- Application Insights (configured in `Program.cs`).
- Health checks at `/healthcheck` — keep this endpoint stable for monitoring.
- There is a commented `app.UseAzureAppConfiguration()` call — if enabling Azure App Configuration, ensure secrets/keys are provided in pipeline or local dev settings.

## Helpful heuristics for PRs

- Small, focused PRs: change one controller/view/service at a time and include tests for behavior changes.
- Match namespaces and project references; build locally with `dotnet build` before pushing.
- If you change caching or telemetry, call that out in the PR description and explain runtime impact.

If anything here is unclear or you want more examples (tests, common refactors, or pipeline notes), tell me which area to expand.
