using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

// TODO: It would be nice to be able to generate random polygons for these testing purposes.
namespace SQE.ApiTest
{
    /// <summary>
    ///     This test suite tests all the current endpoints in the ArtefactController
    /// </summary>
    public class ArtefactTests : WebControllerTest
    {
        public ArtefactTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
        }

        private readonly DatabaseQuery _db;
        private readonly WKTReader _wkr = new WKTReader();

        private const string version = "v1";
        private const string controller = "artefacts";
        // artefactEditions is a list of all editions that have artefacts
        private static readonly uint[] artefactEditions = {1,3,22,37,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,89,90,91,92,93,94,95,96,98,99,100,102,103,104,105,106,107,108,109,111,114,115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,144,147,148,149,150,151,152,153,154,155,156,157,158,159,160,161,163,164,165,166,167,169,170,171,209,210,211,213,214,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,237,238,239,240,241,242,243,244,245,246,247,248,249,250,251,252,253,254,255,256,257,258,259,260,261,262,263,264,265,266,269,270,271,272,273,274,275,277,278,280,282,288,289,290,291,293,294,295,298,299,300,301,302,303,304,305,306,307,308,312,316,317,318,321,323,324,326,327,328,329,331,332,333,334,335,337,338,339,340,341,342,343,345,346,349,350,351,352,354,355,356,357,358,359,361,362,363,365,366,367,368,369,370,371,372,373,374,375,376,377,378,379,380,381,382,383,387,388,389,390,391,392,393,394,395,396,397,398,399,400,401,403,404,405,406,407,408,409,412,413,414,415,420,421,422,423,424,425,426,427,428,429,430,431,432,433,434,435,436,445,470,471,472,476,478,479,481,483,485,486,487,493,494,495,496,497,502,503,504,505,506,509,511,512,513,515,518,519,521,522,524,525,526,528,529,530,531,532,533,534,535,536,537,538,539,540,541,542,543,544,545,546,547,548,549,550,551,552,553,554,555,556,557,558,559,560,561,567,568,569,570,572,573,574,575,576,577,578,579,580,581,582,583,584,585,586,587,589,590,591,592,593,594,595,596,597,598,615,616,617,618,619,620,621,622,623,624,625,626,627,628,629,630,631,632,633,634,635,636,637,638,639,641,642,643,644,645,646,647,648,649,650,651,653,654,655,656,657,658,659,660,661,719,720,721,725,728,742,743,744,746,747,748,751,755,757,760,761,762,763,764,765,766,767,768,769,770,771,772,773,774,798,800,806,810,811,818,819,820,821,822,823,824,825,826,827,828,829,830,831,832,833,834,835,836,837,838,839,840,842,843,844,845,848,849,850,851,852,853,854,855,856,857,858,859,860,861,862,863,864,865,868,870,871,872,873,874,875,876,877,878,879,881,882,883,884,885,886,887,888,889,890,891,892,893,894,897,898,899,900,901,902,903,904,905,906,907,909,910,911,912,913,914,915,916,918,919,920,923,924,925,926,927,928,929,930,931,932,933,934,935,936,937,939,940,941,942,943,944,945,946,947,948,953,955,956,957,958,959,960,961,962,963,964,965,967,968,969,970,972,973,974,975,976,983,988,989,992,993,994,995,996,1000,1003,1011,1012,1013,1014,1015,1016,1017,1018,1019,1021,1022,1023,1024,1028,1029,1034,1035,1037,1040,1041,1042,1044,1045,1046,1048,1049,1050,1054,1058,1059,1060,1062,1063,1064,1065,1066,1067,1069,1070,1071,1073,1075,1076,1077,1078,1079,1080,1081,1082,1083,1084,1085,1087,1089,1090,1091,1092,1093,1094,1095,1097,1098,1099,1101,1102,1103,1106,1107,1108,1109,1110,1111,1112,1113,1114,1115,1116,1117,1118,1119,1120,1122,1123,1124,1125,1127,1129,1130,1131,1132,1133,1134,1135,1136,1137,1139,1159,1160,1161,1162,1163,1164,1165,1167,1169,1174,1176,1179,1180,1181,1182,1187,1188,1189,1190,1191,1192,1196,1197,1198,1199,1201,1208,1209,1211,1214,1215,1216,1217,1225,1226,1227,1228,1229,1230,1236,1239,1240,1242,1243,1244,1245,1246,1253,1256,1257,1258,1259,1260,1261,1262,1263,1264,1265,1266,1267,1268,1269,1271,1274,1279,1280,1281,1285,1286,1287,1288,1290,1291,1292,1293,1294,1295,1296,1298,1299,1300,1301,1303,1304,1305,1306,1307,1308,1309,1310,1311,1312,1313,1314,1315,1316,1317,1319,1320,1321,1322,1323,1324,1325,1326,1327,1328,1329,1330,1331,1608,1609,1610,1611,1614,1615,1616,1626,1628,1629,1634,1635,1636,1637,1638,1640,1641,1642,1643,1644
};
        private static uint artefactCount;

