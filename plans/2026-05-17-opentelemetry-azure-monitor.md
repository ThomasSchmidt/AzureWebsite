# OpenTelemetry Integration with Azure Monitor

**Date:** 2026-05-17  
**Status:** Planning  
**Target Framework:** .NET 10.0  
**Project:** AzureWebsite (ASP.NET Core Razor Pages)

## Overview

This plan outlines the implementation of OpenTelemetry instrumentation for the AzureWebsite solution, with telemetry exported to Azure Monitor (Application Insights). This will enable comprehensive observability including distributed tracing, metrics collection, and structured logging.

**Target Framework:** .NET 10.0 (verified: SDK v10.0.300)  
**Current State:** No Application Insights integration active (confirmed in AGENTS.md)

## Objectives

1. **Enable Distributed Tracing**: Track requests across all components of the application
2. **Collect Metrics**: Gather performance and operational metrics automatically
3. **Structured Logging**: Export logs with contextual telemetry data
4. **Azure Monitor Integration**: Use Azure Monitor as the primary telemetry backend
5. **Minimal Configuration**: Keep setup simple while maintaining flexibility for customization

## Architecture

### Components to Add

1. **OpenTelemetry SDK** - Core telemetry collection framework (built into .NET 10)
2. **Azure Monitor Exporter** - Transports telemetry to Application Insights
3. **ASP.NET Core Integration** - Automatic instrumentation for web requests
4. **Instrumentation Packages** - Additional libraries for HTTP, database, and other operations

### Data Flow

```
Application Code
    ↓
OpenTelemetry SDK (Automatic Instrumentation)
    ↓
Azure Monitor Exporter (OTLP Protocol)
    ↓
Application Insights (Azure Monitor)
    ↓
Azure Portal Visualization & Analysis
```

## Implementation Plan

### Phase 1: Package Installation

#### Required NuGet Packages

Add the following packages to `src/AzureWebsite/AzureWebsite.csproj`:

| Package | Purpose | Version Strategy |
|---------|---------|------------------|
| `Azure.Monitor.OpenTelemetry.AspNetCore` | ASP.NET Core integration with Azure Monitor | Latest stable (requires .NET 8+) |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | OTLP exporter for telemetry transport | Latest stable |

**Note:** `OpenTelemetry.Extensions.Hosting` is **NOT required** - OpenTelemetry is built into .NET 10.

#### Configuration Approach

Use **connection string-based configuration** via `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "ApplicationInsights": "your-connection-string-here"
  }
}
```

**Important:** When the connection string is null or empty, telemetry will be silently disabled. Consider adding validation logging in development environments to confirm whether telemetry is active.

### Phase 2: Program.cs Integration

#### Changes to `Program.cs`

Add OpenTelemetry configuration in the service registration phase, **as the FIRST service** registered:

```csharp
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry with Azure Monitor exporter (MUST be FIRST service)
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration["ConnectionStrings:ApplicationInsights"];
    });

builder.Services.AddRazorPages();

builder.Services.AddHealthChecks();  // Keep existing order
        
builder.Services.AddOutputCache();

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

// ... rest of Program.cs unchanged
```

**Key Considerations:**
- **OpenTelemetry must be configured FIRST** - before any other services to capture all telemetry from the start
- Maintain existing middleware order: StaticFiles → Routing → OutputCache → Authentication → Authorization → HealthChecks → RazorPages
- Use connection string for flexibility across environments
- The `AddOpenTelemetry().UseAzureMonitor()` pattern is correct for .NET 10

### Phase 3: Environment-Specific Configuration

#### Update `appsettings.json` (Development)
```json
{
  "ConnectionStrings": {
    "ApplicationInsights": null // Optional for development - telemetry will be disabled
  }
}
```

#### Update `appsettings.Production.json`
```json
{
  "Logging": {
    "IncludeScopes": false,
    "LogLevel": {
      "Default": "Warning",
      "System": "Warning",
      "Microsoft": "Warning"
    }
  },
  "ConnectionStrings": {
    "ApplicationInsights": "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/"
  }
}
```

