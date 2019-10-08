using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.API.DTO;
using SQE.API.HTTP;
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

		private const string version = "v1";
		private const string controller = "artefacts";
		private static uint artefactCount;

		/// <summary>
		///     Searches randomly for an edition with artefacts and returns the artefacts.
		/// </summary>
		/// <param name="userId">Id of the user whose editions should be randomly selected.</param>
		/// <param name="jwt">A JWT can be added the request to access private editions.</param>
		/// <returns></returns>
		private async Task<ArtefactListDTO> GetEditionArtefacts(uint userId = 1, string jwt = null)
		{
			const string sql = @"
SELECT DISTINCT edition_id 
FROM edition 
JOIN edition_editor USING(edition_id)
JOIN artefact_shape_owner USING(edition_id)
JOIN artefact_shape USING(artefact_shape_id)
WHERE user_id = @UserId AND sqe_image_id IS NOT NULL";
			var parameters = new DynamicParameters();
			parameters.Add("@UserId", userId);
			var allUserEditions = (await _db.RunQueryAsync<uint>(sql, parameters)).ToList();

			var editionId = allUserEditions[(int)artefactCount % (allUserEditions.Count + 1)];
			var url = $"/{version}/editions/{editionId}/{controller}?optional=masks";
			var (response, artefactResponse) = await HttpRequest.SendAsync<string, ArtefactListDTO>(
				_client,
				HttpMethod.Get,
				url,
				null,
				jwt
			);
			response.EnsureSuccessStatusCode();

			artefactCount++;
			return artefactResponse;
		}

		private async Task DeleteArtefact(uint editionId, uint ArtefactId)
		{
			var (response, _) = await HttpRequest.SendAsync<string, string>(
				_client,
				HttpMethod.Delete,
				$"/{version}/editions/{editionId}/{controller}/{ArtefactId}",
				null,
				await HttpRequest.GetJWTAsync(_client)
			);
			response.EnsureSuccessStatusCode();
		}


		private (float? scale, float? rotate, uint translateX, uint translateY) ArtefactPosition()
		{
			return ((float?)1.0, (float?)0, (uint)34765, (uint)556);
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
			Assert.True(artefact.zOrder > -256 && artefact.zOrder < 256);
			Assert.NotNull(artefact.imagedObjectId);
			Assert.NotNull(artefact.side);
			Assert.NotNull(artefact.mask.mask);
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
				"POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";
			var (newScale, newRotate, newTranslateX, newTranslateY) = ArtefactPosition();
			var newName = "CanCreateArtefacts.artefact א";
			var newArtefact = new CreateArtefactDTO
			{
				polygon = new PolygonDTO
				{
					mask = newArtefactShape,
					transformation = new TransformationDTO
					{
						scale = newScale,
						rotate = newRotate,
						translate = new TranslateDTO
						{
							x = newTranslateX,
							y = newTranslateY
						}
					}
				},
				name = newName,
				masterImageId = masterImageId,
				statusMessage = null
			};
			const string defaultStatusMessage = "New";

			// Act
			var (response, writtenArtefact) = await HttpRequest.SendAsync<CreateArtefactDTO, ArtefactDTO>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/{controller}",
				newArtefact,
				await HttpRequest.GetJWTAsync(_client)
			);

			// Assert
			response.EnsureSuccessStatusCode();
			Assert.Equal(newEdition, writtenArtefact.editionId);
			Assert.Equal(newArtefact.polygon.mask, writtenArtefact.mask.mask);
			Assert.Equal(newScale, writtenArtefact.mask.transformation.scale);
			Assert.Equal(newRotate, writtenArtefact.mask.transformation.rotate);
			Assert.Equal(newTranslateX, writtenArtefact.mask.transformation.translate.x);
			Assert.Equal(newTranslateY, writtenArtefact.mask.transformation.translate.y);
			Assert.Equal(newArtefact.name, writtenArtefact.name);
			Assert.Equal(defaultStatusMessage, writtenArtefact.statusMessage);

			// Cleanup
			await DeleteArtefact(newEdition, writtenArtefact.id);

			// Arrange
			newName = null;

			newArtefact = new CreateArtefactDTO
			{
				polygon = new PolygonDTO
				{
					mask = newArtefactShape,
					transformation = new TransformationDTO
					{
						scale = newScale,
						rotate = newRotate,
						translate = new TranslateDTO
						{
							x = newTranslateX,
							y = newTranslateY
						}
					}
				},
				name = newName,
				masterImageId = masterImageId
			};

			// Act
			(response, writtenArtefact) = await HttpRequest.SendAsync<CreateArtefactDTO, ArtefactDTO>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/{controller}",
				newArtefact,
				await HttpRequest.GetJWTAsync(_client)
			);

			// Assert
			response.EnsureSuccessStatusCode();
			Assert.Equal(newEdition, writtenArtefact.editionId);
			Assert.Equal(newArtefact.polygon.mask, writtenArtefact.mask.mask);
			Assert.Equal(newScale, writtenArtefact.mask.transformation.scale);
			Assert.Equal(newRotate, writtenArtefact.mask.transformation.rotate);
			Assert.Equal(newTranslateX, writtenArtefact.mask.transformation.translate.x);
			Assert.Equal(newTranslateY, writtenArtefact.mask.transformation.translate.y);
			Assert.Equal("", writtenArtefact.name);

			// Cleanup
			await DeleteArtefact(newEdition, writtenArtefact.id);

			// Arrange
			newName = "CanCreateArtefacts.artefact ב";
			;

			newArtefact = new CreateArtefactDTO
			{
				polygon = new PolygonDTO
				{
					mask = null,
					transformation = new TransformationDTO
					{
						scale = newScale,
						rotate = newRotate,
						translate = new TranslateDTO
						{
							x = newTranslateX,
							y = newTranslateY
						}
					}
				},
				name = newName,
				masterImageId = masterImageId
			};

			// Act
			(response, writtenArtefact) = await HttpRequest.SendAsync<CreateArtefactDTO, ArtefactDTO>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/{controller}",
				newArtefact,
				await HttpRequest.GetJWTAsync(_client)
			);

			// Assert
			response.EnsureSuccessStatusCode();
			Assert.Equal(newEdition, writtenArtefact.editionId);
			Assert.Equal("", writtenArtefact.mask.mask);
			Assert.Equal(newScale, writtenArtefact.mask.transformation.scale);
			Assert.Equal(newRotate, writtenArtefact.mask.transformation.rotate);
			Assert.Equal(newTranslateX, writtenArtefact.mask.transformation.translate.x);
			Assert.Equal(newTranslateY, writtenArtefact.mask.transformation.translate.y);
			Assert.Equal(newArtefact.name, writtenArtefact.name);

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
			var (response, writtenArtefact) = await HttpRequest.SendAsync<string, string>(
				_client,
				HttpMethod.Delete,
				$"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
				null,
				await HttpRequest.GetJWTAsync(_client)
			);

			// Assert
			response.EnsureSuccessStatusCode();
			// Ensure successful nocontent status
			Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
			// Double check that it is really gone
			var (delResponse, _) = await HttpRequest.SendAsync<string, string>(
				_client,
				HttpMethod.Get,
				$"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
				null,
				await HttpRequest.GetJWTAsync(_client)
			);
			Assert.Equal(HttpStatusCode.NotFound, delResponse.StatusCode);

			await EditionHelpers.DeleteEdition(_client, newEdition, true);
		}

		[Fact]
		public async Task CanGetSuggestedTextFragmentForArtefact()
		{
			// Arrange
			const uint editionId = 894;
			const uint artefactId = 10058;
			var path = $"/{version}/editions/{editionId}/{controller}/{artefactId}/suggested-text-fragments";

			// Act
			var (tfResponse, tfData) = await HttpRequest.SendAsync<string, TextFragmentDataListDTO>(
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
			var (newScale, newRotate, newTranslateX, newTranslateY) = ArtefactPosition();
			var newName = "CanCreateArtefacts.artefact α";
			;
			var newArtefact = new CreateArtefactDTO
			{
				polygon = new PolygonDTO
				{
					mask = newArtefactShape,
					transformation = new TransformationDTO
					{
						scale = newScale,
						rotate = newRotate,
						translate = new TranslateDTO
						{
							x = newTranslateX,
							y = newTranslateY
						}
					}
				},
				name = newName,
				masterImageId = masterImageId
			};

			// Act
			var (response, _) = await HttpRequest.SendAsync<CreateArtefactDTO, ArtefactDTO>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/{controller}",
				newArtefact
			);

			// Assert
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			await EditionHelpers.DeleteEdition(_client, newEdition, true);
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
			var (response, _) = await HttpRequest.SendAsync<string, string>(
				_client,
				HttpMethod.Delete,
				$"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
				null
			);

			// Assert
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			await EditionHelpers.DeleteEdition(_client, newEdition, true);
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
			var newArtefactName = "CannotUpdateUnownedArtefacts.artefact 😈";

			// Act (update name)
			var (nameResponse, _) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(
				_client,
				HttpMethod.Put,
				$"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
				new UpdateArtefactDTO
				{
					polygon = new PolygonDTO
					{
						mask = null,
						transformation = new TransformationDTO
						{
							scale = null,
							rotate = null,
							translate = null
						}
					},
					name = newArtefactName
				}
			);

			// Assert (update name)
			Assert.Equal(HttpStatusCode.Unauthorized, nameResponse.StatusCode);

			await EditionHelpers.DeleteEdition(_client, newEdition, true);
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
			var newArtefactName = "CanUpdateArtefacts.artefact +%%$^";
			var (newScale, newRotate, newTranslateX, newTranslateY) = ArtefactPosition();
			const string newArtefactShape =
				"POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";
			const string statusMessage = "Fully examined";

			// Act (update name and set status)
			var (nameResponse, updatedNameArtefact) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(
				_client,
				HttpMethod.Put,
				$"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
				new UpdateArtefactDTO
				{
					polygon = new PolygonDTO
					{
						mask = null,
						transformation = new TransformationDTO
						{
							scale = null,
							rotate = null,
							translate = null
						}
					},
					name = newArtefactName,
					statusMessage = statusMessage
				},
				await HttpRequest.GetJWTAsync(_client)
			);

			// Assert (update name and set status)
			nameResponse.EnsureSuccessStatusCode();
			Assert.Equal(artefact.mask.transformation.scale, updatedNameArtefact.mask.transformation.scale);
			Assert.Equal(artefact.mask.transformation.rotate, updatedNameArtefact.mask.transformation.rotate);
			Assert.Null(updatedNameArtefact.mask.transformation.translate);
			Assert.NotEqual(artefact.name, updatedNameArtefact.name);
			Assert.Equal(newArtefactName, updatedNameArtefact.name);
			Assert.Equal(statusMessage, updatedNameArtefact.statusMessage);

			// Act (update position)
			var (positionResponse, updatedPositionArtefact) =
				await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(
					_client,
					HttpMethod.Put,
					$"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
					new UpdateArtefactDTO
					{
						polygon = new PolygonDTO
						{
							mask = null,
							transformation = new TransformationDTO
							{
								scale = newScale,
								rotate = newRotate,
								translate = new TranslateDTO
								{
									x = newTranslateX,
									y = newTranslateY
								}
							}
						},
						name = null
					},
					await HttpRequest.GetJWTAsync(_client)
				);

			// Assert (update position)
			positionResponse.EnsureSuccessStatusCode();
			Assert.NotEqual(artefact.mask.transformation.scale, updatedPositionArtefact.mask.transformation.scale);
			Assert.NotEqual(artefact.mask.transformation.rotate, updatedPositionArtefact.mask.transformation.rotate);
			Assert.NotNull(updatedPositionArtefact.mask.transformation.translate);
			Assert.Equal(newScale, updatedPositionArtefact.mask.transformation.scale);
			Assert.Equal(newRotate, updatedPositionArtefact.mask.transformation.rotate);
			Assert.Equal(newTranslateX, updatedPositionArtefact.mask.transformation.translate.x);
			Assert.Equal(newTranslateY, updatedPositionArtefact.mask.transformation.translate.y);
			Assert.Equal(newArtefactName, updatedPositionArtefact.name);

			// Act (update shape)
			var (shapeResponse, updatedShapeArtefact) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(
				_client,
				HttpMethod.Put,
				$"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
				new UpdateArtefactDTO
				{
					polygon = new PolygonDTO
					{
						mask = newArtefactShape,
						transformation = new TransformationDTO
						{
							scale = newScale,
							rotate = newRotate,
							translate = new TranslateDTO
							{
								x = newTranslateX,
								y = newTranslateY
							}
						}
					},
					name = null
				},
				await HttpRequest.GetJWTAsync(_client)
			);

			// Assert (update shape)
			shapeResponse.EnsureSuccessStatusCode();
			Assert.NotEqual(artefact.mask.mask, updatedShapeArtefact.mask.mask);
			Assert.Equal(newArtefactShape, updatedShapeArtefact.mask.mask);
			Assert.Equal(newScale, updatedShapeArtefact.mask.transformation.scale);
			Assert.Equal(newRotate, updatedShapeArtefact.mask.transformation.rotate);
			Assert.Equal(newTranslateX, updatedShapeArtefact.mask.transformation.translate.x);
			Assert.Equal(newTranslateY, updatedShapeArtefact.mask.transformation.translate.y);
			Assert.Equal(newArtefactName, updatedShapeArtefact.name);

			// Arrange (update all)
			var (otherScale, otherRotate, otherTranslateX, otherTranslateY) = ArtefactPosition();
			// Act (update all)
			var (allResponse, updatedAllArtefact) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(
				_client,
				HttpMethod.Put,
				$"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
				new UpdateArtefactDTO
				{
					polygon = new PolygonDTO
					{
						mask = artefact.mask.mask,
						transformation = new TransformationDTO
						{
							scale = otherScale,
							rotate = otherRotate,
							translate = new TranslateDTO
							{
								x = otherTranslateX,
								y = otherTranslateY
							}
						}
					},
					name = artefact.name
				},
				await HttpRequest.GetJWTAsync(_client)
			);

			// Assert (update all)
			allResponse.EnsureSuccessStatusCode();
			Assert.Equal(artefact.mask.mask, updatedAllArtefact.mask.mask);
			Assert.Equal(otherScale, updatedAllArtefact.mask.transformation.scale);
			Assert.Equal(otherRotate, updatedAllArtefact.mask.transformation.rotate);
			Assert.Equal(otherTranslateX, updatedAllArtefact.mask.transformation.translate.x);
			Assert.Equal(otherTranslateY, updatedAllArtefact.mask.transformation.translate.y);
			Assert.Equal(artefact.name, updatedAllArtefact.name);

			await EditionHelpers.DeleteEdition(_client, newEdition, true);
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
			var (nameResponse, _) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(
				_client,
				HttpMethod.Put,
				$"/{version}/editions/{newEdition}/{controller}/{artefact.id}",
				new UpdateArtefactDTO
				{
					polygon = new PolygonDTO
					{
						mask = newArtefactShape,
						transformation = new TransformationDTO
						{
							scale = null,
							rotate = null,
							translate = null
						}
					},
					name = null
				},
				await HttpRequest.GetJWTAsync(_client)
			);

			// Assert (update name)
			Assert.Equal(HttpStatusCode.BadRequest, nameResponse.StatusCode);

			await EditionHelpers.DeleteEdition(_client, newEdition, true);
		}
	}
}