        /// <summary>
        ///     Selects an edition with artefacts in sequence (to avoid locks) and returns its artefacts.
        /// </summary>
        /// <param name="userId">Id of the user whose editions should be randomly selected.</param>
        /// <param name="jwt">A JWT can be added the request to access private editions.</param>
        /// <returns></returns>
        private async Task<ArtefactListDTO> GetEditionArtefacts()
        {
            var editionId = artefactEditions[(int)artefactCount % (artefactEditions.Length + 1)];
            var url = $"/{version}/editions/{editionId}/{controller}?optional=masks";
            var (response, artefactResponse) = await Request.SendHttpRequestAsync<string, ArtefactListDTO>(
                _client,
                HttpMethod.Get,
                url,
                null,
                null
            );
            response.EnsureSuccessStatusCode();

            artefactCount++;
            return artefactResponse;
        }

        private async Task DeleteArtefact(uint editionId, uint ArtefactId)
        {
            var (response, _) = await Request.SendHttpRequestAsync<string, string>(
                _client,
                HttpMethod.Delete,
                $"/{version}/editions/{editionId}/{controller}/{ArtefactId}",
                null,
                await Request.GetJwtViaHttpAsync(_client)
            );
            response.EnsureSuccessStatusCode();
        }


        private static (decimal scale, decimal rotate, int translateX, int translateY, int zIndex) ArtefactPosition()
        {
            return (1.1m, 45m, (int)34765, (int)556, (int)2);
        }

        /// <summary>
        ///     Check that at least some edition has a valid artefact.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanAccessArtefacts()
        {
            // Act
            var artefacts = (await GetEditionArtefacts()).artefacts;

            // Assert
            Assert.NotEmpty(artefacts);
            var artefact = artefacts.First();
            Assert.True(artefact.editionId > 0);
            Assert.True(artefact.id > 0);
            Assert.NotNull(artefact.imagedObjectId);
            Assert.NotNull(artefact.side);
            Assert.NotNull(artefact.mask);
        }

