using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
	public class RoiTests : WebControllerTest
	{
		public RoiTests(WebApplicationFactory<Startup> factory) : base(factory)
		{
			_db = new DatabaseQuery();
		}

		private readonly DatabaseQuery _db;

		private const string version = "v1";
		private const string controller = "rois"; 

		private async Task DeleteRoi(uint editionId, uint roiId)
		{
			var (response, _) = await Request.SendHttpRequestAsync<string, string>(
				_client,
				HttpMethod.Delete,
				$"/{version}/editions/{editionId}/{controller}/{roiId}",
				null,
				await Request.GetJwtViaHttpAsync(_client)
			);
			response.EnsureSuccessStatusCode();
		}

		[Fact]
		private async Task CreateListOfRois()
		{
			//Arrange 
			const uint editionId = 894;
			const uint artefactId = 506;
			var bearerToken = await Request.GetJwtViaHttpAsync(_client);

			var newEdition = await EditionHelpers.CreateCopyOfEdition
				(_client,
				editionId: editionId,
				userAuthDetails: Request.DefaultUsers.User1);

			var roiA = new SetInterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 1410,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 120,
					y = 120
				},
				exceptional = false,
				valuesSet = true
			};

			var roiB = new SetInterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 1410,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 260,
					y = 260
				},
				exceptional = true,
				valuesSet = false
			};

			var roisList = new SetInterpretationRoiDTOList();
			roisList.rois = new List<SetInterpretationRoiDTO>();
			roisList.rois.Add(roiA);
			roisList.rois.Add(roiB);

			var (response, roiData) = await Request.SendHttpRequestAsync<SetInterpretationRoiDTOList, InterpretationRoiDTOList>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/rois/batch",
				roisList,
				bearerToken
			);

			//Assert
			response.EnsureSuccessStatusCode();
			Assert.True(roiData.rois.Count == roisList.rois.Count);

			//Clean up
			foreach (var roi in roiData.rois)
			{
				await DeleteRoi(newEdition, roi.interpretationRoiId);
			}
			await EditionHelpers.DeleteEdition(_client, newEdition);
		}

		[Fact]
		private async Task CreateRoiForEdition()
		{
			//var artefact = (await GetEditionArtefacts()).artefacts[0];

			//Arrange 
			const uint editionId = 894;
			const uint artefactId = 10018;
			var bearerToken = await Request.GetJwtViaHttpAsync(_client);

			var newEdition = await EditionHelpers.CreateCopyOfEdition
				(_client,
				editionId: editionId,
				userAuthDetails: Request.DefaultUsers.User1);

			var roi = new SetInterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 1410,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 200,
					y = 300
				},
				exceptional = false,
				valuesSet = true
			};

			//Act
			var (response, roiRes) = await Request.SendHttpRequestAsync<SetInterpretationRoiDTO, InterpretationRoiDTO>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/rois",
				roi,
				bearerToken
			);

			// Assert
			response.EnsureSuccessStatusCode();

			//Get roi information
			var (getResponse, roiData) = await Request.SendHttpRequestAsync<string, InterpretationRoiDTO>(
				_client,
				HttpMethod.Get,
				$"/{version}/editions/{newEdition}/rois/{roiRes.interpretationRoiId}",
				null,
				bearerToken
			);

			// Assert
			getResponse.EnsureSuccessStatusCode();
			Assert.Equal(roiData.interpretationRoiId, roiRes.interpretationRoiId);
			Assert.Equal(roiData.artefactId, artefactId);
			Assert.Equal(roiData.editorId, newEdition);

			//CleanUp
			await DeleteRoi(roiRes.editorId, roiRes.interpretationRoiId);
			await EditionHelpers.DeleteEdition(_client, newEdition);

		}

		[Fact]
		private async Task GetArtefactRois()
		{
			//Arrange 
			const uint editionId = 894;
			const uint artefactId = 10018;
			var bearerToken = await Request.GetJwtViaHttpAsync(_client);

			var newEdition = await EditionHelpers.CreateCopyOfEdition
				(_client,
				editionId: editionId,
				userAuthDetails: Request.DefaultUsers.User1);

			var roiA = new SetInterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 330,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 120,
					y = 120
				},
				exceptional = false,
				valuesSet = true
			};

			var roiB = new SetInterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 335,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 260,
					y = 260
				},
				exceptional = true,
				valuesSet = false
			};

			var roisList = new SetInterpretationRoiDTOList();
			roisList.rois = new List<SetInterpretationRoiDTO>();
			roisList.rois.Add(roiA);
			roisList.rois.Add(roiB);

			var (response, roiData) = await Request.SendHttpRequestAsync<SetInterpretationRoiDTOList, InterpretationRoiDTOList>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/rois/batch",
				roisList,
				bearerToken
			);

			//Assert
			response.EnsureSuccessStatusCode();


			var (artefactResponse, artefacts) = await Request.SendHttpRequestAsync<string, InterpretationRoiDTOList>(
				_client,
				HttpMethod.Get,
				$"/{version}/editions/{newEdition}/artefacts/{artefactId}/rois",
				null,
				bearerToken
			);

			//Assert
			artefactResponse.EnsureSuccessStatusCode();
			Assert.True(artefacts.rois.Count == roisList.rois.Count);

			//Clean up
			foreach (var roi in roiData.rois)
			{
				await DeleteRoi(newEdition, roi.interpretationRoiId);
			}
			await EditionHelpers.DeleteEdition(_client, newEdition);

		}

		[Fact]
		private async Task UpdateListOfRois()
		{
			//Arrange 
			const uint editionId = 100;
			const uint artefactId = 18;
			var bearerToken = await Request.GetJwtViaHttpAsync(_client);

			var newEdition = await EditionHelpers.CreateCopyOfEdition
				(_client,
				editionId: editionId,
				userAuthDetails: Request.DefaultUsers.User1);

			//create first roi, just to be sure that we can update and delete roi in batch (need interpretationRoiId)
			var roi = new SetInterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 1410,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 200,
					y = 300
				},
				exceptional = false,
				valuesSet = true
			};

			//Act
			var (response, roiRes) = await Request.SendHttpRequestAsync<SetInterpretationRoiDTO, InterpretationRoiDTO>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/rois",
				roi,
				bearerToken
			);

			//create first roi, just to be sure that we can update and delete roi in batch (need interpretationRoiId)
			var roiToDelete = new SetInterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 1410,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 200,
					y = 300
				},
				exceptional = false,
				valuesSet = true
			};

			//Act
			var (responseDel, roiDelRes) = await Request.SendHttpRequestAsync<SetInterpretationRoiDTO, InterpretationRoiDTO>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/rois",
				roi,
				bearerToken
			);


			var roiA = new InterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 251,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 120,
					y = 120
				},
				exceptional = false,
				valuesSet = true,
				editorId = 551,
				interpretationRoiId =888
			};

			var roiB = new InterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 430,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 60,
					y = 60
				},
				exceptional = true,
				valuesSet = false,
				interpretationRoiId = 555,
				editorId = 551
			};

			var batchRoisList = new BatchEditRoiDTO();

			batchRoisList.createRois = new List<InterpretationRoiDTO>();
			batchRoisList.createRois.Add(roiA);
			batchRoisList.createRois.Add(roiB);

			batchRoisList.updateRois = new List<UpdatedInterpretationRoiDTO>();

			var roiC = new UpdatedInterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 252,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 560,
					y = 460
				},
				exceptional = true,
				valuesSet = false,
				interpretationRoiId = roiRes.interpretationRoiId,
				editorId = 551,
				oldInterpretationRoiId = roiRes.interpretationRoiId
			};
			batchRoisList.updateRois.Add(roiC);
		
			batchRoisList.deleteRois = new List<uint>();
			batchRoisList.deleteRois.Add(roiDelRes.interpretationRoiId);

			//update batch
			var (responseBatch, dataBatch) = await Request.SendHttpRequestAsync<BatchEditRoiDTO, BatchEditRoiResponseDTO>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/rois/batch-edit",
				batchRoisList,
				bearerToken
			);

			responseBatch.EnsureSuccessStatusCode();

		}

		[Fact]
		private async Task UpdateRoi()
		{
			//Arrange 
			const uint editionId = 894;
			const uint artefactId = 10018;
			var bearerToken = await Request.GetJwtViaHttpAsync(_client);

			var newEdition = await EditionHelpers.CreateCopyOfEdition
				(_client,
				editionId: editionId,
				userAuthDetails: Request.DefaultUsers.User1);

			var roi = new SetInterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 1410,
				shape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 100,
					y = 100
				},
				exceptional = false,
				valuesSet = true
			};

			//Act
			var (response, roiRes) = await Request.SendHttpRequestAsync<SetInterpretationRoiDTO, InterpretationRoiDTO>(
				_client,
				HttpMethod.Post,
				$"/{version}/editions/{newEdition}/rois",
				roi,
				bearerToken
			);

			// Assert
			response.EnsureSuccessStatusCode();

			var updatedRoi = new SetInterpretationRoiDTO
			{
				artefactId = artefactId,
				signInterpretationId = 1410,
				shape = "POLYGON((10 10,10 1200,1200 1200,10 1200,10 10),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))",
				translate = new TranslateDTO
				{
					x = 230,
					y = 230
				},
				exceptional = false,
				valuesSet = true
			};

			//Update Roi
			var (updateResponse, updatedRoiRes) = await Request.SendHttpRequestAsync<SetInterpretationRoiDTO, UpdatedInterpretationRoiDTO>(
				_client,
				HttpMethod.Put,
				$"/{version}/editions/{newEdition}/rois/{roiRes.interpretationRoiId}",
				updatedRoi,
				bearerToken
			);

			// Assert
			response.EnsureSuccessStatusCode();
			Assert.Equal(roiRes.interpretationRoiId, updatedRoiRes.oldInterpretationRoiId);
			Assert.Equal(updatedRoiRes.artefactId, artefactId);
			Assert.Equal(updatedRoiRes.editorId, newEdition);

			//Clean up
			await DeleteRoi(newEdition, updatedRoiRes.interpretationRoiId);
			await EditionHelpers.DeleteEdition(_client, newEdition);
		}



	}
}
