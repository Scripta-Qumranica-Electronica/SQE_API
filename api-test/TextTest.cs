using System.Collections.Generic;
using System.Linq;
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
        private const string editionsController = "editions";
        private const string linesController = "lines";
        private const string controller = "text-fragments";
        private readonly string _editionBase;
        private readonly string _getTextFragmentsData;
        private readonly string _getTextFragments;
        private readonly string _getTextLinesData;
        private readonly string _getTextLines;

        public TextTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
            _editionBase = $"/{version}/{editionsController}/$EditionId";
            _getTextFragmentsData = $"{_editionBase}/{controller}";
            _getTextFragments = $"{_getTextFragmentsData}/$TextFragmentId";
            _getTextLinesData = $"{_getTextFragments}/{linesController}";
            _getTextLines = $"{_editionBase}/{linesController}/$LineId";
        }
        
        #region Anonymous retrieval

        [Fact]
        public async Task CanGetAnonymousEditionTextFragmentData()
        {
            // Arrange
            var edition = EditionHelpers.GetRandomEdition(_db, _client);
            var editionId = edition.Id;
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, TextFragmentDataListDTO>(_client, HttpMethod.Get, 
                _getTextFragmentsData.Replace("$EditionId", editionId.ToString()), null);

            // Assert
            response.EnsureSuccessStatusCode();
        }
        
        [Fact]
        public async Task CanGetAnonymousEditionTextFragment()
        {
            // Arrange
            var (editionId, textFragmentId) = await _getTextFragment();
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, TextEditionDTO>(_client, HttpMethod.Get, 
                _getTextFragments.Replace("$EditionId", editionId.ToString())
                    .Replace("$TextFragmentId", textFragmentId.ToString()), null);

            // Assert
            response.EnsureSuccessStatusCode();
            _verifyTextEditionDTO(msg); // Verify we got expected data
        }
        
        [Fact]
        public async Task CanGetAnonymousEditionTextLineData()
        {
            // Arrange
            var (editionId, textFragmentId) = await _getTextFragment();
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, LineDataListDTO>(_client, HttpMethod.Get, 
                _getTextLinesData.Replace("$EditionId", editionId.ToString())
                    .Replace("$TextFragmentId", textFragmentId.ToString()), null);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotEmpty(msg.lines);
            Assert.NotEqual((uint)0, msg.lines[0].lineId);
        }
        
        [Fact]
        public async Task CanGetAnonymousEditionTextLine()
        {
            // Arrange
            var (editionId, textFragmentId, lineId) = await _getLine();
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, LineTextDTO>(_client, HttpMethod.Get, 
                _getTextLines.Replace("$EditionId", editionId.ToString())
                    .Replace("$LineId", lineId.ToString()), null);

            // Assert
            response.EnsureSuccessStatusCode();
            _verifyLineTextDTO(msg); // Verify we got expected data
        }
        
        #endregion Anonymous retrieval
        
        // TODO: authenticated retrieval and blocking of unauthorized requests
        #region Authenticated retrieval (should succeed)
        #endregion Authenticated retrieval (should succeed)
        
        #region Unauthenticated retrieval (should fail)
        #endregion Unauthenticated retrieval (should fail)
        
        #region Helpers

        private async Task<(uint editionId, uint textFragmentId)> _getTextFragment()
        {
            var edition = await EditionHelpers.GetRandomEdition(_db, _client);
            var editionId = edition.id;
            var (fragmentsResponse, textFragments) = await HttpRequest.SendAsync<string, TextFragmentDataListDTO>(_client, HttpMethod.Get, 
                _getTextFragmentsData.Replace("$EditionId", editionId.ToString()), null);
            fragmentsResponse.EnsureSuccessStatusCode();
            while (!textFragments.textFragments.Any())
            {
                edition = await EditionHelpers.GetRandomEdition(_db, _client);
                editionId = edition.id;
                (fragmentsResponse, textFragments) = await HttpRequest.SendAsync<string, TextFragmentDataListDTO>(_client, HttpMethod.Get, 
                    _getTextFragmentsData.Replace("$EditionId", editionId.ToString()), null);
                fragmentsResponse.EnsureSuccessStatusCode();
            }
            var textFragmentIdx = ListHelpers.RandomIdx(textFragments.textFragments);
            return (editionId, textFragments.textFragments[textFragmentIdx].colId);
        }

        private async Task<(uint editionId, uint textFragmentId, uint lineId)> _getLine()
        {
            uint editionId = 0, textFragmentId = 0;
            var (lineResponse, lines) = (new HttpResponseMessage(), new LineDataListDTO(new List<LineDataDTO>()));
            while (editionId == 0 || textFragmentId == 0 || lines == null || !lines.lines.Any())
            {
                (editionId, textFragmentId) = await _getTextFragment();
                (lineResponse, lines) = await HttpRequest.SendAsync<string, LineDataListDTO>(_client, HttpMethod.Get, 
                    _getTextLinesData.Replace("$EditionId", editionId.ToString())
                        .Replace("$TextFragmentId", textFragmentId.ToString()), null);
                lineResponse.EnsureSuccessStatusCode();
            }
            
            var lineIdx = ListHelpers.RandomIdx(lines.lines);
            return (editionId, textFragmentId, lines.lines[lineIdx].lineId);
        }

        private static void _verifyLineTextDTO(LineTextDTO msg)
        {
            Assert.NotNull(msg.licence);
            Assert.NotEqual((uint)0, msg.lineId);
            Assert.NotEmpty(msg.signs);
            Assert.NotEqual((uint)0, msg.signs.First().signId);
            Assert.NotEmpty(msg.signs.First().signChars);
            Assert.NotEqual((uint)0, msg.signs.First().signChars.First().signCharId);
            Assert.NotEmpty(msg.signs.First().signChars.First().attributes);
            Assert.NotEqual((uint)0, msg.signs.First().signChars.First().attributes.First().charAttributeId);
        }

        private static void _verifyTextEditionDTO(TextEditionDTO msg)
        {
            Assert.NotNull(msg.licence);
            Assert.NotEqual((uint)0, msg.manuscriptId);
            Assert.NotEmpty(msg.textFragments);
            Assert.NotEqual((uint)0, msg.textFragments.First().textFragmentId);
            Assert.NotEmpty(msg.textFragments.First().lines);
            Assert.NotEqual((uint)0, msg.textFragments.First().lines.First().lineId);
            Assert.NotEmpty(msg.textFragments.First().lines.First().signs);
            Assert.NotEqual((uint)0, msg.textFragments.First().lines.First().signs.First().signId);
            Assert.NotEmpty(msg.textFragments.First().lines.First().signs.First().signChars);
            Assert.NotEqual((uint)0, msg.textFragments.First().lines.First().signs.First().signChars.First().signCharId);
            Assert.NotEmpty(msg.textFragments.First().lines.First().signs.First().signChars.First().attributes);
            Assert.NotEqual((uint)0, msg.textFragments.First().lines.First().signs.First().signChars.First().attributes.First().charAttributeId);

        }
        #endregion Helpers
    }
}