        /// <summary>
        ///     Ensure that a new artefact can be created (and then deleted).
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanCreateArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetEditionArtefacts()).artefacts; // Find edition with artefacts
            var newEdition =
                await EditionHelpers.CreateCopyOfEdition(_client, allArtefacts.First().editionId); // Clone it

            const string masterImageSQL = "SELECT sqe_image_id FROM SQE_image WHERE type = 0 ORDER BY RAND() LIMIT 1";
            var masterImageId = await _db.RunQuerySingleAsync<uint>(masterImageSQL, null);
            const string newArtefactShape =
                "POLYGON((0 0,0 200,200 200,200 0,0 0),(5 5,25 5,25 25,5 25,5 5),(77 80,102 80,102 92,77 92,77 80))";
            var (newScale, newRotate, newTranslateX, newTranslateY, newZIdx) = ArtefactPosition();
            var newName = "CanCreateArtefacts.artefact א";
            var newArtefact = new CreateArtefactDTO
            {
                mask = newArtefactShape,
                placement = new PlacementDTO()
                {
                    scale = newScale,
                    rotate = newRotate,
                    translate = new TranslateDTO
                    {
                        x = newTranslateX,
                        y = newTranslateY
                    },
                    zIndex = newZIdx
                },
                name = newName,
                masterImageId = masterImageId,
                statusMessage = null
            };
            const string defaultStatusMessage = "New";

            // Act
            var (response, writtenArtefact) = await Request.SendHttpRequestAsync<CreateArtefactDTO, ArtefactDTO>(
                _client,
                HttpMethod.Post,
                $"/{version}/editions/{newEdition}/{controller}",
                newArtefact,
                await Request.GetJwtViaHttpAsync(_client)
            );

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(newEdition, writtenArtefact.editionId);
            Assert.Equal(newArtefact.mask, writtenArtefact.mask);
            Assert.Equal(newScale, writtenArtefact.placement.scale);
            Assert.Equal(newRotate, writtenArtefact.placement.rotate);
            Assert.Equal(newTranslateX, writtenArtefact.placement.translate.x);
            Assert.Equal(newTranslateY, writtenArtefact.placement.translate.y);
            Assert.Equal(newArtefact.name, writtenArtefact.name);
            Assert.Equal(defaultStatusMessage, writtenArtefact.statusMessage);

            // Cleanup
            await DeleteArtefact(newEdition, writtenArtefact.id);

            // Arrange
            newName = null;

            newArtefact = new CreateArtefactDTO
            {
                mask = newArtefactShape,
                placement = new PlacementDTO
                {
                    scale = newScale,
                    rotate = newRotate,
                    translate = new TranslateDTO
                    {
                        x = newTranslateX,
                        y = newTranslateY
                    }
                },
                name = newName,
                masterImageId = masterImageId
            };

            // Act
            (response, writtenArtefact) = await Request.SendHttpRequestAsync<CreateArtefactDTO, ArtefactDTO>(
                _client,
                HttpMethod.Post,
                $"/{version}/editions/{newEdition}/{controller}",
                newArtefact,
                await Request.GetJwtViaHttpAsync(_client)
            );

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(newEdition, writtenArtefact.editionId);
            Assert.Equal(newArtefact.mask, writtenArtefact.mask);
            Assert.Equal(newScale, writtenArtefact.placement.scale);
            Assert.Equal(newRotate, writtenArtefact.placement.rotate);
            Assert.Equal(newTranslateX, writtenArtefact.placement.translate.x);
            Assert.Equal(newTranslateY, writtenArtefact.placement.translate.y);
            Assert.Equal("", writtenArtefact.name);

            // Cleanup
            await DeleteArtefact(newEdition, writtenArtefact.id);
        }

        /// <summary>
        ///     Ensure that a existing artefact can be deleted.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanDeleteArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it

            // Act
            var (response, writtenArtefact) = await Request.SendHttpRequestAsync<string, string>(
                _client,
                HttpMethod.Delete,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
                null,
                await Request.GetJwtViaHttpAsync(_client)
            );

            // Assert
            response.EnsureSuccessStatusCode();
            // Ensure successful nocontent status
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            // Double check that it is really gone
            var (delResponse, _) = await Request.SendHttpRequestAsync<string, string>(
                _client,
                HttpMethod.Get,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
                null,
                await Request.GetJwtViaHttpAsync(_client)
            );
            Assert.Equal(HttpStatusCode.NotFound, delResponse.StatusCode);

            await EditionHelpers.DeleteEdition(_client, newEdition);
        }

        [Fact]
        public async Task CanGetSuggestedTextFragmentForArtefact()
        {
            // Arrange
            const uint editionId = 894;
            const uint artefactId = 10058;
            var path = $"/{version}/editions/{editionId}/{controller}/{artefactId}/text-fragments?optional=suggested";

            // Act
            var (tfResponse, tfData) = await Request.SendHttpRequestAsync<string, ArtefactTextFragmentMatchListDTO>(
                _client,
                HttpMethod.Get,
                path,
                null
            );

            // Assert
            tfResponse.EnsureSuccessStatusCode();
            Assert.NotEmpty(tfData.textFragments);
            Assert.Equal((uint)10029, tfData.textFragments.First().id);
            Assert.Equal("frg. 78_79", tfData.textFragments.First().name);
            Assert.Equal((uint)894, tfData.textFragments.First().editorId);
        }

        /// <summary>
        ///     Ensure that a new artefact cannot be created in an edition not owned by the current user.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotCreateArtefactsOnUnownedEdition()
        {
            // Arrange
            var allArtefacts = (await GetEditionArtefacts()).artefacts; // Find edition with artefacts
            var newEdition =
                await EditionHelpers.CreateCopyOfEdition(_client, allArtefacts.First().editionId); // Clone it

            const string masterImageSQL = "SELECT sqe_image_id FROM SQE_image WHERE type = 0 ORDER BY RAND() LIMIT 1";
            var masterImageId = await _db.RunQuerySingleAsync<uint>(masterImageSQL, null);
            const string newArtefactShape =
                "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";
            var (newScale, newRotate, newTranslateX, newTranslateY, newZIdx) = ArtefactPosition();
            const string newName = "CanCreateArtefacts.artefact α";
            ;
            var newArtefact = new CreateArtefactDTO
            {
                mask = newArtefactShape,
                placement = new PlacementDTO
                {
                    scale = newScale,
                    rotate = newRotate,
                    translate = new TranslateDTO
                    {
                        x = newTranslateX,
                        y = newTranslateY
                    },
                    zIndex = newZIdx
                },
                name = newName,
                masterImageId = masterImageId
            };

            // Act
            var (response, _) = await Request.SendHttpRequestAsync<CreateArtefactDTO, ArtefactDTO>(
                _client,
                HttpMethod.Post,
                $"/{version}/editions/{newEdition}/{controller}",
                newArtefact
            );

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            await EditionHelpers.DeleteEdition(_client, newEdition);
        }

        /// <summary>
        /// Ensure that attempts to write invalid polygons are rejected
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotCreateMalformedArtefact()
        {
            // Arrange
            var allArtefacts = (await GetEditionArtefacts()).artefacts; // Find edition with artefacts
            var newEdition =
                await EditionHelpers.CreateCopyOfEdition(_client, allArtefacts.First().editionId); // Clone it

            const string masterImageSQL = "SELECT sqe_image_id FROM SQE_image WHERE type = 0 ORDER BY RAND() LIMIT 1";
            var masterImageId = await _db.RunQuerySingleAsync<uint>(masterImageSQL, null);
            // This is a self-intersecting polygon
            const string newArtefactShape =
                "POLYGON ((0 0, 30 110, 95 109, 146 64, 195 127, 150 210, 280 240, 150 170, 144 105, 75 84, 63 25, 0 0))";
            var (newScale, newRotate, newTranslateX, newTranslateY, newZIdx) = ArtefactPosition();
            var newName = "CannotCreateMalformedArtefact.artefact א";
            var newArtefact = new CreateArtefactDTO
            {
                mask = newArtefactShape,
                placement = new PlacementDTO
                {
                    scale = newScale,
                    rotate = newRotate,
                    translate = new TranslateDTO
                    {
                        x = newTranslateX,
                        y = newTranslateY
                    },
                    zIndex = newZIdx
                },
                name = newName,
                masterImageId = masterImageId,
                statusMessage = null
            };

            // Act
            var newArtefactObject = new Post.V1_Editions_EditionId_Artefacts(newEdition, newArtefact);
            var (artefactResponse, artefact, _, _) =
                await Request.Send(
                    newArtefactObject,
                    _client,
                    auth: true,
                    shouldSucceed: false
                );

            // Assert
            // The response should indicate a bad request
            Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);

            // Test bad scale
            newArtefact = new CreateArtefactDTO
            {
                mask = newArtefactShape,
                placement = new PlacementDTO
                {
                    scale = 100m, // 0–99.9999 is allowed range
                    rotate = newRotate,
                    translate = new TranslateDTO
                    {
                        x = newTranslateX,
                        y = newTranslateY
                    },
                    zIndex = newZIdx
                },
                name = newName,
                masterImageId = masterImageId,
                statusMessage = null
            };

            // Act
            newArtefactObject = new Post.V1_Editions_EditionId_Artefacts(newEdition, newArtefact);
            (artefactResponse, artefact, _, _) =
                await Request.Send(
                    newArtefactObject,
                    _client,
                    auth: true,
                    shouldSucceed: false
                );

            // Assert
            // The response should indicate a bad request
            Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
            var resp = await artefactResponse.Content.ReadAsStringAsync();
            Assert.True(resp.Contains("The scale must be between 0.1 and 99.9999"));

            // Test scale has improper decimal value
            newArtefact = new CreateArtefactDTO
            {
                mask = newArtefactShape,
                placement = new PlacementDTO
                {
                    scale = 2.43567m,
                    rotate = newRotate, // 0–9999.99 is the only allowable range
                    translate = new TranslateDTO
                    {
                        x = newTranslateX,
                        y = newTranslateY
                    },
                    zIndex = newZIdx
                },
                name = newName,
                masterImageId = masterImageId,
                statusMessage = null
            };

            // Act
            newArtefactObject = new Post.V1_Editions_EditionId_Artefacts(newEdition, newArtefact);
            (artefactResponse, artefact, _, _) =
                await Request.Send(
                    newArtefactObject,
                    _client,
                    auth: true,
                    shouldSucceed: false
                );

            // Assert
            // The response should indicate a bad request
            Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
            resp = await artefactResponse.Content.ReadAsStringAsync();
            Assert.True(resp.Contains("The scale cannot have more than 2 digits to the left of the decimal and 4 digits to the right"));

            // Test rotate has improper decimal value
            newArtefact = new CreateArtefactDTO
            {
                mask = newArtefactShape,
                placement = new PlacementDTO
                {
                    scale = newScale,
                    rotate = 180.4576m, // 0–9999.99 is the only allowable range
                    translate = new TranslateDTO
                    {
                        x = newTranslateX,
                        y = newTranslateY
                    },
                    zIndex = newZIdx
                },
                name = newName,
                masterImageId = masterImageId,
                statusMessage = null
            };

            // Act
            newArtefactObject = new Post.V1_Editions_EditionId_Artefacts(newEdition, newArtefact);
            (artefactResponse, artefact, _, _) =
                await Request.Send(
                    newArtefactObject,
                    _client,
                    auth: true,
                    shouldSucceed: false
                );

            // Assert
            // The response should indicate a bad request
            Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
            resp = await artefactResponse.Content.ReadAsStringAsync();
            Assert.True(resp.Contains("The rotate cannot have more than 4 digits to the left of the decimal and 2 digits to the right"));

            // Test rotate out of range
            newArtefact = new CreateArtefactDTO
            {
                mask = newArtefactShape,
                placement = new PlacementDTO
                {
                    scale = newScale,
                    rotate = -180.45m, // 0–9999.99 is the only allowable range
                    translate = new TranslateDTO
                    {
                        x = newTranslateX,
                        y = newTranslateY
                    },
                    zIndex = newZIdx
                },
                name = newName,
                masterImageId = masterImageId,
                statusMessage = null
            };

            // Act
            newArtefactObject = new Post.V1_Editions_EditionId_Artefacts(newEdition, newArtefact);
            (artefactResponse, artefact, _, _) =
                await Request.Send(
                    newArtefactObject,
                    _client,
                    auth: true,
                    shouldSucceed: false
                );

            // Assert
            // The response should indicate a bad request
            Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
            resp = await artefactResponse.Content.ReadAsStringAsync();
            Assert.True(resp.Contains("The rotate must be between 0 and 360"));

            // Cannot create an artefact without a mask
            // Arrange
            newName = "CannotCreateArtefacts.artefact ב";

            newArtefact = new CreateArtefactDTO
            {
                mask = null,
                placement = new PlacementDTO
                {
                    scale = newScale,
                    rotate = newRotate,
                    translate = new TranslateDTO
                    {
                        x = newTranslateX,
                        y = newTranslateY
                    }
                },
                name = newName,
                masterImageId = masterImageId
            };

            // Act
            (artefactResponse, artefact) = await Request.SendHttpRequestAsync<CreateArtefactDTO, ArtefactDTO>(
                _client,
                HttpMethod.Post,
                $"/{version}/editions/{newEdition}/{controller}",
                newArtefact,
                await Request.GetJwtViaHttpAsync(_client)
            );

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
            resp = await artefactResponse.Content.ReadAsStringAsync();
            Assert.True(resp.Contains("The mask field is required."));

            // Cannot create an artefact without a mask
            // Arrange
            newName = "CannotCreateArtefacts.artefact ב";

            newArtefact = new CreateArtefactDTO
            {
                mask = "PLYGON((0 0,10 0,10 10,0 10,0 0))",
                placement = new PlacementDTO
                {
                    scale = newScale,
                    rotate = newRotate,
                    translate = new TranslateDTO
                    {
                        x = newTranslateX,
                        y = newTranslateY
                    }
                },
                name = newName,
                masterImageId = masterImageId
            };

            // Act
            (artefactResponse, artefact) = await Request.SendHttpRequestAsync<CreateArtefactDTO, ArtefactDTO>(
                _client,
                HttpMethod.Post,
                $"/{version}/editions/{newEdition}/{controller}",
                newArtefact,
                await Request.GetJwtViaHttpAsync(_client)
            );

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
            resp = await artefactResponse.Content.ReadAsStringAsync();
            Assert.True(resp.Contains("The mask must be a valid WKT POLYGON description."));
        }

        /// <summary>
        ///     Ensure that a existing artefact cannot be deleted by a use who does not have access to it.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotDeleteUnownedArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it

            // Act
            var (response, _) = await Request.SendHttpRequestAsync<string, string>(
                _client,
                HttpMethod.Delete,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
                null
            );

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            await EditionHelpers.DeleteEdition(_client, newEdition);
        }

        /// <summary>
        ///     Ensure that a existing artefact cannot be updated by a user who does not have access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotUpdateUnownedArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it
            const string newArtefactName = "CannotUpdateUnownedArtefacts.artefact 😈";

            // Act (update name)
            var (nameResponse, _) = await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                _client,
                HttpMethod.Put,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
                new UpdateArtefactDTO
                {
                    mask = null,
                    placement = null,
                    name = newArtefactName
                }
            );

            // Assert (update name)
            Assert.Equal(HttpStatusCode.Unauthorized, nameResponse.StatusCode);

            await EditionHelpers.DeleteEdition(_client, newEdition);
        }

        /// <summary>
        ///     Ensure that a existing artefact can be updated.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it
            const string newArtefactName = "CanUpdateArtefacts.artefact +%%$^";
            var (newScale, newRotate, newTranslateX, newTranslateY, newZIdx) = ArtefactPosition();
            const string newArtefactShape =
                "POLYGON((0 0,0 200,200 200,200 0,0 0),(5 5,25 5,25 25,5 25,5 5),(77 80,102 80,102 92,77 92,77 80))";
            const string statusMessage = "Fully examined";

            // Act (update name and set status)
            var (nameResponse, updatedNameArtefact) =
                await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                    _client,
                    HttpMethod.Put,
                    $"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
                    new UpdateArtefactDTO
                    {
                        mask = null,
                        placement = null,
                        name = newArtefactName,
                        statusMessage = statusMessage
                    },
                    await Request.GetJwtViaHttpAsync(_client)
                );

            // Assert (update name and set status)
            nameResponse.EnsureSuccessStatusCode();
            Assert.False(updatedNameArtefact.isPlaced);
            Assert.Null(updatedNameArtefact.placement.translate);
            Assert.Equal(0, updatedNameArtefact.placement.rotate); // Expect the default value
            Assert.Equal(0, updatedNameArtefact.placement.zIndex); // Expect the default value
            Assert.Equal(1, updatedNameArtefact.placement.scale); // Expect the default value
            Assert.NotEqual(artefact.name, updatedNameArtefact.name);
            Assert.True(string.IsNullOrEmpty(updatedNameArtefact.mask)); // The mask was not updated so we don't send that back
            Assert.Equal(newArtefactName, updatedNameArtefact.name);
            Assert.Equal(statusMessage, updatedNameArtefact.statusMessage);

            // Act (update position)
            var (positionResponse, updatedPositionArtefact) =
                await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                    _client,
                    HttpMethod.Put,
                    $"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
                    new UpdateArtefactDTO
                    {
                        mask = null,
                        placement = new PlacementDTO
                        {
                            scale = newScale,
                            rotate = newRotate,
                            translate = new TranslateDTO
                            {
                                x = newTranslateX,
                                y = newTranslateY
                            },
                            zIndex = newZIdx
                        },
                        name = null
                    },
                    await Request.GetJwtViaHttpAsync(_client)
                );

            // Assert (update position)
            positionResponse.EnsureSuccessStatusCode();
            Assert.NotEqual(artefact.placement.scale, updatedPositionArtefact.placement.scale);
            Assert.NotEqual(artefact.placement.rotate, updatedPositionArtefact.placement.rotate);
            Assert.NotNull(updatedPositionArtefact.placement.translate.x);
            Assert.NotNull(updatedPositionArtefact.placement.translate.y);
            Assert.Equal(newScale, updatedPositionArtefact.placement.scale);
            Assert.Equal(newRotate, updatedPositionArtefact.placement.rotate);
            Assert.Equal(newTranslateX, updatedPositionArtefact.placement.translate.x);
            Assert.Equal(newTranslateY, updatedPositionArtefact.placement.translate.y);
            Assert.Equal(newZIdx, updatedPositionArtefact.placement.zIndex);
            Assert.Equal(newArtefactName, updatedPositionArtefact.name);

            // Act (update shape)
            var (shapeResponse, updatedShapeArtefact) =
                await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                    _client,
                    HttpMethod.Put,
                    $"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
                    new UpdateArtefactDTO
                    {
                        mask = newArtefactShape,
                        placement = new PlacementDTO
                        {
                            scale = newScale,
                            rotate = newRotate,
                            translate = new TranslateDTO
                            {
                                x = newTranslateX,
                                y = newTranslateY
                            }
                        },
                        name = null
                    },
                    await Request.GetJwtViaHttpAsync(_client)
                );

            // Assert (update shape)
            shapeResponse.EnsureSuccessStatusCode();
            Assert.NotEqual(artefact.mask, updatedShapeArtefact.mask);
            Assert.Equal(newArtefactShape, updatedShapeArtefact.mask);
            Assert.Equal(newScale, updatedShapeArtefact.placement.scale);
            Assert.Equal(newRotate, updatedShapeArtefact.placement.rotate);
            Assert.Equal(newTranslateX, updatedShapeArtefact.placement.translate.x);
            Assert.Equal(newTranslateY, updatedShapeArtefact.placement.translate.y);
            Assert.Equal(newArtefactName, updatedShapeArtefact.name);

            // Arrange (update all)
            var (otherScale, otherRotate, otherTranslateX, otherTranslateY, otherzIdx) = ArtefactPosition();
            // Act (update all)
            var (allResponse, updatedAllArtefact) = await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                _client,
                HttpMethod.Put,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
                new UpdateArtefactDTO
                {
                    mask = artefact.mask,
                    placement = new PlacementDTO
                    {
                        scale = otherScale,
                        rotate = otherRotate,
                        translate = new TranslateDTO
                        {
                            x = otherTranslateX,
                            y = otherTranslateY
                        },
                        zIndex = otherzIdx
                    },
                    name = artefact.name
                },
                await Request.GetJwtViaHttpAsync(_client)
            );

            // Assert (update all)
            allResponse.EnsureSuccessStatusCode();
            Assert.True(_wkr.Read(artefact.mask).EqualsNormalized(_wkr.Read(updatedAllArtefact.mask)));
            Assert.Equal(otherScale, updatedAllArtefact.placement.scale);
            Assert.Equal(otherRotate, updatedAllArtefact.placement.rotate);
            Assert.Equal(otherTranslateX, updatedAllArtefact.placement.translate.x);
            Assert.Equal(otherTranslateY, updatedAllArtefact.placement.translate.y);
            Assert.Equal(otherzIdx, updatedAllArtefact.placement.zIndex);
            Assert.Equal(artefact.name, updatedAllArtefact.name);

            await EditionHelpers.DeleteEdition(_client, newEdition);
        }

        /// <summary>
        ///     Ensure that a existing artefact can be placed and unplaced.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanBatchUnplaceArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it
            var placement = new PlacementDTO
            {
                scale = (decimal)1.0,
                rotate = (decimal)0.0,
                translate = new TranslateDTO
                {
                    x = 100,
                    y = 223
                },
                zIndex = 0
            };
            // Act (update position)
            var (updateResponse, updatedArtefacts) =
                await Request.SendHttpRequestAsync<BatchUpdateArtefactPlacementDTO, BatchUpdatedArtefactTransformDTO>(
                    _client,
                    HttpMethod.Post,
                    $"/{version}/editions/{newEdition}/{controller}/batch-transformation",
                    new BatchUpdateArtefactPlacementDTO
                    {
                        artefactPlacements = allArtefacts.Select(x => new UpdateArtefactPlacementDTO()
                        {
                            artefactId = x.id,
                            isPlaced = true,
                            placement = placement
                        }).ToList()
                    },
                    await Request.GetJwtViaHttpAsync(_client)
                );

            // Assert (update name and set status)
            updateResponse.EnsureSuccessStatusCode();
            foreach (var art in updatedArtefacts.artefactPlacements)
            {
                Assert.True(art.isPlaced);
                Assert.Equal(100, art.placement.translate.x);
                Assert.Equal(223, art.placement.translate.y);
                Assert.Equal(1, art.placement.scale);
                Assert.Equal(0, art.placement.rotate);
                Assert.Equal(0, art.placement.zIndex);
            }

            // Act (update remove x/y)
            placement = new PlacementDTO
            {
                scale = (decimal)1.0,
                rotate = (decimal)0.0,
                translate = null,
                zIndex = 0
            };
            (updateResponse, updatedArtefacts) =
                await Request.SendHttpRequestAsync<BatchUpdateArtefactPlacementDTO, BatchUpdatedArtefactTransformDTO>(
                    _client,
                    HttpMethod.Post,
                    $"/{version}/editions/{newEdition}/{controller}/batch-transformation",
                    new BatchUpdateArtefactPlacementDTO
                    {
                        artefactPlacements = allArtefacts.Select(x => new UpdateArtefactPlacementDTO()
                        {
                            artefactId = x.id,
                            isPlaced = false,
                            placement = placement
                        }).ToList()
                    },
                    await Request.GetJwtViaHttpAsync(_client)
                );

            // Assert (update name and set status)
            updateResponse.EnsureSuccessStatusCode();
            foreach (var art in updatedArtefacts.artefactPlacements)
            {
                Assert.False(art.isPlaced);
                Assert.Null(art.placement.translate);
                Assert.Equal(placement.scale, art.placement.scale);
                Assert.Equal(placement.rotate, art.placement.rotate);
                Assert.Equal(placement.zIndex, art.placement.zIndex);
            }

            // Act (full remove of position)
            (updateResponse, updatedArtefacts) =
                await Request.SendHttpRequestAsync<BatchUpdateArtefactPlacementDTO, BatchUpdatedArtefactTransformDTO>(
                    _client,
                    HttpMethod.Post,
                    $"/{version}/editions/{newEdition}/{controller}/batch-transformation",
                    new BatchUpdateArtefactPlacementDTO
                    {
                        artefactPlacements = allArtefacts.Select(x => new UpdateArtefactPlacementDTO()
                        {
                            artefactId = x.id,
                            isPlaced = false,
                            placement = null
                        }).ToList()
                    },
                    await Request.GetJwtViaHttpAsync(_client)
                );

            // Assert (update name and set status)
            updateResponse.EnsureSuccessStatusCode();
            foreach (var art in updatedArtefacts.artefactPlacements)
            {
                Assert.False(art.isPlaced);
                Assert.Null(art.placement.translate);
                Assert.Equal(1, art.placement.scale);
                Assert.Equal(0, art.placement.rotate);
                Assert.Equal(0, art.placement.zIndex);
            }

            await EditionHelpers.DeleteEdition(_client, newEdition);
        }

        /// <summary>
        ///     Ensure that improperly formatted artefact WKT masks are rejected.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RejectsUpdateToImproperArtefactShape()
        {
            // Arrange
            var allArtefacts = (await GetEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it
            const string newArtefactShape =
                "POLYGON(0 0,0 200,200 200,0 200,0 0),5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";

            // Act (update name)
            var (nameResponse, _) = await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                _client,
                HttpMethod.Put,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
                new UpdateArtefactDTO
                {
                    mask = newArtefactShape,
                    placement = null,
                    name = null
                },
                await Request.GetJwtViaHttpAsync(_client)
            );

            // Assert (update name)
            Assert.Equal(HttpStatusCode.BadRequest, nameResponse.StatusCode);

            await EditionHelpers.DeleteEdition(_client, newEdition);
        }
    }
}