using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using Xunit;

// TODO: It would be nice to be able to generate random polygons for these testing purposes.
namespace SQE.ApiTest
{
	/// <summary>
	///  This test suite tests all the current endpoints in the RoiController
	/// </summary>
	public partial class WebControllerTest
	{
		[Theory]
		[InlineData(true, "4Q51", true)]
		[InlineData(false, "4Q51", true)]
		[InlineData(true, "4Q51", false)]
		[InlineData(false, "4Q51", false)]
		[Trait("Category", "Search")]
		public async Task CanSearchForEdition(bool realtime, string edition, bool exact)
		{
			// Act
			var (response, _) = await _performSearch(realtime, edition, exact);

			// Assert
			Assert.NotEmpty(response.editions.editions);
			Assert.All(response.editions.editions, x => x.name.Contains(edition));
			Assert.Empty(response.artefacts.artefacts);
			Assert.Empty(response.images.imagedObjects);
			Assert.Empty(response.textFragments.textFragments);

			if (exact)
				Assert.Single(response.editions.editions);
		}

		[Theory]
		[InlineData(true, "IAA-1094-2", true)]
		[InlineData(false, "IAA-1094-2", true)]
		[InlineData(true, "IAA-1094", false)]
		[InlineData(false, "IAA-1094", false)]
		[Trait("Category", "Search")]
		public async Task CanSearchForImagedObject(bool realtime, string imagedObject, bool exact)
		{
			// Act
			var (response, _) = await _performSearch(
					realtime
					, imageDesignation: imagedObject
					, exactImageDesignation: exact);

			// Assert
			Assert.NotEmpty(response.images.imagedObjects);
			Assert.All(response.images.imagedObjects, x => x.id.Contains(imagedObject));
			Assert.NotEmpty(response.images.imagedObjects.First().editionIds);
			Assert.Empty(response.artefacts.artefacts);
			Assert.Empty(response.editions.editions);
			Assert.Empty(response.textFragments.textFragments);

			if (exact)
				Assert.Single(response.images.imagedObjects);
		}

		[Theory]
		[InlineData(
				true
				, "4Q51"
				, "col. 1a"
				, true)]
		[InlineData(
				false
				, "4Q51"
				, "col. 1a"
				, true)]
		[InlineData(
				true
				, "4Q51"
				, "col"
				, false)]
		[InlineData(
				false
				, "4Q51"
				, "col"
				, false)]
		[Trait("Category", "Search")]
		public async Task CanSearchForTextFragment(
				bool     realtime
				, string edition
				, string textFragment
				, bool   exact)
		{
			// Act
			var (response, _) = await _performSearch(
					realtime
					, edition
					, exact
					, textReference: new List<string>
					{
							textFragment,
					}
					, exactTextReference: exact);

			// Assert
			Assert.NotEmpty(response.editions.editions);
			Assert.NotEmpty(response.textFragments.textFragments);
			Assert.All(response.editions.editions, x => x.name.Contains(edition));
			Assert.All(response.textFragments.textFragments, x => x.name.Contains(textFragment));
			Assert.All(response.textFragments.textFragments, x => x.editionName.Contains(edition));
			Assert.Empty(response.artefacts.artefacts);
			Assert.Empty(response.images.imagedObjects);

			if (exact)
			{
				Assert.Single(response.editions.editions);
				Assert.Single(response.textFragments.textFragments);
			}
		}

