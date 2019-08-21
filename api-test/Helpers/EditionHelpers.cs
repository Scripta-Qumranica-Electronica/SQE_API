using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using Dapper;
using SQE.SqeHttpApi.Server.DTOs;
using Xunit;

namespace SQE.ApiTest.Helpers
{
	public static class EditionHelpers
	{
		private const string version = "v1";
		private const string controller = "editions";
		private static readonly Faker _faker = new Faker();

		/// <summary>
		///     Searches randomly for an edition and returns it.
		/// </summary>
		/// <param name="userId">Id of the user whose editions should be randomly selected.</param>
		/// <param name="jwt">A JWT can be added the request to access private editions.</param>
		/// <returns>a randomly selected EditionDTO</returns>
		public static async Task<EditionDTO> GetRandomEdition(DatabaseQuery db,
			HttpClient client,
			uint userId = 1,
			string jwt = null)
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
				(response, editionResponse) = await HttpRequest.SendAsync<string, EditionGroupDTO>(
					client,
					HttpMethod.Get,
					url,
					null,
					jwt
				);
				response.EnsureSuccessStatusCode();
			}

			return editionResponse.primary;
		}

		/// <summary>
		///     Creates a new edition. This will be a copy of editionId 1 if no other editionId is entered.
		/// </summary>
		/// <param name="client">The HttpClient</param>
		/// <param name="editionId">Optional id of the edition to be cloned</param>
		/// <param name="username">Optional username for the user who will own the edition</param>
		/// <param name="pwd">Optional password for the user who will own the edition</param>
		/// <returns>The ID of the new edition</returns>
		public static async Task<uint> CreateCopyOfEdition(HttpClient client,
			uint editionId = 1,
			string username = null,
			string pwd = null)
		{
			var newScrollRequest = new EditionUpdateRequestDTO("test-name", null, null);
			var (response, msg) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionDTO>(
				client,
				HttpMethod.Post,
				$"/v1/editions/{editionId}",
				newScrollRequest,
				await HttpRequest.GetJWTAsync(client, username, pwd)
			);
			response.EnsureSuccessStatusCode();
			return msg.id;
		}

		/// <summary>
		///     Deletes an edition for all editors.
		/// </summary>
		/// <param name="client">The HttpClient</param>
		/// <param name="editionId"></param>
		/// <param name="authenticated">Optional, whether the request should be made by an authenticated user</param>
		/// <param name="shouldSucceed">Optional, whether the delete action is expected to succeed</param>
		/// <param name="email">Optional, the email of the user who is admin for the edition</param>
		/// <param name="pwd">Optional, the password of the user who is admin for the edition</param>
		/// <returns>void</returns>
		public static async Task DeleteEdition(HttpClient client,
			uint editionId,
			bool authenticated = false,
			bool shouldSucceed = true,
			string email = null,
			string pwd = null)
		{
			var (response, msg) = await HttpRequest.SendAsync<string, DeleteTokenDTO>(
				client,
				HttpMethod.Delete,
				$"/v1/editions/{editionId}?optional=deleteForAllEditors",
				null,
				authenticated ? await HttpRequest.GetJWTAsync(client, email, pwd) : null
			);
			if (shouldSucceed)
			{
				response.EnsureSuccessStatusCode();
				Assert.NotNull(msg.token);
				Assert.Equal(editionId, msg.editionId);
				var (response2, msg2) = await HttpRequest.SendAsync<string, DeleteTokenDTO>(
					client,
					HttpMethod.Delete,
					$"/v1/editions/{msg.editionId}?optional=deleteForAllEditors&token={msg.token}",
					null,
					authenticated ? await HttpRequest.GetJWTAsync(client, email, pwd) : null
				);
				response2.EnsureSuccessStatusCode();
				Assert.Null(msg2);
			}
		}
	}
}