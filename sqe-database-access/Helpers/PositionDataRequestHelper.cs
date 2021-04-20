using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.DatabaseAccess.Helpers
{
	/// <summary>
	///  Holds the data of items which form a pair in the stream
	/// </summary>
	public class PositionDataPair
	{
		public uint ItemId             { get; set; }
		public uint NextItemId         { get; set; }
		public uint PositionInStreamId { get; set; }
		public bool ForkStart          { get; set; }
		public bool ForkEnd            { get; set; }
	}

	/// <summary>
	///  Defines the different stream types
	/// </summary>
	public enum StreamType
	{
		SignInterpretationStream
		, TextFragmentStream
		,
	}

	/// <summary>
	///  Gives the possible actions of changing position data. The actual actions also depend on the data provided in
	///  PositionData:
	///  Principally, from all items given a sub-stream is created which is treated like a single item in respect to the
	///  anchors.
	/// </summary>
	public enum PositionAction
	{
		/// <summary>
		///  Creates a path from the items.
		///  If anchors and/after before are set, the path will be connected to those anchors.
		///  Thus, to insert an item(-path) into an existing path, simply give the items
		///  between the path should be inserted as anchors before and after.
		///  Accordingly, to prepend or append a path simply do not specify an anchor after or before.
		///  It leaves already existing connections untouched.
		///  If the already existing connections of the anchors should be deleted,
		///  simply add break to the list of actions.
		/// </summary>
		CreatePathFromItems

		,

		/// <summary>
		///  Removes all connections between items given as anchors which are directly connected to each other.
		///  NOTE: This uses the anchors to define the items to be processed - the given items are ignored.
		///  If anchorsAfter is empty than all following connections of anchorsBefore are deleted and
		///  the original neighbours are set as anchorsAfter
		///  if anchorsBefore is empty than all preceding connections of anchorsAfter are deleted and
		///  the original neighbours are anchorsBefore
		/// </summary>
		DisconnectNeighbouringAnchors

		,

		/// <summary>
		///  Connects each anchor of anchorsBefore with each of anchorsAfter.
		/// </summary>
		ConnectAnchors

		,

		/// <summary>
		///  Ensures, that the given items represent a path - if not, it creates a path from them.
		///  All other edges leading into or out this path are disconnected from the path and connected
		///  to the nearest remaining neighbour.
		/// </summary>
		TakeOutPathOfItems

		,

		/// <summary>
		///  Like Delete but in case the items form a straight path without any forks,
		///  the now orphaned anchors before and after will be connected (if both exist):
		///  a->b->c, a->b->d, e->b->d with delete b and without anchors => a->c, e->d, e->d.
		///  Note: If the items form a path with forks,  anchors behind are
		///  left without a predecessor because if they are not connected to a branch
		///  which had not been affected by the delete.
		/// </summary>
		DeleteAndClose_CanBeDeleted

		,

		/// <summary>
		///  Takes the path described by the itemIds out using TakeOutPathOfItems
		///  and inserts it between the new anchors provided
		///  as anchors before and anchors after
		///  If the new anchors are adjacent, then they are split up: a->b => a->c->b
		/// </summary>
		MoveInBetween

		,
	}

	public static class PositionDataRequestFactory
	{
		/// <summary>
		///  Create a new PositionDataRequestHelper object with several items.
		///  We use an async factory method since we may need to await the
		///  response from AddExistingAnchors().
		/// </summary>
		/// <param name="dbConnection">IDbConnection for making queries to the database</param>
		/// <param name="streamType">An enum for either the sign stream or the text fragment stream</param>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="addExistingAnchors">
		///  Boolean whether or not to automatically add the anchors before and after
		///  the itemIds to the factory
		/// </param>
		/// <returns></returns>
		public static async Task<PositionDataRequestHelper> CreateInstanceAsync(
				IDbConnection dbConnection
				, StreamType  streamType
				, uint        editionId
				, bool        addExistingAnchors = false)
		{
			var newObject = new PositionDataRequestHelper(
					dbConnection
					, streamType
					, new List<uint>()
					, editionId);

			if (addExistingAnchors)
				await newObject.AddExistingAnchorsAsync();

			return newObject;
		}

		/// <summary>
		///  Create a new PositionDataRequestHelper object with several items.
		///  We use an async factory method since we may need to await the
		///  response from AddExistingAnchors().
		/// </summary>
		/// <param name="dbConnection">IDbConnection for making queries to the database</param>
		/// <param name="streamType">An enum for either the sign stream or the text fragment stream</param>
		/// <param name="itemIds">A list of item ids which should form a path</param>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="addExistingAnchors">
		///  Boolean whether or not to automatically add the anchors before and after
		///  the itemIds to the factory
		/// </param>
		/// <returns></returns>
		public static async Task<PositionDataRequestHelper> CreateInstanceAsync(
				IDbConnection dbConnection
				, StreamType  streamType
				, List<uint>  itemIds
				, uint        editionId
				, bool        addExistingAnchors = false)
		{
			var newObject = new PositionDataRequestHelper(
					dbConnection
					, streamType
					, itemIds
					, editionId);

			if (addExistingAnchors)
				await newObject.AddExistingAnchorsAsync();

			return newObject;
		}

		/// <summary>
		///  Create a new PositionDataRequestHelper object with only one item.
		///  We use an async factory method since we may need to await the
		///  response from AddExistingAnchors().
		/// </summary>
		/// <param name="dbConnection">IDbConnection for making queries to the database</param>
		/// <param name="streamType">An enum for either the sign stream or the text fragment stream</param>
		/// <param name="itemId">Id of the item to be manipulated</param>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="addExistingAnchors">
		///  Boolean whether or not to automatically
		///  add the anchors before and after
		///  the itemIds to the factory
		/// </param>
		/// <returns></returns>
		public static async Task<PositionDataRequestHelper> CreateInstanceAsync(
				IDbConnection dbConnection
				, StreamType  streamType
				, uint        itemId
				, uint        editionId
				, bool        addExistingAnchors = false) => await CreateInstanceAsync(
				dbConnection
				, streamType
				, new List<uint> { itemId }
				, editionId
				, addExistingAnchors);
	}

	public class PositionDataRequestHelper
	{
		private readonly List<PositionAction> _actions = new List<PositionAction>();

		private readonly IDbConnection _dbConnection;
		private readonly uint          _editionId;
		private readonly List<uint>    _itemIds;
		private readonly string        _itemName;
		private readonly string        _itemNameAt;
		private readonly string        _nextName;
		private readonly string        _nextNameAt;
		private readonly StreamType    _streamType;
		private readonly string        _tableName;

		/// <summary>
		///  We only have private constructors because objects should always be created
		///  by CreateInstanceAsync.
		/// </summary>
		/// <param name="dbConnection">DB connection object</param>
		/// <param name="streamType">Type of stream</param>
		/// <param name="itemIds">Ids of items to be processed as stream</param>
		/// <param name="editionId">Id of the edition we are dealing with</param>
		internal PositionDataRequestHelper(
				IDbConnection dbConnection
				, StreamType  streamType
				, List<uint>  itemIds
				, uint        editionId)
		{
			_dbConnection = dbConnection;
			_itemIds = itemIds ?? new List<uint>();
			_editionId = editionId;
			_streamType = streamType;

			if (streamType == StreamType.SignInterpretationStream)
			{
				_tableName = "position_in_stream";
				_itemNameAt = "@sign_interpretation_id";
				_nextNameAt = "@next_sign_interpretation_id";
				_itemName = "sign_interpretation_id";
				_nextName = "next_sign_interpretation_id";
			}
			else
			{
				_tableName = "position_in_text_fragment_stream";
				_itemNameAt = "@text_fragment_id";
				_nextNameAt = "@next_text_fragment_id";
				_itemName = "text_fragment_id";
				_nextName = "next_text_fragment_id";
			}
		}

		public bool AffectsOtherPaths { get; set; }

		public List<uint> AnchorsBefore { get; } = new List<uint>();
		public List<uint> AnchorsAfter  { get; } = new List<uint>();

		/// <summary>
		///  Adds all existing anchors which exist before the first and after the last item to the object
		/// </summary>
		/// <returns></returns>
		public async Task AddExistingAnchorsAsync()
		{
			AnchorsBefore.AddRange(await _getAnchorsBeforeAsync());
			AnchorsAfter.AddRange(await _getAnchorsAfterAsync());
		}

		/// <summary>
		///  Adds the given id as an anchor before
		/// </summary>
		/// <param name="anchorBefore">Id of item to be added as anchor before</param>
		public void AddAnchorBefore(uint anchorBefore)
		{
			AnchorsBefore.Add(anchorBefore);
		}

		/// <summary>
		///  Adds the given id as an anchor after
		/// </summary>
		/// <param name="anchorAfter">Id of item to be added as anchor after</param>
		public void AddAnchorAfter(uint anchorAfter)
		{
			AnchorsAfter.Add(anchorAfter);
		}

		/// <summary>
		///  Adds the given ids as anchors before
		/// </summary>
		/// <param name="anchorsBefore">List of ids of items to be added as anchor before</param>
		public void AddAnchorsBefore(List<uint> anchorsBefore)
		{
			AnchorsBefore.AddRange(anchorsBefore);
		}

		/// <summary>
		///  Adds the given ids as anchors after
		/// </summary>
		/// <param name="anchorsAfter">List of ids of items to be added as anchors after</param>
		public void AddAnchorsAfter(List<uint> anchorsAfter)
		{
			AnchorsAfter.AddRange(anchorsAfter);
		}

		/// <summary>
		///  Ads the id of an additional item.
		///  Note: this becomes the last item to be set before the anchors after
		/// </summary>
		/// <param name="itemId">Id of item</param>
		public void AddItemId(uint itemId)
		{
			_itemIds.Add(itemId);
		}

		/// <summary>
		///  Adds an action to be processed
		/// </summary>
		/// <param name="action"></param>
		public void AddAction(PositionAction action)
		{
			_actions.Add(action);
		}

		/// <summary>
		///  Creates a list of mutation requests from the data set in the object.
		/// </summary>
		/// <returns>List of mutation requests.</returns>
		public async Task<List<MutationRequest>> CreateRequestsAsync()
		{
			var requests = new List<MutationRequest>();
			AffectsOtherPaths = false;

			foreach (var action in _actions)
			{
				switch (action)
				{
					case PositionAction.CreatePathFromItems:
						requests.AddRange(await _createRequestForCreatePathAsync());

						break;

					case PositionAction.DisconnectNeighbouringAnchors:
						requests.AddRange(await _createRequestForDisconnectAnchorsAsync());

						break;

					case PositionAction.ConnectAnchors:
						requests.AddRange(_createRequestForConnectAnchors());

						break;

					case PositionAction.TakeOutPathOfItems:
						requests.AddRange(await _createRequestForTakingOutItemsAsync());

						break;

					case PositionAction.MoveInBetween:
						requests.AddRange(await _createMoveToRequestsAsync());

						break;
				}
			}

			return requests;
		}

		/// <summary>
		///  Creates the requests for creating a path from the given items and to connect the path
		///  with the given anchors.
		/// </summary>
		/// <returns></returns>
		private async Task<List<MutationRequest>> _createRequestForCreatePathAsync()
		{
			var requests = new List<MutationRequest>();

			//Create a path of all items if there are more than one
			if (_itemIds.Count > 1)
			{
				for (var i = 0; i < (_itemIds.Count() - 1); i++)
					await _addRequestForCreatingPairAsync(requests, _itemIds[i], _itemIds[i + 1]);
			}

			// Add each anchor before to the first item if the pair does not yet exist
			foreach (var anchorBefore in AnchorsBefore)
				await _addRequestForCreatingPairAsync(requests, anchorBefore, _itemIds.First());

			// Add each anchor after to the last item if the pair does not yet exist
			foreach (var anchorAfter in AnchorsAfter)
				await _addRequestForCreatingPairAsync(requests, _itemIds.Last(), anchorAfter);

			return requests;
		}

		/// <summary>
		///  Helper which first checks whether the requested pair does not exist yet and if not
		///  adds a request to create it to the given list of requests.
		/// </summary>
		/// <param name="requests">List of requests</param>
		/// <param name="itemId">Id of first item for the pair</param>
		/// <param name="nextItemId">Id of the second item ot the pair.</param>
		/// <returns></returns>
		private async Task _addRequestForCreatingPairAsync(
				ICollection<MutationRequest> requests
				, uint                       itemId
				, uint                       nextItemId)
		{
			if (await _getExistingPairAsync(itemId, nextItemId) == null)
			{
				requests.Add(
						new MutationRequest(
								MutateType.Create
								, _createMutationRequestParameters(itemId, nextItemId)
								, _tableName));
			}
		}

		/// <summary>
		///  Creates requests to break all connections between the anchors.
		///  If only one set of anchors is given, this set is divided from the respective neighbours
		///  and the neighbours are set as the corresponding set of anchors.
		/// </summary>
		/// <returns></returns>
		private async Task<List<MutationRequest>> _createRequestForDisconnectAnchorsAsync()
		{
			List<PositionDataPair> pairs;

			if (AnchorsBefore.Any()
				&& AnchorsAfter.Any())
				pairs = await _getExistingPairsAsync(AnchorsBefore, AnchorsAfter);
			else if (AnchorsBefore.Any())
			{
				pairs = await _getExistingPairsFromItemsAsync(AnchorsBefore);

				AnchorsAfter.AddRange(pairs.Select(p => p.NextItemId));
			}
			else
			{
				pairs = await _getExistingPairsFromNextItemsAsync(AnchorsAfter);

				AnchorsBefore.AddRange(pairs.Select(p => p.ItemId));
			}

			return pairs.Select(
								p => new MutationRequest(
										MutateType.Delete
										, new DynamicParameters()
										, _tableName
										, p.PositionInStreamId))
						.ToList();
		}

		/// <summary>
		///  Creates the mutation requests to connect each anchor before with the anchors after.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<MutationRequest> _createRequestForConnectAnchors()
			=> (from anchorBefore in AnchorsBefore
				from anchorAfter in AnchorsAfter
				select new MutationRequest(
						MutateType.Create
						, _createMutationRequestParameters(anchorBefore, anchorAfter)
						, _tableName)).ToList();

		/// <summary>
		///  Creates the mutation requests to take all items out of the path.
		///  All connections with other items are connected with the next neighbouring remaining item
		/// </summary>
		/// <returns></returns>
		private async Task<List<MutationRequest>> _createRequestForTakingOutItemsAsync()
		{
			var requests = new List<MutationRequest>();
			var looseNeighboursBefore = new List<uint>();
			uint previousItem = 0;

			for (var i = 0; i < _itemIds.Count; i++)
			{
				var itemAsList = _itemIds.GetRange(i, 1);
				var nextItem = _itemIds.ElementAtOrDefault(i + 1);

				// First treat the connections with the neighbours before
				var neighboursBefore = await _getExistingPairsFromNextItemsAsync(itemAsList);

				// Take out the connection with the item preceding in the items list
				neighboursBefore.RemoveAll(id => id.NextItemId == previousItem);

				// If we are not dealing with the first item and they are neighbours before
				// we must set AffectOtherPaths as true if still false.
				AffectsOtherPaths = !AffectsOtherPaths && (i > 0) && neighboursBefore.Any();

				foreach (var neighbourBefore in neighboursBefore)
				{
					// We must delete all connections from the item to existing previous items
					requests.Add(
							new MutationRequest(
									MutateType.Delete
									, new DynamicParameters()
									, _tableName
									, neighbourBefore.PositionInStreamId));

					// Add the id of the neughbour before to the looseNeighboursBefore list.
					looseNeighboursBefore.Add(neighbourBefore.ItemId);
				}

				// Now work on the neighbours after
				var neighboursAfter = await _getExistingPairsFromItemsAsync(itemAsList);

				// Take out the connection with the item following in the items list
				neighboursAfter.RemoveAll(id => id.NextItemId == nextItem);

				// If still neighbours after are present
				if (neighboursAfter.Any())
				{
					// If we are not dealing with the last item
					// we must set AffectOtherPaths as true if still false.
					AffectsOtherPaths = !AffectsOtherPaths && (i < (_itemIds.Count - 1));

					foreach (var neighbourAfter in neighboursAfter)
					{
						// We must delete all connections from the item to existing next items
						requests.Add(
								new MutationRequest(
										MutateType.Delete
										, new DynamicParameters()
										, _tableName
										, neighbourAfter.PositionInStreamId));

						// The disconnected items must be connected to the loose items before
						foreach (var looseNeighbourBefore in looseNeighboursBefore)
						{
							var tmpFactory = await PositionDataRequestFactory.CreateInstanceAsync(
									_dbConnection
									, _streamType
									, new List<uint>
									{
											looseNeighbourBefore
											, neighbourAfter.NextItemId
											,
									}
									, _editionId);

							tmpFactory.AddAction(PositionAction.CreatePathFromItems);

							requests.AddRange(await tmpFactory.CreateRequestsAsync());
						}
					}

					// All loose items before are now connected and the list can be cleared.
					looseNeighboursBefore.Clear();
				}

				previousItem = _itemIds[i];
			}

			return requests;
		}

		private async Task<List<MutationRequest>> _createMoveToRequestsAsync(
				bool splitAnchors = true)
		{
			// First create requests to take out items from their old places
			var requests = await _createRequestForTakingOutItemsAsync();

			if (splitAnchors)
				requests.AddRange(await _createRequestForDisconnectAnchorsAsync());

			// Then put them as a path between the new anchors
			requests.AddRange(await _createRequestForCreatePathAsync());

			return requests;
		}

		/// <summary>
		///  Returns the number of items following the item with itemId
		///  which must at least be followed by the item with nextItemId
		/// </summary>
		/// <param name="itemId"></param>
		/// <param name="nextItemId"></param>
		/// <param name="editionId"></param>
		/// <returns>
		///  0 if the item is not followed by the item with nextItemId,
		///  1 if it only followed by this item
		///  and > 1 with the numbers of all items following
		/// </returns>
		private async Task<uint> _getNumberOfNextItemsAsync(uint itemId, uint nextItemId)
		{
			var queryForConnection = $@"
                SELECT COUNT({
					_tableName
				}_id)
                FROM {
					_tableName
				}
                    JOIN {
						_tableName
					}_owner USING ({
						_tableName
					}_id)
                WHERE {
					_itemName
				}=@ItemId
                    AND edition_id=@EditionId
                    AND (
                        SELECT 1
                        FROM {
							_tableName
						}
                            JOIN {
								_tableName
							}_owner USING ({
								_tableName
							}_id)
                        WHERE {
							_itemName
						}=@ItemId
                            AND {
								_nextName
							}=@NextItemId
                            AND edition_id=@EditionId
                         ) = 1";

			var parameters = new DynamicParameters();
			parameters.Add("@ItemId", itemId);
			parameters.Add("@NextItemId", nextItemId);
			parameters.Add("@EditionId", _editionId);

			return await _dbConnection.QueryFirstAsync<uint>(queryForConnection, parameters);
		}

		/// <summary>
		///  Returns all existing pairs with the item ids as first element and the next item Ids as second
		/// </summary>
		/// <param name="itemIds">List of item ids to be first items</param>
		/// <param name="nextItemIds">List of item ids to be the second item</param>
		/// <returns>List of position data pair</returns>
		private async Task<List<PositionDataPair>> _getExistingPairsAsync(
				List<uint>   itemIds
				, List<uint> nextItemIds)
		{
			var queryForConnection = $@"
                SELECT {
					_itemName
				} AS ItemId,
                       {
						   _nextName
					   } AS NextItemId,
                       {
						   _tableName
					   }_id AS PositionInStreamId
                FROM {
					_tableName
				}
                    JOIN {
						_tableName
					}_owner USING ({
						_tableName
					}_id)
                WHERE {
					_itemName
				} in @ItemIds
                    AND {
						_nextName
					} in @NextItemIds
                    AND edition_id=@EditionId";

			var parameters = new DynamicParameters();
			parameters.Add("@ItemIds", itemIds);
			parameters.Add("@NextItemIds", nextItemIds);
			parameters.Add("@EditionId", _editionId);

			var result =
					await _dbConnection.QueryAsync<PositionDataPair>(
							queryForConnection
							, parameters);

			return result == null
					? new List<PositionDataPair>()
					: result.ToList();
		}

		/// <summary>
		///  Returns the pair with the item id as first element and the next item Id as second
		/// </summary>
		/// <param name="itemId">Item id to be first item</param>
		/// <param name="nextItemId">Item id to be the second item</param>
		/// <returns>Position data pair</returns>
		private async Task<PositionDataPair> _getExistingPairAsync(uint itemId, uint nextItemId)
		{
			var result = await _getExistingPairsAsync(
					new List<uint> { itemId }
					, new List<uint> { nextItemId });

			return result.Any()
					? result.First()
					: null;
		}

		/// <summary>
		///  Returns all existing pairs with the item ids as first element
		/// </summary>
		/// <param name="itemIds">List of item ids to be first items</param>
		/// <returns>List of position data pair</returns>
		private async Task<List<PositionDataPair>> _getExistingPairsFromItemsAsync(
				List<uint> itemIds)
		{
			var queryForConnection = $@"
                SELECT {
					_itemName
				} AS ItemId,
                       {
						   _nextName
					   } AS NextItemId,
                       {
						   _tableName
					   }_id AS PositionInStreamId
                FROM {
					_tableName
				}
                    JOIN {
						_tableName
					}_owner USING ({
						_tableName
					}_id)
                WHERE {
					_itemName
				} in @ItemIds
                    AND edition_id=@EditionId";

			var parameters = new DynamicParameters();
			parameters.Add("@ItemIds", itemIds);
			parameters.Add("@EditionId", _editionId);

			var result =
					await _dbConnection.QueryAsync<PositionDataPair>(
							queryForConnection
							, parameters);

			return result == null
					? new List<PositionDataPair>()
					: result.ToList();
		}

		/// <summary>
		///  Returns all existing pairs with the next item ids as second element
		/// </summary>
		/// <param name="nextItemIds">List of item ids to be second items</param>
		/// <returns>List of position data pair</returns>
		private async Task<List<PositionDataPair>> _getExistingPairsFromNextItemsAsync(
				List<uint> nextItemIds)
		{
			var queryForConnection = $@"
                SELECT {
					_itemName
				} AS ItemId,
                       {
						   _nextName
					   } AS NextItemId,
                       {
						   _tableName
					   }_id AS PositionInStreamId
                FROM {
					_tableName
				}
                    JOIN {
						_tableName
					}_owner USING ({
						_tableName
					}_id)
                WHERE {
					_nextName
				} in @NextItemIds
                    AND edition_id=@EditionId";

			var parameters = new DynamicParameters();
			parameters.Add("@NextItemIds", nextItemIds);
			parameters.Add("@EditionId", _editionId);

			var result =
					await _dbConnection.QueryAsync<PositionDataPair>(
							queryForConnection
							, parameters);

			return result == null
					? new List<PositionDataPair>()
					: result.ToList();
		}

		private async Task<bool> _endsWithLastElementAsync()
		{
			var result = (await _getExistingPairsFromItemsAsync(new List<uint> { _itemIds.Last() }))
					.ToList();

			return result.Any();
		}

		/// <summary>
		///  Creates the parameters for the mutation request for the item and the item which (should) follow(s)
		/// </summary>
		/// <param name="itemId"></param>
		/// <param name="nextItemId"></param>
		/// <returns>Dynamic parameters object</returns>
		private DynamicParameters _createMutationRequestParameters(uint itemId, uint nextItemId)
		{
			var parameters = new DynamicParameters();
			parameters.Add(_itemNameAt, itemId);
			parameters.Add(_nextNameAt, nextItemId);

			return parameters;
		}

		/// <summary>
		///  Gets the list of all leading anchors of the item
		/// </summary>
		/// <returns>List of ids of the anchors before</returns>
		private async Task<List<uint>> _getAnchorsBeforeAsync()
		{
			var queryForConnection = $@"
                SELECT {
					_itemName
				}
                FROM {
					_tableName
				}
                    JOIN {
						_tableName
					}_owner USING ({
						_tableName
					}_id)
                WHERE  {
					_nextName
				} = @ItemId
                    AND edition_id=@EditionId";

			var parameters = new DynamicParameters();
			parameters.Add("@ItemId", _itemIds.First());
			parameters.Add("@EditionId", _editionId);

			var result = await _dbConnection.QueryAsync<uint>(queryForConnection, parameters);

			return result == null
					? new List<uint>()
					: result.ToList();
		}

		/// <summary>
		///  Gets the list of all  anchors following the item
		/// </summary>
		/// <returns>List of ids of the anchors after</returns>
		private async Task<List<uint>> _getAnchorsAfterAsync()
		{
			var queryForConnection = $@"
                SELECT {
					_nextName
				}
                FROM {
					_tableName
				}
                    JOIN {
						_tableName
					}_owner USING ({
						_tableName
					}_id)
                WHERE  {
					_itemName
				} = @ItemId
                    AND edition_id=@EditionId";

			var parameters = new DynamicParameters();

			parameters.Add("@ItemId", _itemIds.Last(), DbType.AnsiString);

			parameters.Add("@EditionId", _editionId);

			var result = await _dbConnection.QueryAsync<uint>(queryForConnection, parameters);

			return result == null
					? new List<uint>()
					: result.ToList();
		}
	}
}
