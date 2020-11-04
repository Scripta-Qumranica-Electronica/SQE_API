/*
 * This file is automatically generated by the GenerateTestRequestObjects
 * project in the Utilities folder. Do not edit this file directly as
 * its contents may be overwritten at any point.
 *
 * Should a class here need to be altered for any reason, you should look
 * first to the auto generation program for possible updating to include
 * the needed special case. Otherwise, it is possible to create your own
 * manually written ApiRequest object, though this is generally discouraged.
 */

using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
	public static partial class Delete
	{
		public class V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId :
				RequestObject<EmptyInput, EmptyOutput>
		{
			private readonly uint _iaaEditionCatalogToTextFragmentId;

			/// <summary>
			///  Remove an existing imaged object and text fragment match, which is not correct
			/// </summary>
			/// <param name="iaaEditionCatalogToTextFragmentId">The unique id of the match to confirm</param>
			/// <returns></returns>
			public V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId(
					uint iaaEditionCatalogToTextFragmentId) => _iaaEditionCatalogToTextFragmentId =
					iaaEditionCatalogToTextFragmentId;

			protected override string HttpPath() => RequestPath.Replace(
					"/iaa-edition-catalog-to-text-fragment-id"
					, $"/{HttpUtility.UrlEncode(_iaaEditionCatalogToTextFragmentId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _iaaEditionCatalogToTextFragmentId);
			}
		}
	}

	public static partial class Get
	{
		public class V1_Catalogue_AllMatches : RequestObject<EmptyInput, CatalogueMatchListDTO>
		{
			protected override string HttpPath() => RequestPath;

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(SignalrRequestString());
			}
		}

		public class V1_Catalogue_ImagedObjects_ImagedObjectId_TextFragments :
				RequestObject<EmptyInput, CatalogueMatchListDTO>
		{
			private readonly string _imagedObjectId;

			/// <summary>
			///  Get a listing of all text fragments matches that correspond to an imaged object
			/// </summary>
			/// <param name="imagedObjectId">Id of imaged object to search for transcription matches</param>
			public V1_Catalogue_ImagedObjects_ImagedObjectId_TextFragments(string imagedObjectId)
				=> _imagedObjectId = imagedObjectId;

			protected override string HttpPath() => RequestPath.Replace(
					"/imaged-object-id"
					, $"/{HttpUtility.UrlEncode(_imagedObjectId)}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _imagedObjectId);
			}
		}

		public class V1_Catalogue_TextFragments_TextFragmentId_ImagedObjects :
				RequestObject<EmptyInput, CatalogueMatchListDTO>
		{
			private readonly uint _textFragmentId;

			/// <summary>
			///  Get a listing of all imaged objects that matches that correspond to a transcribed text fragment
			/// </summary>
			/// <param name="textFragmentId">Unique Id of the text fragment to search for imaged object matches</param>
			public V1_Catalogue_TextFragments_TextFragmentId_ImagedObjects(uint textFragmentId)
				=> _textFragmentId = textFragmentId;

			protected override string HttpPath() => RequestPath.Replace(
					"/text-fragment-id"
					, $"/{HttpUtility.UrlEncode(_textFragmentId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _textFragmentId);
			}
		}

		public class V1_Catalogue_Editions_EditionId_ImagedObjectTextFragmentMatches :
				RequestObject<EmptyInput, CatalogueMatchListDTO>
		{
			private readonly uint _editionId;

			/// <summary>
			///  Get a listing of all corresponding imaged objects and transcribed text fragment in a specified edition
			/// </summary>
			/// <param name="editionId">Unique Id of the edition to search for imaged objects to text fragment matches</param>
			public V1_Catalogue_Editions_EditionId_ImagedObjectTextFragmentMatches(uint editionId)
				=> _editionId = editionId;

			protected override string HttpPath() => RequestPath.Replace(
					"/edition-id"
					, $"/{HttpUtility.UrlEncode(_editionId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId);
			}

			public override uint? GetEditionId() => _editionId;
		}

		public class V1_Catalogue_Manuscripts_ManuscriptId_ImagedObjectTextFragmentMatches :
				RequestObject<EmptyInput, CatalogueMatchListDTO>
		{
			private readonly uint _manuscriptId;

			/// <summary>
			///  Get a listing of all corresponding imaged objects and transcribed text fragment in a specified manuscript
			/// </summary>
			/// <param name="manuscriptId">Unique Id of the manuscript to search for imaged objects to text fragment matches</param>
			public V1_Catalogue_Manuscripts_ManuscriptId_ImagedObjectTextFragmentMatches(
					uint manuscriptId) => _manuscriptId = manuscriptId;

			protected override string HttpPath() => RequestPath.Replace(
					"/manuscript-id"
					, $"/{HttpUtility.UrlEncode(_manuscriptId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _manuscriptId);
			}
		}
	}

	public static partial class Post
	{
		public class V1_Catalogue : RequestObject<CatalogueMatchInputDTO, EmptyOutput>
		{
			private readonly CatalogueMatchInputDTO _payload;

			/// <summary>
			///  Create a new matched pair for an imaged object and a text fragment along with the edition princeps information
			/// </summary>
			/// <param name="newMatch">The details of the new match</param>
			/// <returns></returns>
			public V1_Catalogue(CatalogueMatchInputDTO payload) : base(payload)
				=> _payload = payload;

			protected override string HttpPath() => RequestPath;

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
			}
		}

		public class V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId :
				RequestObject<EmptyInput, EmptyOutput>
		{
			private readonly uint _iaaEditionCatalogToTextFragmentId;

			/// <summary>
			///  Confirm the correctness of an existing imaged object and text fragment match
			/// </summary>
			/// <param name="iaaEditionCatalogToTextFragmentId">The unique id of the match to confirm</param>
			/// <returns></returns>
			public V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId(
					uint iaaEditionCatalogToTextFragmentId) => _iaaEditionCatalogToTextFragmentId =
					iaaEditionCatalogToTextFragmentId;

			protected override string HttpPath() => RequestPath.Replace(
					"/iaa-edition-catalog-to-text-fragment-id"
					, $"/{HttpUtility.UrlEncode(_iaaEditionCatalogToTextFragmentId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _iaaEditionCatalogToTextFragmentId);
			}
		}
	}
}
