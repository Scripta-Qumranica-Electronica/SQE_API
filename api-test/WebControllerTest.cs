using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SQE.SqeHttpApi.Server;
using Xunit;

namespace api_test
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
                builder.ConfigureAppConfiguration((context,conf) =>
                {
                    conf.AddJsonFile(configPath);
                });
 
            });
            _client = _factory.CreateClient();
        }
    }
}