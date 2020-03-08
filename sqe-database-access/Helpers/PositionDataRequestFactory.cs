using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace SQE.DatabaseAccess.Helpers
{
    public class PositionDataPair
    {
        public uint ItemId { get; set; }
        public uint NextItemId { get; set; }
        public uint PositionInStreamId { get; set; }
    }

    public enum StreamType
    {
        SignStream,
        TextFragmentStream
    }

    /// <summary>
    ///     Gives the possible actions of changing position data. The actual actions also depend on the data provided in
    ///     PositionData:
    ///     Principally, from all items given a sub-stream is created which is treated like a single item in respect to the
    ///     anchors.
    /// </summary>
    public enum PositionAction
    {
        /// <summary>
        ///     Creates a path from the items.
        ///     If anchors and/after before are set, the path will be connected to those anchors.
        ///     Thus, to insert an item(-path) into an existing path, simply give the items
        ///     between the path should be inserted as anchors before and after.
        ///     Accordingly, to prepend or append a path simply do not specify an anchor after or before.
        ///     It leaves already existing connections untouched.
        ///     If already existing connections of the anchors should be deleted, simply add break to the list of actions.
        /// </summary>
        Add,

        /// <summary>
        ///     Removes all connections between the anchors - items are ignored.
        ///     If anchorsAfter is empty than all following connections of anchorsBefore are deleted,
        ///     if anchorsBefore is empty than all preceding connections of anchorsAfter are deleted.
        /// </summary>
        Break,

        /// <summary>
        ///     Connects each anchor of anchorsBefore with each of anchorsAfter.
        /// </summary>
        Connect,

        /// <summary>
        ///     Deletes the item(-stream)  from the stream.
        ///     If no anchors are given, all instances of the item(-stream) are deleted,
        ///     if anchors are given, only those connected with the anchors are deleted:
        ///     a->b->c, a->b->d, e->b->d with delete b and without anchors => a, c, d, e,
        ///     with delete b and anchorBefore a will leave e->b->d untouched
        ///     with delete b and anchorsAfter c will leave a->b->d and e->b->d untouched
        ///     with delete b and anchorBefore a and anchorsAfter d will leave a->b->c untouched.
        /// </summary>
        Delete,

        /// <summary>
        ///     Like Delete but in case the items form a straight path without any forks,
        ///     the now orphaned anchors before and after will be connected (if both exist):
        ///     a->b->c, a->b->d, e->b->d with delete b and without anchors => a->c, e->d, e->d.
        ///     Note: If the items form a path with fork,  anchors behind are
        ///     left without a predecessor because if they are not connected to a branch nt affected by the delete.
        /// </summary>
        DeleteAndClose,

        /// <summary>
        ///     Takes the path described by the itemIds out and inserts it between the new anchors provided as Ids
        ///     If the new anchors are adjacent, then they are split up: a->b => a->c->b
        ///     The gap between the old anchors is closed.
        /// </summary>
        MoveTo
    }

    public class PositionDataRequestFactory
    {
        private readonly List<PositionAction> _actions = new List<PositionAction>();
        private readonly List<uint> _anchorsAfter = new List<uint>();
        private readonly List<uint> _anchorsBefore = new List<uint>();
        private readonly IDbConnection _dbConnection;
        private readonly uint _editionId;
        private readonly List<uint> _itemIds;
        private readonly string _itemName;
        private readonly string _itemNameAt;
        private readonly string _nextName;
        private readonly string _nextNameAt;
        private readonly StreamType _streamType;
        private readonly string _tableName;

        public PositionDataRequestFactory(IDbConnection dbConnection,
            StreamType streamType,
            List<uint> itemIds,
            uint editionId)
        {
            _dbConnection = dbConnection;
            _itemIds = itemIds == null ? new List<uint>() : itemIds;
            _editionId = editionId;
            _streamType = streamType;
            if (streamType == StreamType.SignStream)
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

        public PositionDataRequestFactory(IDbConnection dbConnection,
            StreamType streamType,
            uint itemId,
            uint editionId) : this(
            dbConnection,
            streamType,
            new List<uint> { itemId },
            editionId
        )
        {
        }

        /// <summary>
        ///     Create a new PositionDataRequestFactory object. We use an async factory method since we may need to await the
        ///     response from AddExistingAnchors().
        /// </summary>
        /// <param name="dbConnection">IDbConnection for making queries to the database</param>
        /// <param name="streamType">An enum for either the sign stream or the text fragment stream</param>
        /// <param name="itemIds">A list of item ids already arranged as a path</param>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="addExistingAnchors">
        ///     Boolean whether or not to automatically add the anchors before and after
        ///     the itemIds to the factory
        /// </param>
        /// <returns></returns>
        public static async Task<PositionDataRequestFactory> CreateInstanceAsync(IDbConnection dbConnection,
            StreamType streamType,
            List<uint> itemIds,
            uint editionId,
            bool addExistingAnchors = false)
        {
            var newObject =
                new PositionDataRequestFactory(dbConnection, streamType, itemIds, editionId);
            if (addExistingAnchors) await newObject.AddExistingAnchorsAsync();
            return newObject;
        }

        /// <summary>
        ///     Create a new PositionDataRequestFactory object. We use an async factory method since we may need to await the
        ///     response from AddExistingAnchors().
        /// </summary>
        /// <param name="dbConnection">IDbConnection for making queries to the database</param>
        /// <param name="streamType">An enum for either the sign stream or the text fragment stream</param>
        /// <param name="itemId">Id of the item to be manipulated</param>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="addExistingAnchors">
        ///     Boolean whether or not to automatically add the anchors before and after
        ///     the itemIds to the factory
        /// </param>
        /// <returns></returns>
        public static async Task<PositionDataRequestFactory> CreateInstanceAsync(IDbConnection dbConnection,
            StreamType streamType,
            uint itemId,
            uint editionId,
            bool addExistingAnchors = false)
        {
            return await CreateInstanceAsync(
                dbConnection,
                streamType,
                new List<uint> { itemId },
                editionId,
                addExistingAnchors
            );
        }

        public async Task AddExistingAnchorsAsync()
        {
            _anchorsBefore.AddRange(await _getAnchorsBeforeAsync());
            _anchorsAfter.AddRange(await _getAnchorsAfterAsync());
        }

        public void AddAnchorBefore(uint anchorBefore)
        {
            _anchorsBefore.Add(anchorBefore);
        }

        public void AddAnchorAfter(uint anchorAfter)
        {
            _anchorsAfter.Add(anchorAfter);
        }

        public void AddAnchorsBefore(List<uint> anchorsBefore)
        {
            _anchorsBefore.AddRange(anchorsBefore);
        }

        public void AddAnchorsAfter(List<uint> anchorsAfter)
        {
            _anchorsAfter.AddRange(anchorsAfter);
        }

        public void AddItemId(uint itemId)
        {
            _itemIds.Add(itemId);
        }

        public void AddAction(PositionAction action)
        {
            _actions.Add(action);
        }

        public List<uint> getAnchorsBefore()
        {
            return _anchorsBefore;
        }

        public List<uint> getAnchorsAfter()
        {
            return _anchorsAfter;
        }

        public async Task<List<MutationRequest>> CreateRequestsAsync()
        {
            var requests = new List<MutationRequest>();
            foreach (var action in _actions)
                switch (action)
                {
                    case PositionAction.Add:
                        requests.AddRange(_createRequestForAdd());
                        break;
                    case PositionAction.Break:
                        requests.AddRange(await _createRequestForBreakAsync());
                        break;
                    case PositionAction.Connect:
                        requests.AddRange(_createRequestForConnect());
                        break;
                    case PositionAction.Delete:
                        requests.AddRange(await _createRequestForDeleteAsync(false));
                        break;
                    case PositionAction.DeleteAndClose:
                        requests.AddRange(await _createRequestForDeleteAsync(true));
                        break;
                    case PositionAction.MoveTo:
                        requests.AddRange(await _createMoveToRequestsAsync());
                        break;
                }

            return requests;
        }

        private List<MutationRequest> _createRequestForAdd()
        {
            var requests = new List<MutationRequest>();

            //Create a path of all items
            if (_itemIds.Count > 1)
                for (var i = 0; i < _itemIds.Count() - 1; i++)
                {
                    var itemId = _itemIds[i];
                    var nextItemId = _itemIds[i + 1];
                    if (_getExistingPairAsync(itemId, nextItemId) == null)
                        requests.Add(
                            new MutationRequest(
                                MutateType.Create,
                                _createParameters(itemId, nextItemId),
                                _tableName
                            )
                        );
                }

            foreach (var anchorBefore in _anchorsBefore)
                requests.Add(
                    new MutationRequest(
                        MutateType.Create,
                        _createParameters(
                            anchorBefore,
                            _itemIds.First()
                        ),
                        _tableName
                    )
                );

            foreach (var anchorBehind in _anchorsAfter)
                requests.Add(
                    new MutationRequest(
                        MutateType.Create,
                        _createParameters(_itemIds.Last(), anchorBehind),
                        _tableName
                    )
                );

            return requests;
        }

        private async Task<List<MutationRequest>> _createRequestForBreakAsync()
        {
            var requests = new List<MutationRequest>();
            List<PositionDataPair> pairs;
            if (_anchorsBefore.Any()
                && _anchorsAfter.Any())
                pairs = await _getExistingPairsAsync(_anchorsBefore, _anchorsAfter);
            else if (_anchorsBefore.Any())
                pairs = await _getExistingPairsFromItemsAsync(_anchorsBefore);
            else pairs = await _getExistingPairsFromNextItemsAsync(_anchorsAfter);

            var paramName = $"@{_tableName}_id";
            foreach (var positionInStreamId in pairs.Select(p => p.PositionInStreamId))
                requests.Add(
                    new MutationRequest(
                        MutateType.Delete,
                        new DynamicParameters(),
                        _tableName,
                        positionInStreamId
                    )
                );

            return requests;
        }

        private List<MutationRequest> _createRequestForConnect()
        {
            var requests = new List<MutationRequest>();

            foreach (var anchor in _anchorsBefore)
                foreach (var itemId in _anchorsAfter)
                    requests.Add(
                        new MutationRequest(
                            MutateType.Create,
                            _createParameters(anchor, itemId),
                            _tableName
                        )
                    );

            return requests;
        }

        private async Task<List<MutationRequest>> _createRequestForDeleteAsync(bool connect)
        {
            var requests = new List<MutationRequest>();

            //First test, whether the itemIds represent a path, and if,
            //whether this is a straight path without forks.
            // Only those parts not followed by a fork should be deleted
            var singlePath = true;
            var i = 0;
            if (_itemIds.Count() > 1)
                for (i = 0; i < _itemIds.Count() - 1; i++)
                {
                    var itemId = _itemIds[i];
                    var nextItemId = _itemIds[i + 1];
                    var numbersOfNextItems = await _getNumberOfNextItemsAsync(itemId, nextItemId);

                    if (numbersOfNextItems == 0)
                    {
                        // The path is broken, thus we finish here without any mutations to be done
                        requests.Clear();
                        return requests;
                    }

                    if (numbersOfNextItems > 1)
                    {
                        // The path forks at this point into different sub-paths
                        // Thus we must not delete the path leading to this point.
                        // But from here on the sub-path given by the items might needed to be deleted.
                        requests.Clear();
                        singlePath = false;
                    }

                    // Mark this part of the path to be deleted
                    var pair = await _getExistingPairAsync(itemId, nextItemId);
                    if (pair != null)
                        requests.Add(
                            new MutationRequest(
                                MutateType.Delete,
                                new DynamicParameters(),
                                _tableName,
                                pair.PositionInStreamId
                            )
                        );
                }

            // Now we have to check, whether there are anchors required for the path.
            if (_anchorsBefore.Any()) // There are anchors requested before
            {
                // First get all anchors which in fact are connected to the path
                var connections = await _getExistingPairsAsync(
                    _anchorsBefore,
                    _itemIds.GetRange(0, 1)
                );
                if (connections.Count == 0)
                // If no such connections exist, the path is broken, 
                // thus we finish here without any mutations to be done
                {
                    requests.Clear();
                    return requests;
                }

                if (singlePath)
                    // If we have a straight path,
                    // we must also delete the connections to the anchor
                    foreach (var pair in connections)
                        requests.Add(
                            new MutationRequest(
                                MutateType.Delete,
                                new DynamicParameters(),
                                _tableName,
                                pair.PositionInStreamId
                            )
                        );
            }

            if (_anchorsAfter.Any()) // There are anchors requested behind
            {
                // First get all anchors which in fact are connected to the path
                var connections = await _getExistingPairsAsync(
                    _itemIds.GetRange(_itemIds.Count() - 1, 1),
                    _anchorsAfter
                );
                if (connections.Count() == 0)
                // If no such connections exist, the path is broken, 
                // thus we finish here without any mutations to be done
                {
                    requests.Clear();
                    return requests;
                }

                // Delete the connections to the anchors behind
                foreach (var pair in connections)
                    requests.Add(
                        new MutationRequest(
                            MutateType.Delete,
                            new DynamicParameters(),
                            _tableName,
                            pair.PositionInStreamId
                        )
                    );
            }

            if (connect && singlePath)
                requests.AddRange(_createRequestForConnect());

            return requests;
        }

        private async Task<List<MutationRequest>> _createMoveToRequestsAsync()
        {
            var requests = new List<MutationRequest>();
            foreach (var itemId in _itemIds)
            {
                var tempData = await CreateInstanceAsync(
                    _dbConnection,
                    _streamType,
                    itemId,
                    _editionId,
                    true
                );
                tempData.AddAction(PositionAction.DeleteAndClose);
                requests.AddRange(await tempData.CreateRequestsAsync());
            }


            if (_anchorsBefore.Any()
                && _anchorsAfter.Any())
                foreach (var anchorBefore in _anchorsBefore)
                    foreach (var anchorBehind in _anchorsAfter)
                    {
                        var pair = _getExistingPairAsync(anchorBefore, anchorBehind);
                        if (pair != null) requests.AddRange(await _createRequestForBreakAsync());
                    }

            requests.AddRange(_createRequestForAdd());
            return requests;
        }

        /// <summary>
        ///     Returns the number of items following the item with itemId
        ///     which must at least be followed by the item with nextItemId
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="nextItemId"></param>
        /// <param name="editionId"></param>
        /// <returns>
        ///     0 if the item is not followed by the item with nextItemId,
        ///     1 if it only followed by this item
        ///     and > 1 with the numbers of all items following
        /// </returns>
        private async Task<uint> _getNumberOfNextItemsAsync(uint itemId, uint nextItemId)
        {
            var queryForConnection = $@"
                SELECT COUNT({_tableName}_id)
                FROM {_tableName}
                    JOIN {_tableName}_owner USING ({_tableName}_id)
                WHERE {_itemName}=@ItemId
                    AND edition_id=@EditionId
                    AND (
                        SELECT 1
                        FROM {_tableName}
                            JOIN {_tableName}_owner USING ({_tableName}_id)
                        WHERE {_itemName}=@ItemId 
                            AND {_nextName}=@NextItemId 
                            AND edition_id=@EditionId
                         ) = 1";
            var parameters = new DynamicParameters();
            parameters.Add("@ItemId", itemId);
            parameters.Add("@NextItemId", nextItemId);
            parameters.Add("@EditionId", _editionId);

            return await _dbConnection.QueryFirstAsync<uint>(queryForConnection, parameters);
        }

        private async Task<List<PositionDataPair>> _getExistingPairsAsync(List<uint> itemIds, List<uint> nextItemIds)
        {
            var queryForConnection = $@"
                SELECT {_itemName} AS ItemId, 
                       {_nextName} AS NextItemId, 
                       {_tableName}_id AS PositionInStreamId
                FROM {_tableName}
                    JOIN {_tableName}_owner USING ({_tableName}_id)
                WHERE {_itemName} in @ItemIds
                    AND {_nextName} in @NextItemIds
                    AND edition_id=@EditionId"
                ;
            var parameters = new DynamicParameters();
            parameters.Add("@ItemIds", itemIds);
            parameters.Add("@NextItemIds", nextItemIds);
            parameters.Add("@EditionId", _editionId);

            var result = await _dbConnection.QueryAsync<PositionDataPair>(queryForConnection, parameters);
            return result == null ? new List<PositionDataPair>() : result.ToList();
        }

        private async Task<PositionDataPair> _getExistingPairAsync(uint itemId, uint nextItemId)
        {
            var result = await _getExistingPairsAsync(
                new List<uint> { itemId },
                new List<uint> { nextItemId }
            );
            return result.Any() ? result.First() : null;
        }

        private async Task<List<PositionDataPair>> _getExistingPairsFromItemsAsync(List<uint> itemIds)
        {
            var queryForConnection = $@"
                SELECT {_itemName} AS ItemId, 
                       {_nextName} AS NextItemId, 
                       {_tableName}_id AS PositionInStreamId
                FROM {_tableName}
                    JOIN {_tableName}_owner USING ({_tableName}_id)
                WHERE {_itemName} in @ItemIds
                    AND edition_id=@EditionId"
                ;
            var parameters = new DynamicParameters();
            parameters.Add("@ItemIds", itemIds);
            parameters.Add("@EditionId", _editionId);

            var result = await _dbConnection.QueryAsync<PositionDataPair>(queryForConnection, parameters);
            return result == null ? new List<PositionDataPair>() : result.ToList();
        }

        private async Task<List<PositionDataPair>> _getExistingPairsFromNextItemsAsync(List<uint> nextItemIds)
        {
            var queryForConnection = $@"
                SELECT {_itemName} AS ItemId, 
                       {_nextName} AS NextItemId, 
                       {_tableName}_id AS PositionInStreamId
                FROM {_tableName}
                    JOIN {_tableName}_owner USING ({_tableName}_id)
                WHERE {_nextName} in @NextItemIds
                    AND edition_id=@EditionId"
                ;
            var parameters = new DynamicParameters();
            parameters.Add("@NextItemIds", nextItemIds);
            parameters.Add("@EditionId", _editionId);

            var result = await _dbConnection.QueryAsync<PositionDataPair>(queryForConnection, parameters);
            return result == null ? new List<PositionDataPair>() : result.ToList();
        }

        private DynamicParameters _createParameters(uint itemId, uint nextItemId)
        {
            var parameters = new DynamicParameters();
            parameters.Add(_itemNameAt, itemId);
            parameters.Add(_nextNameAt, nextItemId);
            return parameters;
        }

        private async Task<List<uint>> _getAnchorsBeforeAsync()
        {
            var queryForConnection = $@"
                SELECT {_itemName}
                FROM {_tableName}
                    JOIN {_tableName}_owner USING ({_tableName}_id)
                WHERE  {_nextName} = @ItemId
                    AND edition_id=@EditionId"
                ;
            var parameters = new DynamicParameters();
            parameters.Add("@ItemId", _itemIds.First());
            parameters.Add("@EditionId", _editionId);

            var result = await _dbConnection.QueryAsync<uint>(queryForConnection, parameters);
            return result == null ? new List<uint>() : result.ToList();
        }

        private async Task<List<uint>> _getAnchorsAfterAsync()
        {
            var queryForConnection = $@"
                SELECT {_nextName}
                FROM {_tableName}
                    JOIN {_tableName}_owner USING ({_tableName}_id)
                WHERE  {_itemName} = @ItemId
                    AND edition_id=@EditionId"
                ;
            var parameters = new DynamicParameters();
            parameters.Add("@ItemId", _itemIds.Last(), DbType.AnsiString);
            parameters.Add("@EditionId", _editionId);

            var result = await _dbConnection.QueryAsync<uint>(queryForConnection, parameters);
            return result == null ? new List<uint>() : result.ToList();
        }
    }
}