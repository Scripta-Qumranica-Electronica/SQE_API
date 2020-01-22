using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess
{
    public interface ITextRepository
    {
        Task<TextEdition> GetLineByIdAsync(EditionUserInfo editionUser, uint lineId);
        Task<TextEdition> GetTextFragmentByIdAsync(EditionUserInfo editionUser, uint textFragmentId);
        Task<List<ArtefactDataModel>> GetArtefactsAsync(EditionUserInfo editionUser, uint textFragmentId);
        Task<List<LineData>> GetLineIdsAsync(EditionUserInfo editionUser, uint textFragmentId);
        Task<List<TextFragmentData>> GetFragmentDataAsync(EditionUserInfo editionUser);

        Task<TextFragmentData> CreateTextFragmentAsync(EditionUserInfo editionUser,
            string fragmentName,
            uint? previousFragmentId,
            uint? nextFragmentId);
    }

    public class TextRepository : DbConnectionBase, ITextRepository
    {
        private readonly IDatabaseWriter _databaseWriter;

        public TextRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
        {
            _databaseWriter = databaseWriter;
        }

        public async Task<TextEdition> GetLineByIdAsync(EditionUserInfo editionUser, uint lineId)
        {
            var terminators = _getTerminators(editionUser, GetLineTerminators.GetQuery, lineId);

            if (terminators.Length != 2)
                return new TextEdition();

            return await _getEntityById(editionUser, terminators[0], terminators[1]);
        }

        public async Task<TextEdition> GetTextFragmentByIdAsync(EditionUserInfo editionUser, uint textFragmentId)
        {
            var terminators = _getTerminators(editionUser, GetFragmentTerminators.GetQuery, textFragmentId);

            if (terminators.Length != 2)
                return new TextEdition();

            return await _getEntityById(editionUser, terminators[0], terminators[1]);
        }

        public async Task<List<ArtefactDataModel>> GetArtefactsAsync(EditionUserInfo editionUser, uint textFragmentId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<ArtefactDataModel>(
                    GetTextFragmentArtefacts.Query,
                    new { TextFragmentId = textFragmentId, editionUser.EditionId, UserId = editionUser.userId }
                )).ToList();
            }

            ;
        }

        public async Task<List<LineData>> GetLineIdsAsync(EditionUserInfo editionUser, uint textFragmentId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<LineData>(
                    GetLineData.Query,
                    new { TextFragmentId = textFragmentId, editionUser.EditionId, UserId = editionUser.userId }
                )).ToList();
            }
        }

        public async Task<List<TextFragmentData>> GetFragmentDataAsync(EditionUserInfo editionUser)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<TextFragmentData>(
                    GetFragmentData.GetQuery,
                    new { editionUser.EditionId }
                )).ToList();
            }
        }

        public async Task<TextFragmentData> CreateTextFragmentAsync(EditionUserInfo editionUser,
            string fragmentName,
            uint? previousFragmentId,
            uint? nextFragmentId)
        {
            return await DatabaseCommunicationRetryPolicy.ExecuteRetry(
                async () =>
                {
                    using (var transactionScope = new TransactionScope())
                    {

                        // Check to make sure the new named text fragment doesn't conflict with existing ones (the frontend will resolve this)
                        // TODO We don't need this - in fact, there can be different text-fragments with the same name
                        // Thus delete and also _textFragmentNameExistAsync
                        // if (await _textFragmentNameExistAsync(editionUser, fragmentName))
                        //     throw new StandardExceptions.ConflictingDataException("textFragmentName");

                        // Create the new text fragment abstract id
                        var newTextFragmentId = await _createTextFragmentIdAsync();

                        // Add the new text fragment to the edition manuscript
                        await _addTextFragmentToManuscript(editionUser, newTextFragmentId);

                        // Create the data entry for the new text fragment
                        await _createTextFragmentDataAsync(editionUser, newTextFragmentId, fragmentName);

                        // Now set the position for the new text fragment

                        await _createTextFragmentPosition(
                            editionUser,
                            previousFragmentId,
                            newTextFragmentId,
                            nextFragmentId);

                        // End the transaction (it was all or nothing)
                        transactionScope.Complete();

                        // Package the new text fragment to return to user
                        return new TextFragmentData
                        {
                            TextFragmentId = newTextFragmentId,
                            TextFragmentName = fragmentName
                        };
                    }
                }
            );
        }

        #region Private methods

        private uint[] _getTerminators(EditionUserInfo editionUser, string query, uint entityId)
        {
            uint[] terminators;
            using (var connection = OpenConnection())
            {
                terminators = connection.Query<uint>(
                        query,
                        new { EntityId = entityId, editionUser.EditionId, UserId = editionUser.userId }
                    )
                    .ToArray();
                connection.Close();
            }

            return terminators;
        }

        private async Task<TextEdition> _getEntityById(EditionUserInfo editionUser, uint startId, uint endId)
        {
            TextEdition lastEdition = null;
            TextFragment lastTextFragment = null;
            Line lastLine = null;
            Sign lastSign = null;
            NextSignInterpretation lastNextSignInterpretation = null;
            SignInterpretation lastChar = null;
            SignInterpretationROI lastInterpretationRoi = null;


            using (var connection = OpenConnection())
            {
                var attributeDict = (await connection.QueryAsync<AttributeDefinition>(
                    TextFragmentAttributes.GetQuery,
                    new { editionUser.EditionId }
                )).ToDictionary(
                    row => row.attributeValueId,
                    row => row.attributeString
                );
                var scrolls = await connection.QueryAsync(
                    GetTextChunk.GetQuery,
                    new[]
                    {
                        typeof(TextEdition), typeof(TextFragment), typeof(Line), typeof(Sign),
                        typeof(NextSignInterpretation), typeof(SignInterpretation), typeof(CharAttribute),
                        typeof(SignInterpretationROI)
                    },
                    objects =>
                    {
                        var manuscript = objects[0] as TextEdition;
                        var fragment = objects[1] as TextFragment;
                        var line = objects[2] as Line;
                        var sign = objects[3] as Sign;
                        var nextSignInterpretation = objects[4] as NextSignInterpretation;
                        var signInterpretation = objects[5] as SignInterpretation;
                        var charAttribute = objects[6] as CharAttribute;
                        var roi = objects[7] as SignInterpretationROI;

                        var newManuscript = manuscript.manuscriptId != lastEdition?.manuscriptId;

                        if (newManuscript) lastEdition = manuscript;

                        if (fragment.textFragmentId != lastTextFragment?.textFragmentId)

                            lastEdition = manuscript.manuscriptId == lastEdition?.manuscriptId
                                ? lastEdition
                                : manuscript;
                        if (fragment.textFragmentId != lastTextFragment?.textFragmentId)
                        {
                            lastTextFragment = fragment;
                            lastEdition.fragments.Add(fragment);
                        }

                        if (line.lineId != lastLine?.lineId)
                        {
                            lastLine = line;
                            lastTextFragment.lines.Add(line);
                        }

                        if (sign.signId != lastSign?.signId)
                        {
                            lastSign = sign;
                            lastLine.signs.Add(sign);
                        }

                        if (nextSignInterpretation.nextSignInterpretationId
                            != lastNextSignInterpretation?.nextSignInterpretationId)
                            lastNextSignInterpretation = nextSignInterpretation;

                        if (signInterpretation.signInterpretationId != lastChar?.signInterpretationId)
                        {
                            lastChar = signInterpretation;
                            lastSign.signInterpretations.Add(signInterpretation);
                        }

                        lastChar.nextSignInterpretations.Add(nextSignInterpretation);

                        charAttribute.attributeString = attributeDict.TryGetValue(
                            charAttribute.attributeValueId,
                            out var val
                        )
                            ? val
                            : null;

                        lastChar.attributes.Add(charAttribute);

                        if (roi != null
                            && roi.SignInterpretationRoiId != lastInterpretationRoi?.SignInterpretationRoiId)
                        {
                            lastInterpretationRoi = roi;
                            lastChar.signInterpretationRois.Add(roi);
                        }

                        return newManuscript ? manuscript : null;
                    },
                    new { StartId = startId, EndId = endId, editionUser.EditionId },
                    splitOn:
                    "textFragmentId, lineId, signId, nextSignInterpretationId, signInterpretationId, interpretationAttributeId, SignInterpretationRoiId"
                );
                var formattedEdition = scrolls.AsList()[0];
                formattedEdition.AddLicence();
                return formattedEdition;
            }
        }

        #region Text Fragment

        // TODO Delete? cf. above (Ingo)
        /*
        private async Task<bool> _textFragmentNameExistAsync(EditionUserInfo editionUser,
            string textFragmentName)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<TextFragmentData>(GetTextFragmentByName.GetQuery)).Any();
            }
            
        }
        */

        // TODO Not needed anymoro (Ingo)
        /*private static ushort _getNewTextFragmentPosition(uint? previousFragmentId,
            uint? nextFragmentId,
            List<TextFragmentData> textFragmentIds)
        {
            // If neither previousFragmentId nor nextFragmentId have been set, put the new text fragment at the end of the manuscript.
            if (!previousFragmentId.HasValue
                && !nextFragmentId.HasValue)
                return (ushort)(textFragmentIds.Any() ? textFragmentIds.Last().Position + 1 : 1);

            ushort? nextPosition = null;
            if (nextFragmentId.HasValue) // We know the existing text fragment that the new one will displace
            {
                // Verify that the nextFragmentId exists and take its position as the position for the new text fragment
                var nextTextFragment = textFragmentIds.Where(x => x.TextFragmentId == nextFragmentId);

                if (nextTextFragment.Count() != 1) // The specified next text fragment does not exist in the edition
                    throw new StandardExceptions.ImproperInputDataException("textFragmentId");

                nextPosition = nextTextFragment.First().Position;

                if (!previousFragmentId.HasValue) // If no previous fragment ID was given, then return now.
                    return nextPosition.Value;
            }

            // Make sure the previousFragmentId exists in the edition
            var previousTextFragment = textFragmentIds.Where(x => x.TextFragmentId == previousFragmentId).ToList();

            if (previousTextFragment.Count != 1) // The specified previous text fragment does not exist in the edition
                throw new StandardExceptions.ImproperInputDataException("textFragmentId");
            var previousPosition = previousTextFragment.First().Position;

            // If there is also a nextPosition, verify that previousPosition and nextPosition are sequential
            if (nextPosition.HasValue
                && previousPosition + 1 != nextPosition
            ) // The specified previous and next text fragments are not sequential
                throw new StandardExceptions.ImproperInputDataException("textFragmentId");

            // Since there is no nextPosition just assume it should be one higher than the previousFragmentId
            return (ushort)(previousPosition + 1);
        }
        */

        private async Task<uint> _createTextFragmentIdAsync()
        {
            using (var connection = OpenConnection())
            {
                // Create the new text fragment id
                var createNewTextFragmentId = await connection.ExecuteAsync(CreateTextFragment.GetQuery);
                if (createNewTextFragmentId == 0)
                    throw new StandardExceptions.DataNotWrittenException("create new textFragment");

                // Get the new text fragmentid
                var getNewTextFragmentId = (await connection.QueryAsync<uint>(LastInsertId.GetQuery)).ToList();
                if (getNewTextFragmentId.Count != 1)
                    throw new StandardExceptions.DataNotWrittenException("create new textFragment");

                return getNewTextFragmentId.First();
            }
        }

        private async Task<uint> _createTextFragmentDataAsync(EditionUserInfo editionUser,
            uint textFragmentId,
            string textFragmentName)
        {
            // Set the parameters for the mutation object
            var createTextFragmentParameters = new DynamicParameters();
            createTextFragmentParameters.Add("@name", textFragmentName);
            createTextFragmentParameters.Add("@text_fragment_id", textFragmentId);

            // Create the mutation object
            var createTextFragmentMutation = new MutationRequest(
                MutateType.Create,
                createTextFragmentParameters,
                "text_fragment_data"
            );

            // Commit the mutation
            var createTextFragmentResponse = await _databaseWriter.WriteToDatabaseAsync(
                editionUser,
                new List<MutationRequest> { createTextFragmentMutation }
            );

            // Ensure that the entry was created
            if (createTextFragmentResponse.Count != 1
                || !createTextFragmentResponse.First().NewId.HasValue)
                throw new StandardExceptions.DataNotWrittenException("create new textFragment data");

            return createTextFragmentResponse.First().NewId.Value;
        }

        private async Task<uint> _createTextFragmentPosition(EditionUserInfo editionUser,
            uint? anchorBefore,
            uint textFragmentId,
            uint? anchorAfter)
        {
            // Verify that anchorBefore and anchorAfter are valid values if they exist
            var fragments = await GetFragmentDataAsync(editionUser);
            var anchorBeforeExists = false;
            var anchorAfterExists = false;
            int? anchorBeforeIdx = null;
            foreach (var (fragment, idx) in fragments.Select((v, i) => (v, i)))
            {
                if (fragment.TextFragmentId == anchorBefore)
                {
                    anchorBeforeExists = true;
                    anchorBeforeIdx = idx;
                }
                if (fragment.TextFragmentId == anchorAfter)
                {
                    anchorAfterExists = true;
                    // Check for correct sequence of anchors if applicable
                    if (anchorBefore.HasValue && (!anchorBeforeIdx.HasValue || anchorBeforeIdx.Value + 1 != idx))
                        throw new StandardExceptions.InputDataRuleViolationException("the previous and next text fragment ids must be sequential");
                }
            }
            if (anchorBefore.HasValue && !anchorBeforeExists)
                throw new StandardExceptions.ImproperInputDataException("previous text fragment id");
            if (anchorAfter.HasValue && !anchorAfterExists)
                throw new StandardExceptions.ImproperInputDataException("next text fragment id");

            // Prepare the response object
            List<MutationRequest> requests;
            using (var connection = OpenConnection())
            {
                // Set the current text fragment position factory
                var positionDataRequestFactory = await PositionDataRequestFactory.CreateInstanceAsync(
                    connection,
                    StreamType.TextFragmentStream,
                    textFragmentId,
                    editionUser.EditionId);
                positionDataRequestFactory.AddAction(PositionAction.Break);
                positionDataRequestFactory.AddAction(PositionAction.Add);

                // Determine the anchorBefore if none was provided
                if (!anchorBefore.HasValue)
                {
                    // If no before or after text fragment id were provided, add the new text fragment after the last
                    // text fragment in the edition (append it).
                    if (!anchorAfter.HasValue)
                    {
                        if (fragments.Any())
                            anchorBefore = fragments.Last().TextFragmentId;
                    }
                    // Otherwise, find the text fragment before anchorAfter, since the new text fragment will be
                    // inserted between these two
                    else
                    {
                        // Use the position data factory with the anchorAfter text fragment
                        var tempFac = await PositionDataRequestFactory.CreateInstanceAsync(
                            connection,
                            StreamType.TextFragmentStream,
                            anchorAfter.Value,
                            editionUser.EditionId,
                            true);
                        var before = tempFac.getAnchorsBefore(); // Get the text fragment(s) directly before it
                        if (before.Any())
                            anchorBefore = before.First(); // We will work with a non-branching stream for now
                    }
                }
                // Add the before anchor for the new text fragment
                if (anchorBefore.HasValue)
                    positionDataRequestFactory.AddAnchorBefore(anchorBefore.Value);

                // If no anchorAfter has been specified, set it to the text fragment following anchorBefore
                if (!anchorAfter.HasValue)
                {
                    // Use the position data factory with the anchorBefore text fragment
                    var tempFac = await PositionDataRequestFactory.CreateInstanceAsync(
                        connection,
                        StreamType.TextFragmentStream,
                        anchorBefore.Value,
                        editionUser.EditionId,
                        true);
                    var after = tempFac.getAnchorsAfter(); // Get the text fragment(s) directly after it
                    if (after.Any())
                        anchorAfter = after.First(); // We will work with a non-branching stream for now
                }

                // Add the after anchor for the new text fragment
                if (anchorAfter.HasValue)
                    positionDataRequestFactory.AddAnchorAfter(anchorAfter.Value);
                requests = await positionDataRequestFactory.CreateRequestsAsync();
            }

            // Commit the mutation
            var textFragmentMutationResults =
                await _databaseWriter.WriteToDatabaseAsync(
                    editionUser,
                    requests
                );

            // Ensure that the entry was created
            // Ingo: I changed First to Last, since now the first one normally is a delete-request
            // deleting the connection between the anchors.
            if (textFragmentMutationResults.Count != requests.Count
                || !textFragmentMutationResults.Last().NewId.HasValue)
                throw new StandardExceptions.DataNotWrittenException(
                    "create text fragment position"
                );

            return textFragmentMutationResults.Last().NewId.Value;
        }



        // TODO We have create a different functions using MoveTo action in PositionDataRequestFactory
        // I add some _move...-Functions
        /*
        private async Task _shiftTextFragmentsPosition(EditionUserInfo editionUser,
            IReadOnlyCollection<TextFragmentData> textFragmentList,
            ushort startPosition,
            int offset)
        {
            // Create the mutation objects
            var textFragmentShiftMutations = textFragmentList.Where(x => x.Position >= startPosition)
                .Select(
                    x =>
                    {
                        if (x.Position + offset < 0
                            || x.Position + offset > 65535)
                            throw new StandardExceptions.DataNotWrittenException(
                                "change textFragment position",
                                "the desired position is out of range"
                            );
                        var parameters = new DynamicParameters();
                        parameters.Add("@position", x.Position + offset);
                        parameters.Add("@text_fragment_id", x.TextFragmentId);
                        return new MutationRequest(
                            MutateType.Update,
                            parameters,
                            "text_fragment_sequence",
                            x.TextFragmentSequenceId
                        );
                    }
                )
                .ToList();

            // Commit the mutation
            var shiftTextFragmentMutationResults =
                await _databaseWriter.WriteToDatabaseAsync(editionUser, textFragmentShiftMutations);

            // Ensure that the entry was created
            if (shiftTextFragmentMutationResults.Count != textFragmentShiftMutations.Count)
                throw new StandardExceptions.DataNotWrittenException(
                    "shift text fragment positions"
                );
        }
        */

        /// <summary>
        /// Moves a text fragment(_path) to different place.
        /// If one sets newAnchorBefore or -After null and keeps retrieveMissingData false
        /// than the item-path will be added only after or before the anchor provided and leave the other end unconnected.
        /// If retrieveMissingData is true than for the missing anchors the corresponding items, the existing anchor
        /// is connected to are used and the item-path is inserted at this point as is the case if one provides both anchors.
        /// </summary>
        /// <param name="editionUser"></param>
        /// <param name="textFragmentList"></param>
        /// <param name="newAnchorBefore"></param>
        /// <param name="newAnchorAfter"></param>
        /// <param name="retrieveMissingData"></param>
        /// <returns></returns>
        /// <exception cref="DataNotWrittenException"></exception>
        private async Task _moveTextFragments(EditionUserInfo editionUser,
            IReadOnlyCollection<TextFragmentData> textFragmentList,
            TextFragmentData newAnchorBefore,
            TextFragmentData newAnchorAfter,
            bool retrieveMissingData = false)
        {
            var itemIds = textFragmentList.ToList().Select(i => i.TextFragmentId);
            PositionDataRequestFactory posDataFac;
            using (var connection = OpenConnection())
            {
                posDataFac = new PositionDataRequestFactory(
                    connection,
                    StreamType.TextFragmentStream,
                    itemIds.ToList(),
                    editionUser.EditionId
                );
                if (newAnchorBefore != null || newAnchorAfter != null)
                {
                    if (newAnchorBefore != null)
                        posDataFac.AddAnchorBefore(newAnchorBefore.TextFragmentId);
                    else if (retrieveMissingData && newAnchorAfter != null)
                    {
                        var temp = await PositionDataRequestFactory.CreateInstanceAsync(
                            connection,
                            StreamType.TextFragmentStream,
                            newAnchorAfter.TextFragmentId,
                            editionUser.EditionId,
                            true
                        );
                        posDataFac.AddAnchorsBefore(temp.getAnchorsBefore());
                    }

                    if (newAnchorAfter != null)
                        posDataFac.AddAnchorBefore(newAnchorAfter.TextFragmentId);
                    else if (retrieveMissingData && newAnchorBefore != null)
                    {
                        var temp = await PositionDataRequestFactory.CreateInstanceAsync(
                            connection,
                            StreamType.TextFragmentStream,
                            newAnchorBefore.TextFragmentId,
                            editionUser.EditionId,
                            true
                        );
                        posDataFac.AddAnchorsBefore(temp.getAnchorsAfter());
                    }
                }


                connection.Close();
            }
            posDataFac.AddAction(PositionAction.MoveTo);
            List<MutationRequest> requests = await posDataFac.CreateRequestsAsync();
            var shiftTextFragmentMutationResults =
                await _databaseWriter.WriteToDatabaseAsync(editionUser, requests);

            // Ensure that the entry was created
            if (shiftTextFragmentMutationResults.Count != requests.Count)
                throw new StandardExceptions.DataNotWrittenException(
                    "shift text fragment positions"
                );
        }


        private async Task _addTextFragmentToManuscript(EditionUserInfo editionUser, uint textFragmentId)
        {
            using (var connection = OpenConnection())
            {
                // Get the manuscript id of the current edition
                var manuscriptId = await connection.QueryAsync<uint>(
                    ManuscriptOfEdition.GetQuery,
                    new { editionUser.EditionId }
                );

                // Link the manuscript to the new text fragment
                var manuscriptToTextFragmentParameters = new DynamicParameters();
                manuscriptToTextFragmentParameters.Add("@manuscript_id", manuscriptId);
                manuscriptToTextFragmentParameters.Add("@text_fragment_id", textFragmentId);
                var manuscriptToTextFragmentResults =
                    await _databaseWriter.WriteToDatabaseAsync(
                        editionUser,
                        new List<MutationRequest>
                        {
                            new MutationRequest(
                                MutateType.Create,
                                manuscriptToTextFragmentParameters,
                                "manuscript_to_text_fragment"
                            )
                        }
                    );

                // Check for success
                if (manuscriptToTextFragmentResults.Count != 1)
                    throw new StandardExceptions.DataNotWrittenException("manuscript id to new text fragment link");
            }
        }




        #endregion Text Fragment

        #endregion Private methods
    }
}