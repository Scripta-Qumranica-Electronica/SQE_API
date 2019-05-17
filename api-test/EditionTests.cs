//using System.Net.Http;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc.Testing;
//using SQE.SqeHttpApi.Server;
//using Xunit;
//
//namespace ApiTests
//{
//    /// <summary>
//    /// One instance of this will be created per test collection.
//    /// </summary>
//    public class CollectionFixture : ICollectionFixture<WebApplicationFactory<Startup>>
//    {
//        public readonly HttpClient Client;
//
//        public CollectionFixture()
//        {
//            var factory = new WebApplicationFactory<Startup>();
//            Client = factory.CreateClient();
//        }
//    }
//    
//    [CollectionDefinition("Integration tests collection")]
//    public class IntegrationTestsCollection : ICollectionFixture<CollectionFixture>
//    {
//        // This class has no code, and is never created. Its purpose is simply
//        // to be the place to apply [CollectionDefinition] and all the
//        // ICollectionFixture<> interfaces.
//    }
//   
//    [Collection("Edition endpoint tests collection")]
//    public class EditionTests
//    {
//        private readonly CollectionFixture _collectionFixture;
//
//        public EditionTests()
//        {
//            _collectionFixture = new CollectionFixture();
//        }
//
//        [Fact]
//        public async Task Test1()
//        {
//            // When
//            //var response = await _collectionFixture.Client.GetStringAsync();
//            Assert.Equal(1, 1);
//        }
//    }
//}
//

using System;
using System.Threading.Tasks;
using api_test.Helpers;
using Xunit;

namespace api_test
{
    public class EditionTests
    {
        private readonly WebAppFactory _factory;
        public EditionTests()
        {
            _factory = new WebAppFactory();
        }

        [Theory]
        [InlineData("/v1/editions")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
        {

            // Act
            var response = await _factory.Client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
            
            var jwt = await Login.GetJWT(_factory.Client);
        }
    }
}