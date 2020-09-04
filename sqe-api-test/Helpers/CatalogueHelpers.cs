using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using Xunit;

namespace SQE.ApiTest.Helpers
{
    public static class CatalogueHelpers
    {
        public static async Task<CatalogueMatchListDTO> GetImagedObjectsAndTextFragmentsOfEdition(
            uint editionId,
            HttpClient client,
            Func<string, Task<HubConnection>> signalr,
            Request.UserAuthDetails user = null)
        {
            // Act
            var requestobj = new Get.V1_Catalogue_Editions_EditionId_ImagedObjectTextFragmentMatches(editionId);
            await requestobj.Send(client, signalr);

            // Assert
            requestobj.HttpResponseObject.ShouldDeepEqual(requestobj.SignalrResponseObject);
            Assert.NotEmpty(requestobj.HttpResponseObject.matches);
            var firstMatch = requestobj.HttpResponseObject.matches.First();
            ConfirmValidMatch(firstMatch);

            return requestobj.HttpResponseObject;
        }

        public static async Task<CatalogueMatchListDTO> GetImagedObjectsAndTextFragmentsOfManuscript(
            uint manuscriptId,
            HttpClient client,
            Func<string, Task<HubConnection>> signalr,
            Request.UserAuthDetails user = null)
        {
            // Act
            var requestobj = new Get.V1_Catalogue_Manuscripts_ManuscriptId_ImagedObjectTextFragmentMatches(manuscriptId);
            await requestobj.Send(client, signalr);

            // Assert
            requestobj.HttpResponseObject.ShouldDeepEqual(requestobj.SignalrResponseObject);
            Assert.NotEmpty(requestobj.HttpResponseObject.matches);
            var firstMatch = requestobj.HttpResponseObject.matches.First();
            ConfirmValidMatch(firstMatch);

            return requestobj.HttpResponseObject;
        }

        /// <summary>
        /// Create a new text fragment to imaged object match.
        /// This will make the initial request via HTTP.
        /// Then it will make a second request via Signal to match the opposite side.
        /// </summary>
        /// <param name="catalogSide"></param>
        /// <param name="imagedObjectId"></param>
        /// <param name="manuscriptId"></param>
        /// <param name="editionName"></param>
        /// <param name="editionVolume"></param>
        /// <param name="editionLocation1"></param>
        /// <param name="editionLocation2"></param>
        /// <param name="editionSide"></param>
        /// <param name="comment"></param>
        /// <param name="textFragmentId"></param>
        /// <param name="editionId"></param>
        /// <param name="client"></param>
        /// <param name="signalr"></param>
        /// <returns></returns>
        public static async Task CreateImagedObjectTextFragmentMatch(
            SideDesignation catalogSide,
            string imagedObjectId,
            uint manuscriptId,
            string editionName,
            string editionVolume,
            string editionLocation1,
            string editionLocation2,
            SideDesignation editionSide,
            string comment,
            uint textFragmentId,
            uint editionId,
            HttpClient client,
            Func<string, Task<HubConnection>> signalr,
            bool? confirmed = false)
        {
            var match = new CatalogueMatchInputDTO()
            {
                catalogSide = catalogSide,
                imagedObjectId = imagedObjectId,
                manuscriptId = manuscriptId,
                editionName = editionName,
                editionVolume = editionVolume,
                editionLocation1 = editionLocation1,
                editionLocation2 = editionLocation2,
                editionSide = editionSide,
                comment = comment,
                textFragmentId = textFragmentId,
                editionId = editionId,
                confirmed = confirmed
            };

            // Make one request via HTTP
            var request = new Post.V1_Catalogue(match);
            await request.Send(client, null, auth: true, requestUser: Request.DefaultUsers.User1);
            request.HttpResponseMessage.EnsureSuccessStatusCode();

            // Make a second request via SignalR to confirm the match
            var match2 = match;
            match2.catalogSide = match2.catalogSide == SideDesignation.recto ? SideDesignation.verso : SideDesignation.recto;
            match2.editionSide = match2.editionSide == SideDesignation.recto ? SideDesignation.verso : SideDesignation.recto;
            var requestConf = new Post.V1_Catalogue(match2);
            await requestConf.Send(null, signalr, auth: true, requestUser: Request.DefaultUsers.User1);

            var matches = await GetImagedObjectsAndTextFragmentsOfEdition(editionId, client, signalr);
            Assert.Contains(matches.matches, x =>
                x.imagedObjectId == match.imagedObjectId
                && x.textFragmentId == match.textFragmentId
                && x.editionId == match.editionId
                && x.manuscriptId == match.manuscriptId);
            Assert.Contains(matches.matches, x =>
                x.imagedObjectId == match2.imagedObjectId
                && x.textFragmentId == match2.textFragmentId
                && x.editionId == match2.editionId
                && x.manuscriptId == match2.manuscriptId);
        }

        public static async Task ConfirmTextFragmentImagedObjectMatch(
            uint editionId,
            uint matchId,
            HttpClient client,
            Func<string, Task<HubConnection>> signalr,
            Request.UserAuthDetails user = null)
        {
            var request = new Post.V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId(matchId);
            await request.Send(client, signalr, auth: user != null, requestUser: user);

            var matchList = await GetImagedObjectsAndTextFragmentsOfEdition(editionId, client, signalr, user);
            Assert.Contains(matchList.matches, x => x.matchId == matchId && x.confirmed == true);
        }

        public static async Task UnconfirmTextFragmentImagedObjectMatch(
            uint editionId,
            uint matchId,
            HttpClient client,
            Func<string, Task<HubConnection>> signalr,
            Request.UserAuthDetails user = null)
        {
            var request = new Delete.V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId(matchId);
            await request.Send(client, signalr, auth: user != null, requestUser: user);

            var matchList = await GetImagedObjectsAndTextFragmentsOfEdition(editionId, client, signalr, user);
            Assert.Contains(matchList.matches, x => x.matchId == matchId && x.confirmed == false);
        }

        private static void ConfirmValidMatch(CatalogueMatchDTO match)
        {
            Assert.NotNull(match.matchAuthor);
            if (match.confirmed.HasValue)
            {
                Assert.NotNull(match.matchConfirmationAuthor);
                Assert.NotNull(match.dateOfConfirmation);
            }
            if (!match.confirmed.HasValue)
            {
                Assert.Null(match.matchConfirmationAuthor);
                Assert.Null(match.dateOfConfirmation);
            }
            Assert.NotNull(match.manuscriptName);
            Assert.NotNull(match.name);
            Assert.NotNull(match.imagedObjectId);
            Assert.NotNull(match.institution);
            Assert.NotNull(match.editionName);
            Assert.NotNull(match.filename);
            Assert.NotNull(match.thumbnail);
            Assert.NotNull(match.url);
            Assert.True(match.dateOfMatch < DateTime.Now);
        }
    }
}