**Note:** The existing `appsettings.Development.json` should also be updated if needed.

### Phase 4: Additional Instrumentation (Optional)

#### HTTP Client Instrumentation
Add automatic instrumentation for HTTP clients:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddHttpClientInstrumentation();
    });
```

#### Database Instrumentation (if applicable)
For Entity Framework or ADO.NET:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSqlClientInstrumentation();
    });
```

### Phase 5: Testing & Validation

#### Verification Steps

1. **Build Verification**
   ```bash
   dotnet build src/AzureWebsite/AzureWebsite.csproj
   ```

2. **Runtime Verification**
   ```bash
   dotnet watch run --project src/AzureWebsite/AzureWebsite.csproj
   ```

3. **Telemetry Validation**
   - Verify traces appear in Application Insights → Traces blade
   - Check metrics in Application Insights → Metrics Explorer
   - Review logs in Application Insights → Logs blade

4. **Distributed Tracing Test**
   - Make HTTP request to application
   - Verify trace ID appears in Application Insights spans
   - Confirm parent-child span relationships

### Phase 6: Monitoring & Alerting Setup

#### Recommended Application Insights Components

1. **Performance Counters** - Monitor CPU, memory, request rates
2. **Availability Tests** - Synthetic monitoring for critical endpoints
3. **Alert Rules** - Configure alerts for:
   - High error rates (>1%)
   - Slow requests (>5s p95)
   - Dependency failures

## Configuration Options

### Connection String Format (Recommended)

```
InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/;LiveEndpoint=https://<region>.livediagnostics.monitor.azure.com/
```

**Where:**
- `InstrumentationKey` - Your Application Insights instrumentation key (32 hex characters)
- `<region>` - Azure region (e.g., `eastus`, `westus2`)

### Alternative: Instrumentation Key Only (Simpler)

For simpler setup, use just the instrumentation key:
```csharp
options.ConnectionString = $"InstrumentationKey={your-key}";
```

Azure Monitor will auto-discover the correct endpoint based on your Azure subscription region.

**Note:** The full connection string format is recommended for production to ensure proper endpoint routing.

## Potential Issues & Considerations

### 1. Null Connection String Handling

When `ConnectionStrings:ApplicationInsights` is null or empty, the exporter will silently fail. Add validation in development:

```csharp
var connectionString = builder.Configuration["ConnectionStrings:ApplicationInsights"];

if (string.IsNullOrEmpty(connectionString))
{
    // Telemetry will be disabled - log warning in development
    Console.WriteLine("Warning: Application Insights connection string not configured. Telemetry disabled.");
}
```

### 2. Performance Impact

OpenTelemetry adds minimal overhead (~5-10ms startup time), but verify with load testing in production.

### 3. Memory Usage

Telemetry data is buffered before export. Monitor memory usage if you see increases after deployment.

### 4. Sampling Configuration

For high-traffic applications, consider implementing sampling to reduce telemetry volume:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetSampler(new ParentBasedTraceIdRatio(0.01)); // Sample 1% of traces
    });
```

### 5. Environment-Specific Behavior

Ensure telemetry is disabled in development/staging unless explicitly configured, to avoid:
- Unnecessary costs
- Data privacy concerns
- Information overload in Application Insights

## Customization Points

### 1. Sampling Configuration

Control telemetry volume with sampling rules:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetSampler(new AlwaysOnSampler()); // Or ParentBasedTraceIdRatio
    });
```

Options:
- `AlwaysOnSampler` - Capture all telemetry
- `ParentBasedTraceIdRatio` - Sample based on probability
- `AlwaysOffSampler` - Disable telemetry

### 2. Attribute Enrichment

Add custom attributes to spans:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("AzureWebsite");
        tracing.OnStart = (span, _) =>
        {
            // Add custom attributes
            span.SetTag("custom.attribute", "value");
        };
    });
