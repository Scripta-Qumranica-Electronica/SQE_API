using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SQE.API.Server
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // TODO: when we know the deployment details we will probably need to change the logging settings
                .AddJsonFile("appsettings.json", true)
                .AddCommandLine(args)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Starting web host");
                CreateHostBuilder(args).Build().Run();
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

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {
                        webBuilder
                            .UseStartup<Startup>()
                            .UseSerilog()
                            .UseUrls(urls: "http://*:5000");
                    }
                );
        }

        // return WebHost.CreateDefaultBuilder(args)
        //     .UseStartup<Startup>()
        //     .UseSerilog()
        //     .UseUrls(urls: "http://*:5000");
    }


    // public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
    //     .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
}