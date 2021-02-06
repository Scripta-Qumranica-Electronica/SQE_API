using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
	public partial class WebControllerTest
	{
		private async Task<(uint editionId, string objectId)> GetEditionImagesWithArtefact()
		{
			var editionId = EditionHelpers.GetEditionId();

			var req = new Get.V1_Editions_EditionId_ImagedObjects(
					editionId
					, new List<string> { "masks" });

			await req.SendAsync(_client, StartConnectionAsync);

			var (httpResponse, httpData, signalrData) = (req.HttpResponseMessage
														 , req.HttpResponseObject
														 , req.SignalrResponseObject);

			httpResponse.EnsureSuccessStatusCode();
			httpData.ShouldDeepEqual(signalrData);
			var imageWithMaskId = 0;

			foreach (var (io, index) in httpData.imagedObjects.Select((x, idx) => (x, idx)))
			{
				if (io.artefacts.Any())
				{
					imageWithMaskId = index;

					break;
				}
			}

			return (editionId, httpData.imagedObjects[imageWithMaskId].id);
		}

		/// <summary>
		///  Can recognize imaged object ids with url encoded values
		/// </summary>
		/// <returns></returns>
		[Fact]
		[Trait("Category", "Imaged Object")]
		public async Task CanDecodeImagedObjectIdWithUrlEncodedValue()
		{
			// Note: the dotnet HTTP Request system must automatically decode the escaped the URL's we submit.
			var id = "IAA-275/1-1";

			var textFragRequest = new Get.V1_ImagedObjects_ImagedObjectId_TextFragments(id);

			await textFragRequest.SendAsync(_client);

			var (response, msg) = (textFragRequest.HttpResponseMessage
								   , textFragRequest.HttpResponseObject);

			response.EnsureSuccessStatusCode();
			Assert.Single(msg.matches);
			Assert.Equal("4Q7", msg.matches.First().manuscriptName);

			Assert.Equal("frg. 1", msg.matches.First().textFragmentName);

			Assert.Equal((uint) 9423, msg.matches.First().textFragmentId);
		}

		/// <summary>
		///  This tests if an anonymous user can retrieve imaged object institutions
		/// </summary>
		/// <returns></returns>
		[Fact]
		[Trait("Category", "Imaged Object")]
		public async Task CanGetImagedObjectInstitutions()
		{
			// Act
			var request = new Get.V1_ImagedObjects_Institutions();

			await request.SendAsync(_client, StartConnectionAsync, requestRealtime: true);

			// Assert
			request.HttpResponseMessage.EnsureSuccessStatusCode();
			Assert.NotEmpty(request.HttpResponseObject.institutions);
		}

		/// <summary>
		///  This tests if an anonymous user can retrieve an imaged object belonging to an edition
		/// </summary>
		/// <returns></returns>
		[Theory]
		[Trait("Category", "Imaged Object")]
		[InlineData(true, false)]
		[InlineData(true, true)]
		[InlineData(false, false)]
		public async Task CanGetImagedObjectOfEdition(bool optionalArtefacts, bool optionalMasks)
		{
			// Arrange
			var (editionId, objectId) = await GetEditionImagesWithArtefact();
			var optional = new List<string>();

			if (optionalArtefacts)
				optional.Add("artefacts");

			if (optionalMasks && optionalArtefacts)
				optional.Add("masks");

			// Act
			var request =
					new Get.V1_Editions_EditionId_ImagedObjects_ImagedObjectId(
							editionId
							, objectId
							, optional);

			await request.SendAsync(_client, StartConnectionAsync, requestRealtime: true);

			// Assert
			request.HttpResponseMessage.EnsureSuccessStatusCode();
			Assert.NotNull(request.HttpResponseObject.recto.images.FirstOrDefault().imageManifest);

			if (!optionalArtefacts)
				Assert.Null(request.HttpResponseObject.artefacts);

			if (optionalArtefacts)
				Assert.NotNull(request.HttpResponseObject.artefacts);

			if (optionalMasks)
			{
				Assert.NotNull(request.HttpResponseObject.artefacts);
				var foundArtefactWithMask = false;

				foreach (var art in request.HttpResponseObject.artefacts)
				{
					if (!string.IsNullOrEmpty(art.mask))
					{
						foundArtefactWithMask = true;

						break;
					}
				}

				Assert.True(foundArtefactWithMask);
			}
		}

		/// <summary>
		///  This tests if an anonymous user can retrieve imaged objects
		/// </summary>
		/// <returns></returns>
		[Theory]
		[Trait("Category", "Imaged Object")]
		[InlineData(true, false)]
		[InlineData(true, true)]
		[InlineData(false, false)]
		public async Task CanGetImagedObjectsOfEdition(bool optionalArtefacts, bool optionalMasks)
		{
			// Arrange
			var editionId = EditionHelpers.GetEditionId();
			var optional = new List<string>();

			if (optionalArtefacts)
				optional.Add("artefacts");

			if (optionalMasks && optionalArtefacts)
				optional.Add("masks");

			// Act
			var request = new Get.V1_Editions_EditionId_ImagedObjects(editionId, optional);

			await request.SendAsync(_client, StartConnectionAsync, requestRealtime: true);

			// Assert
			request.HttpResponseMessage.EnsureSuccessStatusCode();
			Assert.NotEmpty(request.HttpResponseObject.imagedObjects);

			Assert.NotNull(
					request.HttpResponseObject.imagedObjects.FirstOrDefault()
						   .recto.images.FirstOrDefault()
						   .imageManifest);

			if (!optionalArtefacts)
			{
				foreach (var io in request.HttpResponseObject.imagedObjects)
					Assert.Null(io.artefacts);
			}

			if (optionalArtefacts)
			{
				var foundArtefact = false;
				var foundArtefactWithMask = false;

				foreach (var io in request.HttpResponseObject.imagedObjects.Where(
						io => io.artefacts.Any()))
				{
					foundArtefact = true;

					if (!optionalMasks)
						break;

					foreach (var art in io.artefacts)
					{
						if (!string.IsNullOrEmpty(art.mask))
						{
							foundArtefactWithMask = true;

							break;
						}
					}
				}

				Assert.True(foundArtefact);

				if (optionalMasks)
					Assert.True(foundArtefactWithMask);
				else
					Assert.False(foundArtefactWithMask);
			}
		}

		[Fact]
		[Trait("Category", "Imaged Object")]
		public async Task CanGetInstitutionalImages()
		{
			await ImagedObjectHelpers.GetInstitutionImagedObjects(
					"IAA"
					, _client
					, StartConnectionAsync);
		}

		[Fact]
		[Trait("Category", "Imaged Object")]
		public async Task CanGetSpecifiedImagedObject()
		{
			// Arrange
			var availableImagedObjects =
					await ImagedObjectHelpers.GetInstitutionImagedObjects(
							"IAA"
							, _client
							, StartConnectionAsync);

			// Act
			var imagedObject = await GetSpecificImagedObjectAsync(
					availableImagedObjects.institutionalImages.First().id
					, _client
					, StartConnectionAsync);

			// Assert
			Assert.NotEmpty(imagedObject.images);
		}

		[Fact]
		[Trait("Category", "Imaged Object")]
		public async Task CanGetImagedObjectTextFragment()
		{
			// Note that "IAA-1039-1" had text fragment matches at the time this test was written.
			// Make sure to check the database for errors if this test fails.
			var textFragmentMatches = await GetImagedObjectTextFragmentMatchesAsync(
					"IAA-1039-1"
					, _client
					, StartConnectionAsync);

			Assert.NotNull(textFragmentMatches);
			Assert.NotEmpty(textFragmentMatches.matches);
			Assert.NotNull(textFragmentMatches.matches.First().manuscriptName);

			Assert.NotNull(textFragmentMatches.matches.First().textFragmentName);

			Assert.NotEqual<uint>(0, textFragmentMatches.matches.First().editionId);

			Assert.NotEqual<uint>(0, textFragmentMatches.matches.First().textFragmentId);
		}

		[Theory]
		[InlineData("IAA-1094-2", false, true)]
		[InlineData("IAA-1094-2", true, true)]
		[InlineData("IAA-1094-1", false, false)]
		[InlineData("IAA-1094-1", true, false)]
		[Trait("Category", "Imaged Object")]
		public async Task CanAddImagedObjectToEdition(
				string imagedObjectId
				, bool realtime
				, bool shouldSucceed)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var newEdition = await editionCreator.CreateEdition();

				// Act
				var newEditionImage = await _createEditionImagedObject(
						newEdition
						, imagedObjectId
						, _client
						, StartConnectionAsync
						, realtime
						, shouldSucceed);

				// Assert
				if (!shouldSucceed)
				{
					Assert.Null(newEditionImage);

					return;
				}

				Assert.NotNull(newEditionImage);
				Assert.Equal(imagedObjectId, newEditionImage.id);
				Assert.NotEmpty(newEditionImage.recto.images);
				Assert.NotEmpty(newEditionImage.verso.images);
			}
		}

		[Theory]
		[InlineData("IAA-1094-2", false, true)]
		[InlineData("IAA-1094-2", true, true)]
		[InlineData("IAA-1094-2", false, false)]
		[InlineData("IAA-1094-2", true, false)]
		[Trait("Category", "Imaged Object")]
		public async Task CanDeleteImagedObjectFromEdition(
				string imagedObjectId
				, bool realtime
				, bool shouldSucceed)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var newEdition = await editionCreator.CreateEdition();

				if (shouldSucceed)
				{
					await _createEditionImagedObject(
							newEdition
							, imagedObjectId
							, _client
							, StartConnectionAsync
							, realtime
							, shouldSucceed);
				}

				// Act
				var request =
						new Delete.V1_Editions_EditionId_ImagedObjects_ImagedObjectId(
								newEdition
								, imagedObjectId);

				await request.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime
						, shouldSucceed: shouldSucceed);

				// Assert
				var imagedObjectRequest = new Get.V1_Editions_EditionId_ImagedObjects(newEdition);

				await imagedObjectRequest.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime);

				var imageObjects = realtime
						? imagedObjectRequest.SignalrResponseObject
						: imagedObjectRequest.HttpResponseObject;

				Assert.NotEmpty(imageObjects.imagedObjects);
				Assert.DoesNotContain(imageObjects.imagedObjects, x => x.id == imagedObjectId);
			}
		}

		public static async Task<SimpleImageListDTO> GetSpecificImagedObjectAsync(
				string                              imagedObjectId
				, HttpClient                        client
				, Func<string, Task<HubConnection>> signalr)
		{
			// Act
			var request = new Get.V1_ImagedObjects_ImagedObjectId(imagedObjectId);

			await request.SendAsync(client, signalr);

			// Assert
			request.HttpResponseObject.ShouldDeepEqual(request.SignalrResponseObject);

			Assert.NotEmpty(request.HttpResponseObject.images);

			return request.HttpResponseObject;
		}

		public static async Task<ImagedObjectTextFragmentMatchListDTO>
				GetImagedObjectTextFragmentMatchesAsync(
						string                              imagedObjectId
						, HttpClient                        client
						, Func<string, Task<HubConnection>> signalr)
		{
			// Act
			var request = new Get.V1_ImagedObjects_ImagedObjectId_TextFragments(imagedObjectId);

			await request.SendAsync(client, signalr);

			// Assert
			request.HttpResponseObject.ShouldDeepEqual(request.SignalrResponseObject);

			return request.HttpResponseObject;
		}

		private static async Task<ImagedObjectDTO> _createEditionImagedObject(
				uint                                editionId
				, string                            imagedObjectId
				, HttpClient                        client
				, Func<string, Task<HubConnection>> signalr
				, bool                              realtime
				, bool                              shouldSucceed)
		{
			var request =
					new Post.V1_Editions_EditionId_ImagedObjects_ImagedObjectId(
							editionId
							, imagedObjectId);

			await request.SendAsync(
					realtime
							? null
							: client
					, signalr
					, true
					, requestRealtime: realtime
					, shouldSucceed: shouldSucceed
					, listeningFor: request.AvailableListeners.CreatedImagedObject);

			if (!shouldSucceed)
				return null;

			var response = realtime
					? request.SignalrResponseObject
					: request.HttpResponseObject;

			response.ShouldDeepEqual(request.CreatedImagedObject);

			return response;
		}
	}
}