```

### 3. Log Processing

Configure log sampling and processing:

```csharp
builder.Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    });
```

## Deployment Considerations

### Azure App Service Deployment

1. **Publish Application**
   ```bash
   dotnet publish src/AzureWebsite/AzureWebsite.csproj -c Release -o ./publish/website -r linux-x64
   ```
   
2. **Deploy to Azure**
   - Copy published files to Azure App Service
   - Configure Application Insights connection string in production settings via:
     - Azure Portal → Configuration → Application settings
     - Or via CI/CD pipeline

3. **Verify Deployment**
   - Check Application Insights telemetry stream (should see data within 1-2 minutes)
   - Monitor initial request traces
   - Validate metrics collection
   - Review logs in Application Insights → Logs blade

### Environment Variables (Alternative for Containers)

For containerized deployments, use environment variables:

```csharp
options.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
```

**Fallback:** If the connection string is null or empty, telemetry will be silently disabled. Consider adding validation logging.

## Rollback Plan

If issues arise:

1. **Disable Telemetry** - Set connection string to null in `appsettings.Production.json`:
   ```json
   {
     "ConnectionStrings": {
       "ApplicationInsights": null
     }
   }
   ```

2. **Remove Packages** (if needed):
   ```bash
   dotnet remove package Azure.Monitor.OpenTelemetry.AspNetCore
   dotnet remove package OpenTelemetry.Exporter.OpenTelemetryProtocol
   ```

3. **Restore Previous Build**
   - Use version control to revert to last known good state
   - Commit: `git restore src/AzureWebsite/Program.cs`

## Success Criteria

- [ ] Application builds successfully with new packages (`dotnet build`)
- [ ] No compilation errors or warnings related to OpenTelemetry
- [ ] Traces appear in Application Insights within 5 minutes of deployment
- [ ] Metrics are collected for HTTP requests, page loads, and health checks
- [ ] Logs include contextual telemetry data (trace IDs, span IDs)
- [ ] No performance degradation (>5% increase in startup time)
- [ ] Distributed tracing works across HTTP clients and dependencies
- [ ] Health check endpoint `/healthcheck` still functions correctly

## Next Steps

1. **Review Plan** - Validate approach with team
2. **Obtain Application Insights Resource** - Create or identify target resource
3. **Implement Phase 1** - Add NuGet packages
4. **Implement Phase 2** - Update Program.cs
5. **Test Locally** - Verify telemetry collection
6. **Deploy to Staging** - Validate in staging environment
7. **Monitor & Tune** - Adjust sampling and configuration as needed

## Troubleshooting

### Telemetry Not Appearing in Application Insights

1. **Check Connection String**
   - Verify the connection string is correctly set in `appsettings.Production.json`
   - Ensure no typos in the instrumentation key
   - Confirm the Application Insights resource exists and is accessible

2. **Check Logs**
   - Review application logs for errors related to telemetry initialization
   - Look for warnings about null or invalid connection strings

3. **Verify Package Installation**
   ```bash
   dotnet list package --include-transitive
   ```
   Ensure both required packages are installed with compatible versions.

4. **Test Locally**
   - Set a valid connection string in `appsettings.json` for testing
   - Run the application and check Application Insights telemetry stream

### Performance Issues

1. **High Memory Usage**
   - Check if telemetry buffer is growing unbounded
   - Consider implementing sampling to reduce data volume

2. **Slow Startup Time**
   - OpenTelemetry adds ~5-10ms overhead
   - If critical, consider deferring telemetry initialization

### Connection Errors

1. **Ingestion Endpoint Issues**
   - Verify the region in your connection string matches your Application Insights resource
   - Check Azure Monitor service availability in your region

2. **Network Connectivity**
   - Ensure outbound HTTPS (port 443) is allowed to Azure endpoints
   - Test connectivity: `curl https://<region>.in.applicationinsights.azure.com/`

## References

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/dotnet/)
- [Azure Monitor OpenTelemetry Exporter](https://learn.microsoft.com/dotnet/api/overview/azure/monitor.opentelemetry.exporter-readme)
- [ASP.NET Core OpenTelemetry Integration](https://learn.microsoft.com/dotnet/api/overview/azure/monitor.opentelemetry.aspnetcore-readme)
- [Application Insights Overview](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [.NET 10 Release Notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10)
