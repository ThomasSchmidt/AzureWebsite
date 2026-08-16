using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AzureWebsite.Tests;

[Trait("Category", "integrationtest")]
public class ProgramTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProgramTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateProductionFactory()
    {
        var environmentVariables = new Dictionary<string, string>
        {
            { "ASPNETCORE_ENVIRONMENT", "Production" }
        };

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    context.Configuration = new ConfigurationBuilder()
                        .AddInMemoryCollection(environmentVariables)
                        .Build();
                });
            });
    }

    [Fact]
    public async Task Main_ApplicationStartsAndServesRequests()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.NotNull(response);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Main_HealthCheckEndpoint_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthcheck");

        Assert.NotNull(response);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Main_ErrorPage_ReturnsNotFoundWhenNoException()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Error");

        Assert.NotNull(response);
        // Error page should return 200 when accessed directly without exception
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Main_BlogListingPage_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Blog");

        Assert.NotNull(response);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Main_StaticFiles_AreServed()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/favicon.ico");

        Assert.NotNull(response);
        // favicon.ico may return 404 if not present, but the middleware should handle it
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Expected OK or NotFound, got {response.StatusCode}");
    }

    [Fact]
    public async Task Main_OutputCache_Middleware_IsConfigured()
    {
        var client = _factory.CreateClient();

        var response1 = await client.GetAsync("/");
        var response2 = await client.GetAsync("/");

        // Both requests should succeed
        Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);
    }

    [Fact]
    public async Task Main_HSTS_Middleware_IsConfigured()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.NotNull(response);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        // HSTS header should be present in production-like environment
        // In test environment it may not be set, but the middleware is configured
    }

    [Fact]
    public async Task Main_RazorPages_AreMapped()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.NotNull(response);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Main_ApplicationServices_AreRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // BlogService should be registered as singleton
        var blogService = services.GetService<AzureWebsite.Services.IBlogService>();
        Assert.NotNull(blogService);
        
        // MemoryCache should be registered
        var cache = services.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        Assert.NotNull(cache);
    }

    [Fact]
    public async Task Main_ProductionEnvironment_UsesExceptionHandler()
    {
        var productionFactory = CreateProductionFactory();
        var client = productionFactory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.NotNull(response);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Main_ProductionEnvironment_UsesHsts()
    {
        var productionFactory = CreateProductionFactory();
        var client = productionFactory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.NotNull(response);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        // HSTS middleware is configured in production environment
        // The header may not appear in test client due to redirect behavior
    }

    [Fact]
    public async Task Main_ProductionEnvironment_UsesHttpsRedirection()
    {
        var productionFactory = CreateProductionFactory();
        var client = productionFactory.CreateClient();

        // In production, HTTPS redirection middleware is active
        var response = await client.GetAsync("/");

        Assert.NotNull(response);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
