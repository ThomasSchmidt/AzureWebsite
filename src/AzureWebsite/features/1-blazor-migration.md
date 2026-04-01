# ASP.NET Core MVC to Blazor Migration Plan

## Current State Analysis

### Existing Application Structure
- **Framework**: ASP.NET Core 10.0 MVC with Razor Pages
- **Features**: 
  - Home page with dynamic content (ShowThis setting)
  - Privacy and Error pages
  - Health check endpoint at `/healthcheck`
  - Application Insights telemetry integration
  - Output caching (6000 seconds on Index action)
  - Basic layout with header navigation and footer

### Key Components to Migrate
1. **HomeController.Index** → Blazor Home page
2. **Shared Layout** → Blazor components (Header, Footer, MainLayout)
3. **HomeViewModel** → C# class for data model
4. **WebsiteSettings** → Configuration binding (keep as-is)

---

## Migration Phases

### Phase 1: Project Setup & Dependencies

#### 1.1 Update Project File (`AzureWebsite.csproj`)
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <!-- Keep existing Application Insights and other settings -->
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Existing packages -->
    
    <!-- Add Blazor packages -->
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="X.X.X" />
    <PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="X.X.X" />
  </ItemGroup>

</Project>
```

#### 1.2 Install Required NuGet Packages
- `Microsoft.AspNetCore.Components.Web` - Blazor WebAssembly/Server support
- `System.Text.Json` (if not already included)

---

### Phase 2: Application Configuration (`Program.cs`)

#### 2.1 Update Service Registration
```csharp
// Replace AddControllersWithViews() with Blazor services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(); // For Blazor Server
// OR builder.Services.AddWebAssemblyHost(); // For Blazor WASM

// Keep existing services:
// - AddApplicationInsightsTelemetry()
// - AddHealthChecks()
// - AddOutputCache()
// - Configuration binding for WebsiteSettings
```

#### 2.2 Update Middleware Pipeline
```csharp
// Replace:
app.UseEndpoints(endpoints => { _ = endpoints.MapDefaultControllerRoute(); });

// With:
app.MapBlazorHub();
app.MapFallbackToPage("/_Host"); // For Blazor Server
// OR app.MapFallbackToFile("index.html", "index.html"); // For Blazor WASM
```

---

### Phase 3: Component Extraction & Creation

#### 3.1 Create Components Folder Structure
```
src/AzureWebsite/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor          // Replaces _Layout.cshtml
│   │   ├── Header.razor              // Extracted header component
│   │   └── Footer.razor              // Extracted footer component
│   └── Pages/
│       ├── Home.razor                // Replaces Views/Home/Index.cshtml
│       ├── Privacy.razor             // Replaces Views/Home/Privacy.cshtml
│       └── Error.razor               // Replaces Views/Error.cshtml
```

#### 3.2 Create MainLayout.razor
**Purpose**: Global layout with navigation and section rendering

```razor
@inherits LayoutComponentBase

<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@Title - Schmidt</title>
    <link rel="stylesheet" href="css/bootstrap/bootstrap.min.css" />
    <link rel="stylesheet" href="css/site.css" />
    @RenderHeadContent()
</head>
<body>
    <div class="d-flex flex-column min-vh-100">
        <Header />
        
        <main>
            <div class="container">
                @Body
            </div>
        </main>
        
        <Footer />
    </div>

    <script src="_framework/blazor.web.js"></script>
    @RenderSection("Scripts", required: false)
</body>
</html>
```

#### 3.3 Create Header.razor
**Purpose**: Navigation header component

```razor
<header>
    <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
        <div class="container">
            <a class="navbar-brand" asp-area="" asp-page="/Index">AzureWebsite</a>
            <button class="navbar-toggler" type="button" data-toggle="collapse" 
                    data-target=".navbar-collapse" aria-controls="navbarSupportedContent"
                    aria-expanded="false" aria-label="Toggle navigation">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="navbar-collapse collapse d-sm-inline-flex flex-sm-row-reverse">
                <ul class="navbar-nav flex-grow-1">
                    <li class="nav-item">
                        <a class="nav-link text-dark" asp-page="/Home/Index">Home</a>
                    </li>
                </ul>
            </div>
        </div>
    </nav>
</header>
```

#### 3.4 Create Footer.razor
**Purpose**: Site footer component

```razor
<footer class="border-top footer text-muted">
    <div class="container">
        &copy; @DateTime.Now.Year - Powered by @System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
    </div>
</footer>
```

---

### Phase 4: Page Migration

#### 4.1 Home.razor (Primary Page)
**Purpose**: Replaces HomeController.Index with dynamic content

```razor
@page "/"
@using Website.Models
@inject IOptions<WebsiteSettings> Settings
@inject IOutputCacheCache OutputCaching
@inject OutputCacheOptions CacheOptions
@title "Home"

<PageTitle>Welcome</PageTitle>

<div class="text-center">
    <h1 class="display-4">Welcome</h1>
    
    @if (!string.IsNullOrEmpty(Settings.Value.ShowThis))
    {
        <div class="alert alert-info mt-3">
            <strong>@Settings.Value.ShowThis</strong>
        </div>
    }
    
    <p class="lead">Blazor has replaced ASP.NET Core MVC!</p>
</div>

