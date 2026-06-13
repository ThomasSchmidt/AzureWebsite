## Purpose

This file tells AI coding agents how this repository is organized, how to build/run it, and which project-specific patterns to follow when changing code.

## Big picture

- **What:** An ASP.NET Core Razor Pages site running on `net10` in `src/AzureWebsite`. The solution file is `AzureWebsite.slnx`.
- **Runtime:** Uses minimal `Program.cs` (WebApplication builder). Key middleware order: StaticFiles → Routing → OutputCache → Authentication → Authorization → HealthChecks (`/healthcheck`) → RazorPages. HSTS is configured with 365-day preload; HTTPS redirection is commented out. No Application Insights or Azure App Configuration integration currently active.
- **Tests:** Unit tests live in `test/AzureWebsite.Tests` and use a separate test project (`AzureWebsite.Tests.csproj`).

## Key files to inspect (examples)

- Startup and middleware: `src/AzureWebsite/Program.cs`
- Razor pages: `src/AzureWebsite/Pages/Index.cshtml`, `src/AzureWebsite/Pages/Error.cshtml`
- Page models: `src/AzureWebsite/Pages/Index.cshtml.cs`, `src/AzureWebsite/Pages/Error.cshtml.cs`
- Config model: `src/AzureWebsite/Infrastructure/WebsiteSettings.cs`
- Solution README: `README.md`
- Tests: `test/AzureWebsite.Tests/AzureWebsite.Tests.csproj`

## Project-specific conventions

- Namespace: files use `AzureWebsite` namespaces. Match the surrounding files' namespace when adding new files to avoid compile-time surprises.
- Configuration: environment-specific files live in `src/AzureWebsite` as `appsettings*.json`. Prefer reading configuration via bound POCOs (see `WebsiteSettings` and `IOptions<WebsiteSettings>` usage).
- Caching: Output caching is enabled globally (`UseOutputCache()`). Be mindful when changing dynamic content.
- Routing: The app uses Razor Pages routing (`MapRazorPages()`). There are no controllers — this is a pure Razor Pages application, not MVC.
- Security: HSTS is configured with 365-day max age and preload enabled; HTTPS redirection is commented out in production branch.

## Feature Planning Specification Files

- All features must be documented in the `plans/<plan-date>-<plan-name>.md` directory where `<plan-date>` is YYYY-MM-DD format (create this folder if it doesn't exist).
- Each spec file should contain a clear description of the feature, implementation approach, and any relevant diagrams or examples

## Build / Run / Test workflows

- Build solution: `dotnet build AzureWebsite.slnx`
- Build website only: `dotnet build src/AzureWebsite/AzureWebsite.csproj`
- Run locally (watch): `dotnet watch run --project src/AzureWebsite/AzureWebsite.csproj` or use the workspace task labeled `watch`.
- Publish: `dotnet publish src/AzureWebsite/AzureWebsite.csproj -c Release -o ./out` or use the workspace `publish` task.
- Run all tests: `dotnet test`.
- Run all tests and collect code coverage with `dotnet test --collect:"XPlat Code Coverage"` 
- To test a specific C# file use `dotnet test --filter "FullyQualifiedName=<full-namespace-to-class>"` where <full-namespace-to-class> is the fully qualified name for the class to test
- Note: VS Code tasks exist for build/publish/watch (check the workspace `Tasks` panel).

## When you modify code

- Update both `src/AzureWebsite` and tests in `test/AzureWebsite.Tests` where appropriate. Run `dotnet test` after changing logic or configuration binding.
- Preserve middleware ordering in `Program.cs` (StaticFiles → Routing → OutputCache → Authentication → Authorization → HealthChecks → RazorPages). Changes to ordering can change behavior.
- Respect existing views under `src/AzureWebsite/Pages/Shared` for layout and partials.

## Integration points / external dependencies

- Health checks at `/healthcheck` — currently configured with no providers (`AddHealthChecks()` + `MapHealthChecks("/healthcheck")`). Keep this endpoint stable for monitoring; add diagnostics as needed.
- No Application Insights or Azure App Configuration integration is currently active in the codebase.

## Helpful heuristics for PRs

- Small, focused PRs: change one page/service at a time and include tests for behavior changes.
- Match namespaces and project references; build locally with `dotnet build` before pushing.

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

### Configuration Binding Pattern
Configuration is bound to the `WebsiteSettings` POCO via `IOptions<WebsiteSettings>`:
```csharp
private readonly IOptions<WebsiteSettings> _settings;

public IndexModel(IOptions<WebsiteSettings> settings)
{
    _settings = settings;
}
```
To add new settings: extend the `WebsiteSettings` class and update the appropriate `appsettings*.json` files.

### C# conventions
- Use C# version 14
- Use modern C# with pattern matching, eg. use `SomeThing is null` instead of `SomeThing == null`

### Project Structure Overview
- **`src/AzureWebsite/Pages`** – Razor pages (.cshtml, .cshtml.cs) with shared layout under `Pages/Shared`
- **`src/AzureWebsite/Infrastructure`** – Configuration models (e.g., `WebsiteSettings`)
- **`test/AzureWebsite.Tests`** – Unit tests using xUnit
- **`src/AzureWebsite/appsettings*.json`** – Environment-specific configuration (`appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`)
- **`src/AzureWebsite/Program.cs`** – Minimal hosting setup and middleware pipeline

### VS Code Tasks Reference
The workspace defines tasks for `build`, `publish`, and `watch`. These are available in the VS Code Tasks panel:
- Run with `Ctrl+Shift+B` or via the command palette (`Tasks: Run Task`)
- The `watch` task runs `dotnet watch run` for hot-reload development
