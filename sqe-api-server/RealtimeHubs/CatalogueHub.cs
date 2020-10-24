/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-api-server` project. Changes made
 * there will automatically be incorporated here the next time the
 * `sqe-realtime-hub-builder` is run.
 */

using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.DatabaseAccess.Helpers;

namespace SQE.API.Server.RealtimeHubs
{
	public partial class MainHub
	{
		/// <summary>
		///  Get a listing of all text fragments to imaged object matches
		/// </summary>
		[AllowAnonymous]
		public async Task<CatalogueMatchListDTO> GetV1CatalogueAllMatches()

		{
			try
			{
				return await _catalogueService.GetAllMatches();
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Get a listing of all text fragments matches that correspond to an imaged object
		/// </summary>
		/// <param name="imagedObjectId">Id of imaged object to search for transcription matches</param>
		[AllowAnonymous]
		public async Task<CatalogueMatchListDTO>
				GetV1CatalogueImagedObjectsImagedObjectIdTextFragments(string imagedObjectId)

		{
			try
			{
				return await _catalogueService.GetTextFragmentsOfImagedObject(imagedObjectId);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Get a listing of all imaged objects that matches that correspond to a transcribed text fragment
		/// </summary>
		/// <param name="textFragmentId">Unique Id of the text fragment to search for imaged object matches</param>
		[AllowAnonymous]
		public async Task<CatalogueMatchListDTO>
				GetV1CatalogueTextFragmentsTextFragmentIdImagedObjects(uint textFragmentId)

		{
			try
			{
				return await _catalogueService.GetImagedObjectsOfTextFragment(textFragmentId);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Get a listing of all corresponding imaged objects and transcribed text fragment in a specified edition
		/// </summary>
		/// <param name="editionId">Unique Id of the edition to search for imaged objects to text fragment matches</param>
		[AllowAnonymous]
		public async Task<CatalogueMatchListDTO>
				GetV1CatalogueEditionsEditionIdImagedObjectTextFragmentMatches(uint editionId)

		{
			try
			{
				return await _catalogueService.GetTextFragmentsAndImagedObjectsOfEdition(editionId);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Get a listing of all corresponding imaged objects and transcribed text fragment in a specified manuscript
		/// </summary>
		/// <param name="manuscriptId">Unique Id of the manuscript to search for imaged objects to text fragment matches</param>
		[AllowAnonymous]
		public async Task<CatalogueMatchListDTO>
				GetV1CatalogueManuscriptsManuscriptIdImagedObjectTextFragmentMatches(
						uint manuscriptId)

		{
			try
			{
				return await _catalogueService.GetTextFragmentsAndImagedObjectsOfManuscript(
						manuscriptId);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Create a new matched pair for an imaged object and a text fragment along with the edition princeps information
		/// </summary>
		/// <param name="newMatch">The details of the new match</param>
		/// <returns></returns>
		[Authorize]
		public async Task PostV1Catalogue(CatalogueMatchInputDTO newMatch)

		{
			try
			{
				await _catalogueService.CreateTextFragmentImagedObjectMatch(
						await _userService.GetCurrentUserObjectAsync(null, true)
						, newMatch);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Confirm the correctness of an existing imaged object and text fragment match
		/// </summary>
		/// <param name="iaaEditionCatalogToTextFragmentId">The unique id of the match to confirm</param>
		/// <returns></returns>
		[Authorize]
		public async Task PostV1CatalogueConfirmMatchIaaEditionCatalogToTextFragmentId(
				uint iaaEditionCatalogToTextFragmentId)

		{
			try
			{
				await _catalogueService.ConfirmTextFragmentImagedObjectMatch(
						await _userService.GetCurrentUserObjectAsync(null, true)
						, iaaEditionCatalogToTextFragmentId
						, true);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Remove an existing imaged object and text fragment match, which is not correct
		/// </summary>
		/// <param name="iaaEditionCatalogToTextFragmentId">The unique id of the match to confirm</param>
		/// <returns></returns>
		[Authorize]
		public async Task DeleteV1CatalogueConfirmMatchIaaEditionCatalogToTextFragmentId(
				uint iaaEditionCatalogToTextFragmentId)

		{
			try
			{
				await _catalogueService.ConfirmTextFragmentImagedObjectMatch(
						await _userService.GetCurrentUserObjectAsync(null, true)
						, iaaEditionCatalogToTextFragmentId
						, false);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}
	}
}