@code {
    [Inject]
    private IOptions<WebsiteSettings> Settings { get; set; } = default!;
}
```

#### 4.2 Privacy.razor
**Purpose**: Replaces Privacy view page

```razor
@page "/privacy"
@using Microsoft.AspNetCore.Mvc.RazorPages
@title "Privacy Policy"

<PageTitle>Privacy</PageTitle>

<h1>Privacy Policy</h1>

<p>Your privacy is important to us.</p>
```

#### 4.3 Error.razor
**Purpose**: Replaces error handling page

```razor
@page "/error"
@using Microsoft.AspNetCore.Mvc.RazorPages
@inject IHttpContextAccessor HttpContextAccessor
@title "Error"

<PageTitle>Error</PageTitle>

<h1 class="text-danger">Error.</h1>
<h2 class="text-danger">An error occurred while processing your request.</h2>

@if (Model?.RequestId != null)
{
    <p><strong>Request ID:</strong> <code>@Model.RequestId</code></p>
}

<p>Your support ID is: @HttpContextAccessor.HttpContext?.TraceIdentifier</p>
```

---

### Phase 5: Data Models & Configuration

#### 5.1 Keep Existing Models
- `HomeViewModel.cs` - Can be kept or simplified for Blazor
- `WebsiteSettings.cs` - Keep as-is (already configured)

#### 5.2 Update appsettings.json
Ensure configuration binding is properly set up:

```json
{
  "WebsiteSettings": {
    "ShowThis": "Welcome to our Blazor application!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

### Phase 6: Static Files & Assets

#### 6.1 CSS Migration
- Keep existing `wwwroot/css/site.css`
- Add Bootstrap if not already present (Blazor templates include it)
- Update any MVC-specific CSS classes to Blazor-compatible ones

#### 6.2 JavaScript Migration
- Convert jQuery code to Blazor JavaScript interop if needed
- Create JS interop services for custom functionality:

```csharp
// wwwroot/js/custom.js
window.blazorInit = function () {
    console.log('Blazor initialized');
};

// Infrastructure/JsInterop.cs
public static class JsInterop {
    public static async Task Init(IJSRuntime jsRuntime) {
        await jsRuntime.InvokeVoidAsync("blazorInit");
    }
}
```

---

### Phase 7: Testing & Validation

#### 7.1 Update Tests (`AzureWebsite.Tests.csproj`)
- Convert controller tests to component tests
- Use `TestContext` and `WebApplicationFactory<Program>` for integration testing

#### 7.2 Test Scenarios
- [ ] Home page renders correctly with ShowThis value
- [ ] Navigation links work properly
- [ ] Privacy and Error pages accessible
- [ ] Health check endpoint still functional
- [ ] Output caching works on Blazor pages
- [ ] Application Insights telemetry captures events

---

### Phase 8: Deployment & Rollout

#### 8.1 Pre-deployment Checklist
- [ ] All routes tested locally
- [ ] Configuration validated for production
- [ ] Performance benchmarks compared to MVC version
- [ ] Accessibility testing completed
- [ ] Cross-browser compatibility verified

#### 8.2 Deployment Strategy
**Option A: Blue-Green Deployment**
1. Deploy Blazor app alongside existing MVC
2. Route 50% traffic to test
3. Gradually increase traffic if stable
4. Decommission MVC after validation

**Option B: Direct Migration**
1. Backup current deployment
2. Deploy Blazor version
3. Monitor logs and metrics closely
4. Rollback plan ready

---

## Timeline Estimate

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| 1. Project Setup | 0.5 days | None |
| 2. Configuration | 0.5 days | Phase 1 complete |
| 3. Components | 1 day | Phase 2 complete |
| 4. Pages | 1 day | Phase 3 complete |
| 5. Data Models | 0.5 days | Phase 4 complete |
| 6. Assets | 0.5 days | Phase 5 complete |
| 7. Testing | 1-2 days | All phases complete |
| 8. Deployment | 0.5 days | Testing passed |

**Total Estimated Time**: 5-7 working days

---

## Key Considerations & Risks

### Advantages of Blazor Migration
✅ **Component-based architecture** - Better code reusability  
✅ **Strongly-typed UI** - Compile-time validation  
✅ **C# everywhere** - No JavaScript context switching  
✅ **Better state management** - Built-in reactivity  
✅ **Improved developer experience** - Hot reload, better debugging

### Potential Challenges
⚠️ **Bundle size** - Blazor Server has smaller footprint than WASM  
⚠️ **Real-time features** - Requires SignalR for server communication  
⚠️ **SEO considerations** - May need prerendering or SSR  
⚠️ **Learning curve** - Team may need training on Blazor patterns

### Recommendations
1. **Start with Blazor Server** - Easier migration path, better performance initially
2. **Consider Blazor WASM later** - If offline capabilities needed
3. **Implement progressive enhancement** - Keep some MVC pages if complex forms needed
4. **Use MudBlazor or Radzen** - Consider UI component libraries for faster development

---

## Next Steps

1. **Review this plan** with team stakeholders
2. **Create feature branch** for migration work
3. **Set up development environment** with Blazor templates
4. **Begin Phase 1** (Project Setup)
5. **Document decisions** made during migration in `/memories/session/`

---

## References

- [Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [MVC to Blazor Migration Guide](https://docs.microsoft.com/en-us/aspnet/core/migration/50-to-60?view=aspnetcore-6.0#blazor)
- [Blazor Component Patterns](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/patterns)
