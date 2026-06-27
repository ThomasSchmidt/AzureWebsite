using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using AzureWebsite.Services;

namespace AzureWebsite;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add OpenTelemetry with Azure Monitor exporter (MUST be FIRST service)
        // Only configure if connection string is available (production env var or local override)
        var appInsightsConnectionString = builder.Configuration["ConnectionStrings:ApplicationInsights"];
        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            builder.Services.AddOpenTelemetry()
                .UseAzureMonitor(options =>
                {
                    options.ConnectionString = appInsightsConnectionString;
                });
        }

        builder.Services.AddRazorPages();

        // Blog support
        builder.Services.AddMemoryCache();
        builder.Services.Configure<BlogSettings>(builder.Configuration.GetSection("Blog"));
        builder.Services.AddSingleton<IBlogService, BlogService>();

        builder.Services.AddHealthChecks();
        
        builder.Services.AddOutputCache();

        builder.Services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = true;
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHttpsRedirection();
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseOutputCache();

        app.MapHealthChecks("/healthcheck");

        app.MapRazorPages();

        app.Run();
    }
}
