using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using Dapper;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.ApiTest.Helpers
{
    public static class EditionHelpers
    {
        private static readonly Faker _faker = new Faker("en");
        private const string version = "v1";
        private const string controller = "editions";
        
        /// <summary>
        /// Searches randomly for an edition and returns it.
        /// </summary>
        /// <param name="userId">Id of the user whose editions should be randomly selected.</param>
        /// <param name="jwt">A JWT can be added the request to access private editions.</param>
        /// <returns></returns>
        public static async Task<EditionDTO> GetRandomEdition(DatabaseQuery db, HttpClient client, uint userId = 1, string jwt = null)
        {
            const string sql = @"
SELECT edition_id 
FROM edition 
JOIN edition_editor USING(edition_id)
WHERE user_id = @UserId";
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            var allUserEditions = (await db.RunQueryAsync<uint>(sql, parameters)).ToList();
            var (response, editionResponse) = (new HttpResponseMessage(), new EditionGroupDTO());
            while (editionResponse?.primary == null)
            {
                var randomEdition = allUserEditions[_faker.Random.Int(0, allUserEditions.Count - 1)];
                var url = $"/{version}/{controller}/{randomEdition}";
                (response, editionResponse) = await HttpRequest.SendAsync<string, EditionGroupDTO>(client,
                    HttpMethod.Get, url, null, jwt);
                response.EnsureSuccessStatusCode();
            }

            return editionResponse.primary;
        }
    }
}