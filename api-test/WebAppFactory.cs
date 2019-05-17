using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.SqeHttpApi.Server;
using Xunit;

namespace api_test
{
    /// <summary>
    /// Fires up the application and provides an HTTP Client to access its controller endpoints.
    /// </summary>
    public class WebAppFactory : ICollectionFixture<WebApplicationFactory<Startup>>
    {
        public readonly HttpClient Client;

        public WebAppFactory()
        {
            var factory = new WebApplicationFactory<Startup>();
            Client = factory.CreateClient();
        }
    }
}