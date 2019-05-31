using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.SqeHttpApi.Server;
using Xunit;

namespace api_test
{
    /// <summary>
    /// Fires up the application and provides an HTTP Client to access its controller endpoints.
    /// </summary>
    public class WebControllerTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        public readonly HttpClient _client;

        public WebControllerTest(WebApplicationFactory<Startup> factory)
        {
            _client = factory.CreateClient();
        }
    }
}