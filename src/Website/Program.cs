using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AzureWebsite
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});
				/*
				.ConfigureAppConfiguration(config =>
				{
					var settings = config.Build();
					var appConfigConnectionString = settings.GetConnectionString("AppConfig");
					config.AddAzureAppConfiguration(options =>
					{
						options
							.Connect(appConfigConnectionString)
							.ConfigureRefresh(refreshOptions =>
							{
								refreshOptions
									.Register("Website:Sentinel", true)
									.SetCacheExpiration(TimeSpan.FromSeconds(10));
							});
					});
				});
				*/
	}
}
