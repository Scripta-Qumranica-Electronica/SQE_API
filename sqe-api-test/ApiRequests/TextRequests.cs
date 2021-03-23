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
		public class V1_Editions_EditionId_Lines_LineId : RequestObject<EmptyInput, EmptyOutput>
		{
			private readonly uint _editionId;
			private readonly uint _lineId;

			/// <summary>
			///  Delete a full line from a text fragment
			/// </summary>
			/// <param name="editionId">Id of the edition</param>
			/// <param name="lineId">Id of the line to be deleted</param>
			/// <returns>
			///  The updated details concerning the line sequence
			/// </returns>
			public V1_Editions_EditionId_Lines_LineId(uint editionId, uint lineId)

			{
				_editionId = editionId;
				_lineId = lineId;
				AvailableListeners = new Listeners();

				_listenerDict.Add(
						ListenerMethods.DeletedLine
						, (DeletedLineIsNull, DeletedLineListener));
			}

			public Listeners AvailableListeners { get; }

			public DeleteIntIdDTO DeletedLine { get; private set; }

			private void DeletedLineListener(HubConnection signalrListener)
				=> signalrListener.On<DeleteIntIdDTO>(
						"DeletedLine"
						, receivedData => DeletedLine = receivedData);

			private bool DeletedLineIsNull() => DeletedLine == null;

			protected override string HttpPath() => RequestPath
													.Replace(
															"/edition-id"
															, $"/{HttpUtility.UrlEncode(_editionId.ToString())}")
													.Replace(
															"/line-id"
															, $"/{HttpUtility.UrlEncode(_lineId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _editionId
							   , _lineId);
			}

			public override uint? GetEditionId() => _editionId;

			public class Listeners
			{
				public ListenerMethods DeletedLine = ListenerMethods.DeletedLine;
			}
		}
	}

	public static partial class Get
	{
		public class V1_Editions_EditionId_TextFragments :
				RequestObject<EmptyInput, TextFragmentDataListDTO>
		{
			private readonly uint _editionId;

			/// <summary>
			///  Retrieves the ids of all Fragments of all fragments in the given edition of a scroll
			/// </summary>
			/// <param name="editionId">Id of the edition</param>
			/// <returns>An array of the text fragment ids in correct sequence</returns>
			public V1_Editions_EditionId_TextFragments(uint editionId) => _editionId = editionId;

			protected override string HttpPath() => RequestPath.Replace(
					"/edition-id"
					, $"/{HttpUtility.UrlEncode(_editionId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId);
			}

			public override uint? GetEditionId() => _editionId;
		}

		public class V1_Editions_EditionId_TextFragments_TextFragmentId_Artefacts :
				RequestObject<EmptyInput, ArtefactDataListDTO>
		{
			private readonly uint _editionId;
			private readonly uint _textFragmentId;

			/// <summary>
			///  Retrieves the ids of all Artefacts in the given textFragmentName
			/// </summary>
			/// <param name="editionId">Id of the edition</param>
			/// <param name="textFragmentId">Id of the text fragment</param>
			/// <returns>An array of the line ids in the proper sequence</returns>
			public V1_Editions_EditionId_TextFragments_TextFragmentId_Artefacts(
					uint   editionId
					, uint textFragmentId)

			{
				_editionId = editionId;
				_textFragmentId = textFragmentId;
			}

			protected override string HttpPath() => RequestPath
													.Replace(
															"/edition-id"
															, $"/{HttpUtility.UrlEncode(_editionId.ToString())}")
													.Replace(
															"/text-fragment-id"
															, $"/{HttpUtility.UrlEncode(_textFragmentId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _editionId
							   , _textFragmentId);
			}

			public override uint? GetEditionId() => _editionId;
		}

		public class V1_Editions_EditionId_TextFragments_TextFragmentId_Lines :
				RequestObject<EmptyInput, LineDataListDTO>
		{
			private readonly uint _editionId;
			private readonly uint _textFragmentId;

			/// <summary>
			///  Retrieves the ids of all lines in the given textFragmentName
			/// </summary>
			/// <param name="editionId">Id of the edition</param>
			/// <param name="textFragmentId">Id of the text fragment</param>
			/// <returns>An array of the line ids in the proper sequence</returns>
			public V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(
					uint   editionId
					, uint textFragmentId)

			{
				_editionId = editionId;
				_textFragmentId = textFragmentId;
			}

			protected override string HttpPath() => RequestPath
													.Replace(
															"/edition-id"
															, $"/{HttpUtility.UrlEncode(_editionId.ToString())}")
													.Replace(
															"/text-fragment-id"
															, $"/{HttpUtility.UrlEncode(_textFragmentId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _editionId
							   , _textFragmentId);
			}

			public override uint? GetEditionId() => _editionId;
		}

		public class V1_Editions_EditionId_TextFragments_TextFragmentId :
				RequestObject<EmptyInput, TextEditionDTO>
		{
			private readonly uint _editionId;
			private readonly uint _textFragmentId;

			/// <summary>
			///  Retrieves all signs and their data from the given textFragmentName
			/// </summary>
			/// <param name="editionId">Id of the edition</param>
			/// <param name="textFragmentId">Id of the text fragment</param>
			/// <returns>
			///  A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
			///  sequence
			/// </returns>
			public V1_Editions_EditionId_TextFragments_TextFragmentId(
					uint   editionId
					, uint textFragmentId)

			{
				_editionId = editionId;
				_textFragmentId = textFragmentId;
			}

			protected override string HttpPath() => RequestPath
													.Replace(
															"/edition-id"
															, $"/{HttpUtility.UrlEncode(_editionId.ToString())}")
													.Replace(
															"/text-fragment-id"
															, $"/{HttpUtility.UrlEncode(_textFragmentId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _editionId
							   , _textFragmentId);
			}

			public override uint? GetEditionId() => _editionId;
		}

		public class V1_Editions_EditionId_FullText : RequestObject<EmptyInput, TextEditionDTO>
		{
			private readonly uint _editionId;

			/// <summary>
			///  Retrieves all signs and their data from the entire edition
			/// </summary>
			/// <param name="editionId">Id of the edition</param>
			/// <returns>
			///  A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
			///  sequence
			/// </returns>
			public V1_Editions_EditionId_FullText(uint editionId) => _editionId = editionId;

			protected override string HttpPath() => RequestPath.Replace(
					"/edition-id"
					, $"/{HttpUtility.UrlEncode(_editionId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId);
			}

			public override uint? GetEditionId() => _editionId;
		}

		public class V1_Editions_EditionId_Lines_LineId : RequestObject<EmptyInput, LineTextDTO>
		{
			private readonly uint _editionId;
			private readonly uint _lineId;

			/// <summary>
			///  Retrieves all signs and their data from the given line
			/// </summary>
			/// <param name="editionId">Id of the edition</param>
			/// <param name="lineId">Id of the line</param>
			/// <returns>
			///  A manuscript edition object including the fragments and their lines in a
			///  hierarchical order and in correct sequence
			/// </returns>
			public V1_Editions_EditionId_Lines_LineId(uint editionId, uint lineId)

			{
				_editionId = editionId;
				_lineId = lineId;
			}

			protected override string HttpPath() => RequestPath
													.Replace(
															"/edition-id"
															, $"/{HttpUtility.UrlEncode(_editionId.ToString())}")
													.Replace(
															"/line-id"
															, $"/{HttpUtility.UrlEncode(_lineId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _editionId
							   , _lineId);
			}

			public override uint? GetEditionId() => _editionId;
		}
	}

	public static partial class Post
	{
		public class V1_Editions_EditionId_TextFragments :
				RequestObject<CreateTextFragmentDTO, TextFragmentDataDTO>
		{
			private readonly uint                  _editionId;
			private readonly CreateTextFragmentDTO _payload;

			/// <summary>
			///  Creates a new text fragment in the given edition of a scroll
			/// </summary>
			/// <param name="createFragment">A JSON object with the details of the new text fragment to be created</param>
			/// <param name="editionId">Id of the edition</param>
			public V1_Editions_EditionId_TextFragments(
					uint                    editionId
					, CreateTextFragmentDTO payload) : base(payload)
			{
				_editionId = editionId;
				_payload = payload;
				AvailableListeners = new Listeners();

				_listenerDict.Add(
						ListenerMethods.CreatedTextFragment
						, (CreatedTextFragmentIsNull, CreatedTextFragmentListener));
			}

			public Listeners AvailableListeners { get; }

			public TextFragmentDataDTO CreatedTextFragment { get; private set; }

			private void CreatedTextFragmentListener(HubConnection signalrListener)
				=> signalrListener.On<TextFragmentDataDTO>(
						"CreatedTextFragment"
						, receivedData => CreatedTextFragment = receivedData);

			private bool CreatedTextFragmentIsNull() => CreatedTextFragment == null;

			protected override string HttpPath() => RequestPath.Replace(
					"/edition-id"
					, $"/{HttpUtility.UrlEncode(_editionId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _editionId
							   , _payload);
			}

			public override uint? GetEditionId() => _editionId;

			public class Listeners
			{
				public ListenerMethods CreatedTextFragment = ListenerMethods.CreatedTextFragment;
			}
		}

		public class V1_Editions_EditionId_TextFragments_TextFragmentId_Lines :
				RequestObject<CreateLineDTO, LineDataDTO>
		{
			private readonly uint          _editionId;
			private readonly CreateLineDTO _payload;
			private readonly uint          _textFragmentId;

			/// <summary>
			///  Creates a new line before or after another line.
			/// </summary>
			/// <param name="editionId">Id of the edition</param>
			/// <param name="textFragmentId">
			///  Id of the text fragment where the line will be
			///  added
			/// </param>
			/// <param name="lineData">The information about the line to be created</param>
			/// <returns>
			///  The details concerning the newly created line
			/// </returns>
			public V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(
					uint            editionId
					, uint          textFragmentId
					, CreateLineDTO payload) : base(payload)
			{
				_editionId = editionId;
				_textFragmentId = textFragmentId;
				_payload = payload;
				AvailableListeners = new Listeners();

				_listenerDict.Add(
						ListenerMethods.CreatedLine
						, (CreatedLineIsNull, CreatedLineListener));
			}

			public Listeners AvailableListeners { get; }

			public LineDataDTO CreatedLine { get; private set; }

			private void CreatedLineListener(HubConnection signalrListener)
				=> signalrListener.On<LineDataDTO>(
						"CreatedLine"
						, receivedData => CreatedLine = receivedData);

			private bool CreatedLineIsNull() => CreatedLine == null;

			protected override string HttpPath() => RequestPath
													.Replace(
															"/edition-id"
															, $"/{HttpUtility.UrlEncode(_editionId.ToString())}")
													.Replace(
															"/text-fragment-id"
															, $"/{HttpUtility.UrlEncode(_textFragmentId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _editionId
							   , _textFragmentId
							   , _payload);
			}

			public override uint? GetEditionId() => _editionId;

			public class Listeners
			{
				public ListenerMethods CreatedLine = ListenerMethods.CreatedLine;
			}
		}
	}

	public static partial class Put
	{
		public class V1_Editions_EditionId_TextFragments_TextFragmentId :
				RequestObject<UpdateTextFragmentDTO, TextFragmentDataDTO>
		{
			private readonly uint                  _editionId;
			private readonly UpdateTextFragmentDTO _payload;
			private readonly uint                  _textFragmentId;

			/// <summary>
			///  Updates the specified text fragment with the submitted properties
			/// </summary>
			/// <param name="editionId">Edition of the text fragment being updates</param>
			/// <param name="textFragmentId">Id of the text fragment being updates</param>
			/// <param name="updatedTextFragment">Details of the updated text fragment</param>
			/// <returns>The details of the updated text fragment</returns>
			public V1_Editions_EditionId_TextFragments_TextFragmentId(
					uint                    editionId
					, uint                  textFragmentId
					, UpdateTextFragmentDTO payload) : base(payload)
			{
				_editionId = editionId;
				_textFragmentId = textFragmentId;
				_payload = payload;
				AvailableListeners = new Listeners();

				_listenerDict.Add(
						ListenerMethods.CreatedTextFragment
						, (CreatedTextFragmentIsNull, CreatedTextFragmentListener));
			}

			public Listeners AvailableListeners { get; }

			public TextFragmentDataDTO CreatedTextFragment { get; private set; }

			private void CreatedTextFragmentListener(HubConnection signalrListener)
				=> signalrListener.On<TextFragmentDataDTO>(
						"CreatedTextFragment"
						, receivedData => CreatedTextFragment = receivedData);

			private bool CreatedTextFragmentIsNull() => CreatedTextFragment == null;

			protected override string HttpPath() => RequestPath
													.Replace(
															"/edition-id"
															, $"/{HttpUtility.UrlEncode(_editionId.ToString())}")
													.Replace(
															"/text-fragment-id"
															, $"/{HttpUtility.UrlEncode(_textFragmentId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _editionId
							   , _textFragmentId
							   , _payload);
			}

			public override uint? GetEditionId() => _editionId;

			public class Listeners
			{
				public ListenerMethods CreatedTextFragment = ListenerMethods.CreatedTextFragment;
			}
		}

		public class V1_Editions_EditionId_Lines_LineId : RequestObject<UpdateLineDTO, LineDataDTO>
		{
			private readonly uint          _editionId;
			private readonly uint          _lineId;
			private readonly UpdateLineDTO _payload;

			/// <summary>
			///  Changes the details of the line (currently the lines name)
			/// </summary>
			/// <param name="editionId">Id of the edition</param>
			/// <param name="lineId">Id of the line</param>
			/// <param name="lineData">The updated line data</param>
			/// <returns>
			///  The updated details concerning the line sequence
			/// </returns>
			public V1_Editions_EditionId_Lines_LineId(
					uint            editionId
					, uint          lineId
					, UpdateLineDTO payload) : base(payload)
			{
				_editionId = editionId;
				_lineId = lineId;
				_payload = payload;
				AvailableListeners = new Listeners();

				_listenerDict.Add(
						ListenerMethods.UpdatedLine
						, (UpdatedLineIsNull, UpdatedLineListener));
			}

			public Listeners AvailableListeners { get; }

			public LineDataDTO UpdatedLine { get; private set; }

			private void UpdatedLineListener(HubConnection signalrListener)
				=> signalrListener.On<LineDataDTO>(
						"UpdatedLine"
						, receivedData => UpdatedLine = receivedData);

			private bool UpdatedLineIsNull() => UpdatedLine == null;

			protected override string HttpPath() => RequestPath
													.Replace(
															"/edition-id"
															, $"/{HttpUtility.UrlEncode(_editionId.ToString())}")
													.Replace(
															"/line-id"
															, $"/{HttpUtility.UrlEncode(_lineId.ToString())}");

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(
							   SignalrRequestString()
							   , _editionId
							   , _lineId
							   , _payload);
			}

			public override uint? GetEditionId() => _editionId;

			public class Listeners
			{
				public ListenerMethods UpdatedLine = ListenerMethods.UpdatedLine;
			}
		}
	}
}
