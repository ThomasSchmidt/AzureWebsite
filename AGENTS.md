## Purpose

This file tells AI coding agents how this repository is organized, how to build/run it, and which project-specific patterns to follow when changing code.

## Big picture

- **What:** An ASP.NET Core MVC site running on `net10` primarily in `src/AzureWebsite` with server-rendered Razor views, controllers, and simple app settings. The solution file is `AzureWebsite.slnx`.
- **Runtime:** Uses minimal `Program.cs` (WebApplication builder). Key middleware order: StaticFiles → Routing → OutputCache → Authentication → Authorization → HealthChecks (`/healthcheck`) → Endpoints. Application Insights is registered as a service (not middleware).
- **Tests:** Unit tests live in `test/AzureWebsite.Tests` and use a separate test project (`AzureWebsite.Tests.csproj`).

## Key files to inspect (examples)

- Startup and middleware: [`src/AzureWebsite/Program.cs`](src/AzureWebsite/Program.cs)
- Example controller + caching: [`src/AzureWebsite/Controllers/HomeController.cs`](src/AzureWebsite/Controllers/HomeController.cs)
- Config model: [`src/AzureWebsite/Infrastructure/WebsiteSettings.cs`](src/AzureWebsite/Infrastructure/WebsiteSettings.cs)
- Solution README: [`README.md`](README.md)
- Tests: [`test/AzureWebsite.Tests/AzureWebsite.Tests.csproj`](test/AzureWebsite.Tests/AzureWebsite.Tests.csproj)

## Project-specific conventions

- Namespace: files use `AzureWebsite` namespaces. Match the surrounding files' namespace when adding new files to avoid compile-time surprises.
- Configuration: environment-specific files live in `src/AzureWebsite` as `appsettings*.json`. Prefer reading configuration via bound POCOs (see `WebsiteSettings` and `IOptions<WebsiteSettings>` usage in controllers).
- Caching: Output caching is enabled globally and used via attribute `[OutputCache(Duration = 6000)]` on controllers. Be mindful when changing dynamic content.
- Routing: The app uses the default controller route (`MapDefaultControllerRoute`). Assume conventional MVC routes unless modifying `Program.cs` routing.
- Telemetry/CI: Application Insights is registered in startup; do not remove telemetry registration without reason.

## Feature Planning Specification Files

- All features must be documented in the `./plans/<plan-name>` directory (create this folder if it doesn't exist)
- Filename format: `<number>-<feature-name>.md` (e.g., `1-add-user-authentication.md`)
- Each spec file should contain a clear description of the feature, implementation approach, and any relevant diagrams or examples

## Build / Run / Test workflows

- Build solution: `dotnet build AzureWebsite.slnx`
- Build website only: `dotnet build src/AzureWebsite/AzureWebsite.csproj`
- Run locally (watch): `dotnet watch run --project src/AzureWebsite/AzureWebsite.csproj` or use the workspace task labeled `watch`.
- Publish: `dotnet publish src/AzureWebsite/AzureWebsite.csproj -c Release -o ./out` or use the workspace `publish` task.
- Run tests: `dotnet test test/AzureWebsite.Tests/AzureWebsite.Tests.csproj` or `dotnet test AzureWebsite.slnx`.
- Note: VS Code tasks exist for build/publish/watch (check the workspace `Tasks` panel).

## When you modify code

- Update both `src/AzureWebsite` and tests in `test/AzureWebsite.Tests` where appropriate. Run `dotnet test` after changing controller logic or configuration binding.
- Preserve middleware ordering in `Program.cs` (StaticFiles → Routing → OutputCache → Authentication → Authorization → HealthChecks → Endpoints). Changes to ordering can change behavior.
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

## Additional Details & Examples

### Testing with xUnit Traits
When adding new tests, annotate them with the appropriate category trait so they run in the correct CI stage:

```csharp
[Trait("Category", "unittest")]
[Fact]
public void TestMethodName()
{
    // test code
}

[Trait("Category", "integrationtest")]
[Fact]
public void IntegrationTest()
{
    // integration test code
}
```

### CI/CD Pipeline Details
The `ci.azure-pipelines.yml` and `cd.azure-pipelines.yml` files define the full build, test, coverage, and deployment pipeline:
- **SDK**: Uses .NET 10.0 SDK
- **Build**: Builds the solution defined by `**/*.slnx` (the actual file is `AzureWebsite.slnx`)
- **Deployment**: Publishes to Azure App Service named `schmidt` in the `production` environment
- **Coverage**: Generates code coverage reports using ReportGenerator

### Solution File Format
The repository uses a `.slnx` file (`AzureWebsite.slnx`) instead of the traditional `.sln`. All build tasks reference the solution via the `**/*.slnx` glob. When adding new projects, remember to update the `.slnx` file manually or via Visual Studio.

### Runtime & Target Framework
- **Target**: `net10.0` (.NET 10)
- **Runtime Identifiers**: `win-x64;linux-x64`
- **Publish Command**: Uses `-r linux-x64` for Azure App Service deployment

### Authentication & Authorization Setup
The middleware pipeline includes `app.UseAuthentication()` and `app.UseAuthorization()`. No authentication schemes are currently configured, but the project is ready to integrate:
- ASP.NET Core Identity
- JWT Bearer tokens
- Azure AD authentication

Custom routes would need to be added in the `MapEndpoints` section of `Program.cs`.

### Configuration Binding Pattern
Configuration is bound to the `WebsiteSettings` POCO via `IOptions<WebsiteSettings>`:
```csharp
private readonly IOptions<WebsiteSettings> _settings;

public HomeController(IOptions<WebsiteSettings> settings)
{
    _settings = settings;
}
```
To add new settings: extend the `WebsiteSettings` class and update the appropriate `appsettings*.json` files.

### Project Structure Overview
- **`src/AzureWebsite`** – Main application code (controllers, views, services)
- **`test/AzureWebsite.Tests`** – Unit tests using xUnit
- **`src/AzureWebsite/appsettings*.json`** – Environment-specific configuration
- **`src/AzureWebsite/Program.cs`** – Minimal hosting setup and middleware pipeline

### VS Code Tasks Reference
The workspace defines tasks for `build`, `publish`, and `watch`. These are available in the VS Code Tasks panel:
- Run with `Ctrl+Shift+B` or via the command palette (`Tasks: Run Task`)
- The `watch` task runs `dotnet watch run` for hot-reload development
