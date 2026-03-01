## Purpose

This file tells AI coding agents how this repository is organized, how to build/run it, and which project-specific patterns to follow when changing code.

## Big picture

- **What:** An ASP.NET Core MVC site primarily in `src/AzureWebsite` with server-rendered Razor views, controllers, and simple app settings. The solution file is `AzureWebsite.slnx`.
- **Runtime:** Uses minimal `Program.cs` (WebApplication builder). Key middleware: Application Insights, HealthChecks (`/healthcheck`), Output Cache, Authentication/Authorization, Static Files, and default controller routing.
- **Tests:** Unit tests live in `test/AzureWebsite.Tests` and use a separate test project (`AzureWebsite.Tests.csproj`).

## Key files to inspect (examples)

- Startup and middleware: [`src/AzureWebsite/Program.cs`](src/AzureWebsite/Program.cs)
- Example controller + caching: [`src/AzureWebsite/Controllers/HomeController.cs`](src/AzureWebsite/Controllers/HomeController.cs)
- Config model: [`src/AzureWebsite/Infrastructure/WebsiteSettings.cs`](src/AzureWebsite/Infrastructure/WebsiteSettings.cs)
- Solution README: [`README.md`](README.md)
- Tests: [`test/AzureWebsite.Tests/AzureWebsite.Tests.csproj`](test/AzureWebsite.Tests/AzureWebsite.Tests.csproj)

## Project-specific conventions and gotchas

- Namespace inconsistencies: files use both `AzureWebsite` and `Website` namespaces. Match the surrounding files' namespace when adding new files to avoid compile-time surprises.
- Configuration: environment-specific files live in `src/AzureWebsite` as `appsettings*.json`. Prefer reading configuration via bound POCOs (see `WebsiteSettings` and `IOptions<WebsiteSettings>` usage in controllers).
- Caching: Output caching is enabled globally and used via attribute `[OutputCache(Duration = 6000)]` on controllers. Be mindful when changing dynamic content.
- Routing: The app uses the default controller route (`MapDefaultControllerRoute`). Assume conventional MVC routes unless modifying `Program.cs` routing.
- Telemetry/CI: Application Insights is registered in startup; do not remove telemetry registration without reason.

## feature specification files
- All features most be put in folder `features` and named with an increasing number as first part of filename in the following format `1-feature-name.md` and these spec files contain a description of the feature.

## Build / Run / Test workflows

- Build solution: `dotnet build AzureWebsite.slnx`
- Build website only: `dotnet build src/AzureWebsite/AzureWebsite.csproj`
- Run locally (watch): `dotnet watch run --project src/AzureWebsite/AzureWebsite.csproj` or use the workspace task labeled `watch`.
- Publish: `dotnet publish src/AzureWebsite/AzureWebsite.csproj -c Release -o ./out` or use the workspace `publish` task.
- Run tests: `dotnet test test/AzureWebsite.Tests/AzureWebsite.Tests.csproj` or `dotnet test AzureWebsite.slnx`.
- Note: VS Code tasks exist for build/publish/watch (check the workspace `Tasks` panel).

## When you modify code

- Update both `src/AzureWebsite` and tests in `test/AzureWebsite.Tests` where appropriate. Run `dotnet test` after changing controller logic or configuration binding.
- Preserve middleware ordering in `Program.cs` (StaticFiles -> Routing -> OutputCache -> Auth -> AuthZ -> Endpoints). Changes to ordering can change behavior.
- Respect existing views under `src/AzureWebsite/Views` and `Views/Shared` for layout and partials.

## Integration points / external dependencies

- Application Insights (configured in `Program.cs`).
- Health checks at `/healthcheck` — keep this endpoint stable for monitoring.
- There is a commented `app.UseAzureAppConfiguration()` call — if enabling Azure App Configuration, ensure secrets/keys are provided in pipeline or local dev settings.

## Helpful heuristics for PRs

