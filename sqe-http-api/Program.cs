using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;

/*using Serilog.Formatting.Compact;*/ // Use this to format log entries as JSON output

namespace SQE.SqeHttpApi.Server
{
	public static class Program
	{
		public static int Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
				.Enrich.FromLogContext()
				.WriteTo.Async(
					a => a.File( /*new CompactJsonFormatter(),*/ "logs/sqe-api.log",
						rollingInterval: RollingInterval.Day,
						buffered: true
					)
				)
				.CreateLogger();

			try
			{
				Log.Information("Starting web host");
				CreateWebHostBuilder(args).Build().Run();
				return 0;
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Host terminated unexpectedly");
				return 1;
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		private static IWebHostBuilder CreateWebHostBuilder(string[] args)
		{
			return WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>()
				.UseSerilog()
				.UseUrls(urls: "http://*:5000");
		}
	}
}