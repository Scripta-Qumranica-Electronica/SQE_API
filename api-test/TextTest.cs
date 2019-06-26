using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.ApiTest.Helpers;
using SQE.SqeHttpApi.Server;
using SQE.SqeHttpApi.Server.DTOs;
using Xunit;

namespace SQE.ApiTest
{
    public class TextTest : WebControllerTest
    {
        private readonly Faker _faker = new Faker("en");
        private readonly DatabaseQuery _db;

        private const string version = "v1";
        private const string controller = "text-fragments";
        private readonly string _getTextFragments;

        public TextTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
            _getTextFragments = $"/{version}/editions/$EditionId/{controller}";
        }
        
        #region Anonymous retrieval

        [Fact]
        public async Task CanGetAnonymousEditionTextFragments()
        {
            // Setup
            var editionId = _faker.Random.Number(1, 1100);
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, TextFragmentListDTO>(_client, HttpMethod.Get, 
                _getTextFragments.Replace("$EditionId", editionId.ToString()), null);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotEmpty(msg.textFragments);
            Assert.NotEqual((uint)0, msg.textFragments[0].colId);
        }
        
        #endregion Anonymous retrieval
    }
}