- Small, focused PRs: change one controller/view/service at a time and include tests for behavior changes.
- Match namespaces and project references; build locally with `dotnet build` before pushing.
- If you change caching or telemetry, call that out in the PR description and explain runtime impact.

If anything here is unclear or you want more examples (tests, common refactors, or pipeline notes), tell me which area to expand.

## Additional Details

### Azure App Configuration
The project contains a commented call to `app.UseAzureAppConfiguration()` in `Program.cs`. If you enable Azure App Configuration, you must provide the connection string via an environment variable or user secret. The configuration is then merged into the existing `IConfiguration` used by the app.

### Health Checks
The health endpoint is exposed at `/healthcheck` via `app.MapHealthChecks("/healthcheck")`. It is used by Azure Monitor and CI pipelines to verify the service is running. No custom health checks are registered, so the default status is `Healthy` when the app starts.

### Output Caching
Output caching is enabled globally with `builder.Services.AddOutputCache()` and applied per controller action using the `[OutputCache(Duration = 6000)]` attribute. The `HomeController.Index` action demonstrates this. Be careful when adding dynamic content that changes frequently; you may need to adjust the duration or remove the attribute.

### Testing
Unit tests are located in `test/AzureWebsite.Tests`. They use xUnit and are filtered by the `Category` trait in the CI pipeline (`Category=unittest` or `integrationtest`). When adding new tests, annotate them with the appropriate category so they run in the correct stage.

### CI/CD Pipeline
The `ci.azure-pipelines.yml` and `cd.azure-pipelines.yml` files define the full build, test, coverage, and deployment pipeline. Key points:
* Uses .NET 8.0 SDK.
* Builds the solution defined by `**/*.sln` – note that the actual solution file is `AzureWebsite.slnx`.
* Publishes the website artifact to Azure App Service `schmidt` in the `production` environment.
* Publishes code‑coverage reports using ReportGenerator.

### Solution File
The repository uses a `.slnx` file (`AzureWebsite.slnx`) instead of the traditional `.sln`. All build tasks reference the solution via the `**/*.sln` glob, which works because the `.slnx` file is matched. If you add a new project, remember to update the `.slnx` file.

### Runtime & Target Framework
The project targets `net10.0` (a placeholder for the actual .NET 6/8 runtime). Runtime identifiers are set to `win-x64;linux-x64`, and the publish step uses `-r linux-x64` for the Azure App Service.

### Telemetry
Application Insights is configured in `Program.cs` with `builder.Services.AddApplicationInsightsTelemetry()`. The resource IDs are hard‑coded in the `.csproj`. Do not remove this registration unless you intend to replace telemetry.

### Authentication & Authorization
The middleware pipeline includes `app.UseAuthentication()` and `app.UseAuthorization()`. No authentication schemes are configured in this snippet, but the project is ready to plug in Identity or JWT providers.

### Routing
The app uses the default controller route via `endpoints.MapDefaultControllerRoute()`. Custom routes would need to be added in the `MapEndpoints` section.

### Configuration Binding
Configuration is bound to the `WebsiteSettings` POCO via `IOptions<WebsiteSettings>`. The `HomeController` injects `IOptions<WebsiteSettings>` to access settings. Add new settings by extending the `WebsiteSettings` class and updating the `appsettings*.json` files.

### Namespace Conventions
Use `AzureWebsite` as the root namespaces. When adding new files, match the surrounding namespace to avoid compile‑time issues.

### Project Structure
* `src/AzureWebsite` – main application code.
* `test/AzureWebsite.Tests` – unit tests.
* `src/AzureWebsite/appsettings*.json` – environment‑specific configuration.
* `src/AzureWebsite/Program.cs` – minimal hosting setup.

### VS Code Tasks
The workspace defines tasks for `build`, `publish`, and `watch`. These are available in the VS Code Tasks panel and can be run with `Ctrl+Shift+B` or via the command palette.