		[Theory]
		[InlineData(
				true
				, "4Q51"
				, "Frg. 68"
				, true)]
		[InlineData(
				false
				, "4Q51"
				, "Frg. 68"
				, true)]
		[InlineData(
				true
				, "4Q51"
				, "Col."
				, false)]
		[InlineData(
				false
				, "4Q51"
				, "Col."
				, false)]
		[Trait("Category", "Search")]
		public async Task CanSearchForArtefact(
				bool     realtime
				, string edition
				, string artefact
				, bool   exact)
		{
			// Act
			var (response, _) = await _performSearch(
					realtime
					, edition
					, exact
					, artefactDesignation: new List<string>
					{
							artefact,
					}
					, exactArtefactDesignation: exact);

			// Assert
			Assert.NotEmpty(response.editions.editions);
			Assert.NotEmpty(response.artefacts.artefacts);
			Assert.All(response.editions.editions, x => x.name.Contains(edition));
			Assert.All(response.artefacts.artefacts, x => x.name.Contains(artefact));

			Assert.All(
					response.artefacts.artefacts
					, x => response.editions.editions.Select(x => x.id).Contains(x.editionId));

			Assert.Empty(response.textFragments.textFragments);
			Assert.Empty(response.images.imagedObjects);

			if (exact)
			{
				Assert.Single(response.editions.editions);
				Assert.Equal(2, response.artefacts.artefacts.Count);
			}
		}

		[Theory]
		[InlineData(
				true
				, "4Q51"
				, "IAA-1094-2"
				, "col. 1a"
				, "Frg. 77"
				, true)]
		[InlineData(
				false
				, "4Q51"
				, "IAA-1094-2"
				, "col. 1a"
				, "Frg. 77"
				, true)]
		[InlineData(
				true
				, "4Q51"
				, "IAA-1094"
				, "col."
				, "Frg."
				, false)]
		[InlineData(
				false
				, "4Q51"
				, "IAA-1094"
				, "col."
				, "Frg."
				, false)]
		[Trait("Category", "Search")]
		public async Task CanSearchMultipleEntities(
				bool     realtime
				, string edition
				, string imagedObject
				, string textFragment
				, string artefact
				, bool   exact)
		{
			// Act
			var (response, _) = await _performSearch(
					realtime
					, edition
					, exact
					, imagedObject
					, exact
					, new List<string> { artefact }
					, exact
					, new List<string> { textFragment }
					, exact);

			// Assert
			Assert.NotEmpty(response.editions.editions);
			Assert.NotEmpty(response.artefacts.artefacts);
			Assert.NotEmpty(response.textFragments.textFragments);
			Assert.NotEmpty(response.images.imagedObjects);
			Assert.All(response.editions.editions, x => x.name.Contains(edition));
			Assert.All(response.textFragments.textFragments, x => x.name.Contains(textFragment));
			Assert.All(response.textFragments.textFragments, x => x.editionName.Contains(edition));
			Assert.All(response.artefacts.artefacts, x => x.name.Contains(artefact));

			Assert.All(
					response.artefacts.artefacts
					, x => response.editions.editions.Select(x => x.id).Contains(x.editionId));

			Assert.All(response.images.imagedObjects, x => x.id.Contains(imagedObject));

			if (exact)
			{
				Assert.Single(response.editions.editions);
				Assert.Single(response.textFragments.textFragments);
				Assert.Equal(2, response.artefacts.artefacts.Count);
				Assert.Single(response.images.imagedObjects);
			}
		}

		private async Task<(DetailedSearchResponseDTO, HttpResponseMessage)> _performSearch(
				bool           realtime
				, string       textDesignation          = null
				, bool         exactTextDesignation     = false
				, string       imageDesignation         = null
				, bool         exactImageDesignation    = false
				, List<string> artefactDesignation      = null
				, bool         exactArtefactDesignation = false
				, List<string> textReference            = null
				, bool         exactTextReference       = false)
		{
			artefactDesignation ??= new List<string>();
			textReference ??= new List<string>();

			var criteria = new DetailedSearchRequestDTO
			{
					artefactDesignation = artefactDesignation
					, exactArtefactDesignation = exactArtefactDesignation
					, exactImageDesignation = exactImageDesignation
					, exactTextDesignation = exactTextDesignation
					, imageDesignation = imageDesignation
					, textDesignation = textDesignation
					, textReference = textReference
					, exactTextReference = exactTextReference
					,
			};

			var search = new Post.V1_Search(criteria);

			// Act
			await search.SendAsync(
					realtime
							? null
							: _client
					, StartConnectionAsync
					, requestRealtime: realtime);

			return realtime
					? (search.SignalrResponseObject, null)
					: (search.HttpResponseObject, search.HttpResponseMessage);
		}
	}
}
