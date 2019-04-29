using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using sqe_signalr_api;

namespace SqeSignalrApi
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls(urls: "http://*:5001");
    }
}