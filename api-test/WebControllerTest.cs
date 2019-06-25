using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SQE.SqeHttpApi.Server;
using Xunit;

namespace SQE.ApiTest
{
    /// <summary>
    /// Fires up the application and provides an HTTP Client to access its controller endpoints.
    /// </summary>
    public class WebControllerTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        public readonly WebApplicationFactory<Startup> _factory;
        public readonly HttpClient _client;

        public WebControllerTest(WebApplicationFactory<Startup> factory)
        {
            var projectDir = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(projectDir, "../../../../sqe-http-api/appsettings.json");
 
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // Setting the environment to IntegrationTests will turn off the emailer code.
                builder.UseEnvironment("IntegrationTests"); 
                builder.ConfigureAppConfiguration((context,conf) =>
                {
                    conf.AddJsonFile(configPath);
                });
 
            });
            _client = _factory.CreateClient();
        }
    }
}