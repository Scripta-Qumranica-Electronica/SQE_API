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
	public static partial class Get
	{
		public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_WordVariants :
				RequestObject<EmptyInput, QwbWordVariantListDTO>
		{
			private readonly uint _editionId;
			private readonly uint _signInterpretationId;

			/// <summary>
			///  Search QWB (via proxy) for any variant readings for the word that contains the submitted sign
			///  interpretation id.
			/// </summary>
			/// <param name="editionId">Edition in which the sign interpretation id is found</param>
			/// <param name="signInterpretationId">Id of the sign interpretation to search</param>
			/// <returns></returns>
			public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_WordVariants(
					uint   editionId
					, uint signInterpretationId)

			{
				_editionId = editionId;
				_signInterpretationId = signInterpretationId;
			}

			protected override string HttpPath() => RequestPath
													.Replace(
															"/edition-id"
															, $"/{HttpUtility.UrlEncode(_editionId.ToString())}")
													.Replace(
															"/sign-interpretation-id"
															, $"/{HttpUtility.UrlEncode(_signInterpretationId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _editionId
							   , _signInterpretationId);
			}

			public override uint? GetEditionId() => _editionId;
		}

		public class V1_QwbProxy_Words_QwbWordId_WordVariants :
				RequestObject<EmptyInput, QwbWordVariantListDTO>
		{
			private readonly uint _qwbWordId;

			/// <summary>
			///  Search QWB (via proxy) for any variant readings for the word that contains the submitted
			///  QWB word id.
			/// </summary>
			/// <param name="qwbWordId">QWB word Id</param>
			/// <returns></returns>
			public V1_QwbProxy_Words_QwbWordId_WordVariants(uint qwbWordId)
				=> _qwbWordId = qwbWordId;

			protected override string HttpPath() => RequestPath.Replace(
					"/qwb-word-id"
					, $"/{HttpUtility.UrlEncode(_qwbWordId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _qwbWordId);
			}
		}

		public class V1_QwbProxy_Parallels_StartWord_QwbStartWordId_EndWord_QwbEndWordId :
				RequestObject<EmptyInput, QwbParallelListDTO>
		{
			private readonly uint _qwbEndWordId;
			private readonly uint _qwbStartWordId;

			/// <summary>
			///  Search QWB (via proxy) for any parallel text.
			/// </summary>
			/// <param name="qwbStartWordId">QWB word Id for the beginning of the text selection</param>
			/// <param name="qwbEndWordId">QWB word Id for the end of the text selection</param>
			/// <returns></returns>
			public V1_QwbProxy_Parallels_StartWord_QwbStartWordId_EndWord_QwbEndWordId(
					uint   qwbStartWordId
					, uint qwbEndWordId)

			{
				_qwbStartWordId = qwbStartWordId;
				_qwbEndWordId = qwbEndWordId;
			}

			protected override string HttpPath() => RequestPath
													.Replace(
															"/qwb-start-word-id"
															, $"/{HttpUtility.UrlEncode(_qwbStartWordId.ToString())}")
													.Replace(
															"/qwb-end-word-id"
															, $"/{HttpUtility.UrlEncode(_qwbEndWordId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _qwbStartWordId
							   , _qwbEndWordId);
			}
		}

		public class V1_QwbProxy_Bibliography_QwbBibliographyId :
				RequestObject<EmptyInput, QwbBibliographyEntryDTO>
		{
			private readonly uint _qwbBibliographyId;

			/// <summary>
			///  Get full bibliographic entry from QWB (via proxy).
			/// </summary>
			/// <param name="qwbBibliographyId">ID of the qwb bibliographical item to be retrieved</param>
			/// <returns></returns>
			public V1_QwbProxy_Bibliography_QwbBibliographyId(uint qwbBibliographyId)
				=> _qwbBibliographyId = qwbBibliographyId;

			protected override string HttpPath() => RequestPath.Replace(
					"/qwb-bibliography-id"
					, $"/{HttpUtility.UrlEncode(_qwbBibliographyId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _qwbBibliographyId);
			}
		}
	}
}