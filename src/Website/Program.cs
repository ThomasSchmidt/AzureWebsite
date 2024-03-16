using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzureWebsite
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddMvc();
			builder.Services.AddApplicationInsightsTelemetry();
			builder.Services.AddHealthChecks();
			builder.Services.AddControllersWithViews();

			var app = builder.Build();

			if (app.Environment.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
				// app.UseHttpsRedirection();
			}

			//app.UseAzureAppConfiguration();

			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			app.MapHealthChecks("/healthcheck");


			app.UseEndpoints(endpoints =>
			{
				_ = endpoints.MapDefaultControllerRoute();
			});

			app.Run();
		}
	}
}
