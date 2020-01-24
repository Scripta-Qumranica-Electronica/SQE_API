using System.Collections.Generic;
using System.Linq;
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

        Task<TextFragmentData> UpdateTextFragmentAsync(EditionUserInfo editionUser,
            uint textFragmentId,
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

        /// <summary>
        ///     Gets the text of a line in an edition
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="lineId">Line id</param>
        /// <returns>A detailed text object</returns>
        public async Task<TextEdition> GetLineByIdAsync(EditionUserInfo editionUser, uint lineId)
        {
            var terminators = _getTerminators(editionUser, GetLineTerminators.GetQuery, lineId);

            if (terminators.Length != 2)
                return new TextEdition();

            return await _getEntityById(editionUser, terminators[0], terminators[1]);
        }

        /// <summary>
        ///     Gets the text of a text fragment in an edition
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="textFragmentId">Text fragment id</param>
        /// <returns>A detailed text object</returns>
        public async Task<TextEdition> GetTextFragmentByIdAsync(EditionUserInfo editionUser, uint textFragmentId)
        {
            var terminators = _getTerminators(editionUser, GetFragmentTerminators.GetQuery, textFragmentId);

            if (terminators.Length != 2)
                return new TextEdition();

            return await _getEntityById(editionUser, terminators[0], terminators[1]);
        }

        /// <summary>
        ///     Gets a list of all artefacts with ROI's linked to text in the text fragment
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="textFragmentId">Text fragment id</param>
        /// <returns>A list of artefacts</returns>
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

        /// <summary>
        ///     Get a list of all lines in a text fragment.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="textFragmentId">Text fragment id</param>
        /// <returns>A list of lines in the text fragment</returns>
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

        /// <summary>
        ///     Get a list of all the text fragments in an edition
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <returns>A list of all text fragments in the edition</returns>
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

        /// <summary>
        ///     Creates a new text fragment in an edition. If previousFragmentId or nextFragmentId are null, the missing
        ///     value will be automatically calculated. If both are null, then the new text fragment is added to the end
        ///     of the list of text fragments.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="fragmentName">Name of the new fragment</param>
        /// <param name="previousFragmentId">
        ///     Id of the text fragment that should directly precede the new text fragment,
        ///     may be null
        /// </param>
        /// <param name="nextFragmentId">
        ///     Id of the text fragment that should directly follow the new text fragment,
        ///     may be null
        /// </param>
        /// <returns></returns>
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
                        // Create the new text fragment abstract id
                        var newTextFragmentId = await _createTextFragmentIdAsync();

                        // Add the new text fragment to the edition manuscript
                        await _addTextFragmentToManuscript(editionUser, newTextFragmentId);

                        // Create the data entry for the new text fragment
                        await _setTextFragmentDataAsync(editionUser, newTextFragmentId, fragmentName);

                        // Now set the position for the new text fragment
                        (previousFragmentId, nextFragmentId) = await _createTextFragmentPosition(
                            editionUser,
                            previousFragmentId,
                            newTextFragmentId,
                            nextFragmentId
                        );

                        // End the transaction (it was all or nothing)
                        transactionScope.Complete();

                        // Package the new text fragment to return to user
                        return new TextFragmentData
                        {
                            TextFragmentId = newTextFragmentId,
                            TextFragmentName = fragmentName,
                            PreviousTextFragmentId = previousFragmentId,
                            NextTextFragmentId = nextFragmentId,
                            EditionEditorId = editionUser.EditionEditorId.Value
                        };
                    }
                }
            );
        }

        /// <summary>
        ///     Updates the details of a text fragment. If previousFragmentId or nextFragmentId are null, the missing
        ///     value will be automatically calculated. If both are null, the text fragment will not be moved.
        ///     If the fragmentName is null or "", the name will not be altered.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="textFragmentId">id of the text fragment to change</param>
        /// <param name="fragmentName">New name for the text fragment, may be null or ""</param>
        /// <param name="previousFragmentId">
        ///     Id of the text fragment that should precede the updated text fragment,
        ///     may be null
        /// </param>
        /// <param name="nextFragmentId">
        ///     Id of the text fragment that should follow the updated text fragment,
        ///     may be null
        /// </param>
        /// <returns>Details of the updated text fragment</returns>
        public async Task<TextFragmentData> UpdateTextFragmentAsync(EditionUserInfo editionUser,
            uint textFragmentId,
            string fragmentName,
            uint? previousFragmentId,
            uint? nextFragmentId)
        {
            using (var transactionScope = new TransactionScope())
            {
                // Write the new name if it exists
                if (!string.IsNullOrEmpty(fragmentName))
                    await _setTextFragmentDataAsync(editionUser, textFragmentId, fragmentName, false);
                else // Get the current name
                    using (var connection = OpenConnection())
                    {
                        fragmentName = await connection.QuerySingleAsync<string>(
                            GetFragmentNameById.GetQuery,
                            new { editionUser.EditionId, UserId = editionUser.userId, TextFragmentId = textFragmentId }
                        );
                    }

                // Set the new position if it exists
                if (previousFragmentId.HasValue
                    || nextFragmentId.HasValue)
                    (previousFragmentId, nextFragmentId) = await _moveTextFragments(
                        editionUser,
                        textFragmentId,
                        previousFragmentId,
                        nextFragmentId
                    );

                // End the transaction (it was all or nothing)
                transactionScope.Complete();

                // Package the new text fragment to return to user
                return new TextFragmentData
                {
                    TextFragmentId = textFragmentId,
                    TextFragmentName = fragmentName,
                    PreviousTextFragmentId = previousFragmentId,
                    NextTextFragmentId = nextFragmentId,
                    EditionEditorId = editionUser.EditionEditorId.Value
                };
            }
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

        /// <summary>
        ///     Created a new text fragment id in the system
        /// </summary>
        /// <returns>Id of the newly created text fragment</returns>
        /// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
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

        /// <summary>
        ///     Set the name of a text fragment
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="textFragmentId">Id of the text fragment to set</param>
        /// <param name="textFragmentName">Name to be set</param>
        /// <param name="create">
        ///     Boolean whether a new text fragment should be created for this name. Set to
        ///     false if you are updating existing data.
        /// </param>
        /// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
        private async Task _setTextFragmentDataAsync(EditionUserInfo editionUser,
            uint textFragmentId,
            string textFragmentName,
            bool create = true)
        {
            // Set the parameters for the mutation object
            var createTextFragmentParameters = new DynamicParameters();
            createTextFragmentParameters.Add("@name", textFragmentName);
            createTextFragmentParameters.Add("@text_fragment_id", textFragmentId);

            // Create the mutation object
            var createTextFragmentMutation = new MutationRequest(
                create ? MutateType.Create : MutateType.Update,
                createTextFragmentParameters,
                "text_fragment_data",
                create ? null : (uint?)await _getTextFragmentDataId(editionUser, textFragmentId)
            );

            // Commit the mutation
            var createTextFragmentResponse = await _databaseWriter.WriteToDatabaseAsync(
                editionUser,
                new List<MutationRequest> { createTextFragmentMutation }
            );

            // Ensure that the entry was created
            if (createTextFragmentResponse.Count != 1
                || create && !createTextFragmentResponse.First().NewId.HasValue)
                throw new StandardExceptions.DataNotWrittenException("create new textFragment data");
        }

        /// <summary>
        ///     Set the position of a newly created text fragment, if anchorBefore or anchorAfter are null, they
        ///     will be automatically created.  If both are null, then the fragment is positioned at the end of
        ///     the list of text fragments.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="anchorBefore">Id of the directly preceding text fragment, may be null</param>
        /// <param name="textFragmentId">Id of the text fragment for which a position is being created</param>
        /// <param name="anchorAfter">Id of the directly following text fragment, may be null</param>
        /// <returns>The id of the preceding and following text fragments</returns>
        /// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
        private async Task<(uint? previousTextFragmentId, uint? nextTextFragmentId)> _createTextFragmentPosition(
            EditionUserInfo editionUser,
            uint? anchorBefore,
            uint textFragmentId,
            uint? anchorAfter)
        {
            // Prepare the response object
            PositionDataRequestFactory positionDataRequestFactory;
            (positionDataRequestFactory, anchorBefore, anchorAfter) = await _createTextFragmentPositionRequestFactory(
                editionUser,
                anchorBefore,
                textFragmentId,
                anchorAfter
            );

            positionDataRequestFactory.AddAction(PositionAction.Break);
            positionDataRequestFactory.AddAction(PositionAction.Add);
            var requests = await positionDataRequestFactory.CreateRequestsAsync();

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

            return (anchorBefore, anchorAfter);
        }

        /// <summary>
        ///     Updates the position of a text fragment, if anchorBefore or anchorAfter are null, they
        ///     will be automatically created.  If both are null, then an error is thrown.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="newAnchorBefore">Id of the directly preceding text fragment, may be null</param>
        /// <param name="textFragmentIds">Id of the text fragments for which a position is being created</param>
        /// <param name="newAnchorAfter">Id of the directly following text fragment, may be null</param>
        /// <returns>The id of the preceding and following text fragments</returns>
        /// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
        private async Task<(uint? previousTextFragmentId, uint? nextTextFragmentId)> _moveTextFragments(
            EditionUserInfo editionUser,
            List<uint> textFragmentIds,
            uint? newAnchorBefore,
            uint? newAnchorAfter)
        {
            if (!newAnchorBefore.HasValue
                && !newAnchorAfter.HasValue)
                throw new StandardExceptions.InputDataRuleViolationException(
                    "must provide either a previous or next text fragment id"
                );

            PositionDataRequestFactory positionDataRequestFactory;
            (positionDataRequestFactory, newAnchorBefore, newAnchorAfter) =
                await _createTextFragmentPositionRequestFactory(
                    editionUser,
                    newAnchorBefore,
                    textFragmentIds,
                    newAnchorAfter
                );
            positionDataRequestFactory.AddAction(PositionAction.MoveTo);
            var requests = await positionDataRequestFactory.CreateRequestsAsync();
            var shiftTextFragmentMutationResults =
                await _databaseWriter.WriteToDatabaseAsync(editionUser, requests);

            // Ensure that the entry was created
            if (shiftTextFragmentMutationResults.Count != requests.Count)
                throw new StandardExceptions.DataNotWrittenException(
                    "shift text fragment positions"
                );
            return (newAnchorBefore, newAnchorAfter);
        }


        /// <summary>
        ///     Updates the position of a text fragment, if anchorBefore or anchorAfter are null, they
        ///     will be automatically created.  If both are null, then an error is thrown.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="anchorBefore">Id of the directly preceding text fragment, may be null</param>
        /// <param name="textFragmentId">Id of the text fragment for which a position is being created</param>
        /// <param name="anchorAfter">Id of the directly following text fragment, may be null</param>
        /// <returns>The id of the preceding and following text fragments</returns>
        /// <exception cref="DataNotWrittenException"></exception>
        private async Task<(uint? previousTextFragmentId, uint? nextTextFragmentId)> _moveTextFragments(
            EditionUserInfo editionUser,
            uint textFragmentId,
            uint? newAnchorBefore,
            uint? newAnchorAfter)
        {
            return await _moveTextFragments(
                editionUser,
                new List<uint> { textFragmentId },
                newAnchorBefore,
                newAnchorAfter
            );
        }

        /// <summary>
        ///     Create a PositionDataRequestFactory from the submitted data. If anchorBefore or anchorAfter are null,
        ///     the missing data will be automatically calculated. If both are null, the submitted text fragments
        ///     will be positioned at the end of the list of text fragments for the edition.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="anchorBefore">Id of the text fragment preceding the text fragments being positioned, may be null</param>
        /// <param name="textFragmentIds">Text fragments to be positioned</param>
        /// <param name="anchorAfter">Id of the text fragment following the text fragments being positioned, may be null</param>
        /// <returns>A PositionDataRequestFactory along with the ids of the previous and next text fragments</returns>
        private async Task<(PositionDataRequestFactory positionDataRequestFactory, uint? previousTextFragmentId, uint?
                nextTextFragmentId)>
            _createTextFragmentPositionRequestFactory(EditionUserInfo editionUser,
                uint? anchorBefore,
                List<uint> textFragmentIds,
                uint? anchorAfter)
        {
            // Prepare the response object
            PositionDataRequestFactory positionDataRequestFactory;
            using (var connection = OpenConnection())
            {
                // Verify that anchorBefore and anchorAfter are valid values if they exist
                var fragments = await GetFragmentDataAsync(editionUser);
                await _verifyTextFragmentsSequence(fragments, anchorBefore, anchorAfter);

                // Set the current text fragment position factory
                positionDataRequestFactory = await PositionDataRequestFactory.CreateInstanceAsync(
                    connection,
                    StreamType.TextFragmentStream,
                    textFragmentIds,
                    editionUser.EditionId
                );

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
                            true
                        );
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
                        true
                    );
                    var after = tempFac.getAnchorsAfter(); // Get the text fragment(s) directly after it
                    if (after.Any())
                        anchorAfter = after.First(); // We will work with a non-branching stream for now
                }

                // Add the after anchor for the new text fragment
                if (anchorAfter.HasValue)
                    positionDataRequestFactory.AddAnchorAfter(anchorAfter.Value);
            }

            return (positionDataRequestFactory, anchorBefore, anchorAfter);
        }

        /// <summary>
        ///     Create a PositionDataRequestFactory from the submitted data. If anchorBefore or anchorAfter are null,
        ///     the missing data will be automatically calculated. If both are null, the submitted text fragments
        ///     will be positioned at the end of the list of text fragments for the edition.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="anchorBefore">Id of the text fragment preceding the text fragments being positioned, may be null</param>
        /// <param name="textFragmentId">Text fragment to be positioned</param>
        /// <param name="anchorAfter">Id of the text fragment following the text fragments being positioned, may be null</param>
        /// <returns>A PositionDataRequestFactory along with the ids of the previous and next text fragments</returns>
        private async Task<(PositionDataRequestFactory positionDataRequestFactory, uint? previousTextFragmentId, uint?
                nextTextFragmentId)>
            _createTextFragmentPositionRequestFactory(EditionUserInfo editionUser,
                uint? anchorBefore,
                uint textFragmentId,
                uint? anchorAfter)
        {
            return await _createTextFragmentPositionRequestFactory(
                editionUser,
                anchorBefore,
                new List<uint> { textFragmentId },
                anchorAfter
            );
        }

        /// <summary>
        ///     Ensures that anchorBefore and anchorAfter are either null or part of the current edition. If
        ///     both exist, this verifies that they are indeed sequential.
        /// </summary>
        /// <param name="fragments">List of all fragments in the edition</param>
        /// <param name="anchorBefore">Id of the first text fragment</param>
        /// <param name="anchorAfter">Id of the second text fragment</param>
        /// <returns></returns>
        /// <exception cref="InputDataRuleViolationException"></exception>
        /// <exception cref="ImproperInputDataException"></exception>
        private async Task _verifyTextFragmentsSequence(
            List<TextFragmentData> fragments,
            uint? anchorBefore,
            uint? anchorAfter)
        {
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
                    if (anchorBefore.HasValue
                        && (!anchorBeforeIdx.HasValue || anchorBeforeIdx.Value + 1 != idx))
                        throw new StandardExceptions.InputDataRuleViolationException(
                            "the previous and next text fragment ids must be sequential"
                        );
                }
            }

            if (anchorBefore.HasValue
                && !anchorBeforeExists)
                throw new StandardExceptions.ImproperInputDataException("previous text fragment id");
            if (anchorAfter.HasValue
                && !anchorAfterExists)
                throw new StandardExceptions.ImproperInputDataException("next text fragment id");
        }

        /// <summary>
        ///     Adds a text fragment to an edition.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="textFragmentId">Id of the text fragment to be added</param>
        /// <returns></returns>
        /// <exception cref="DataNotWrittenException"></exception>
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

        /// <summary>
        ///     Gets the text fragment data id for a text fragment id
        /// </summary>
        /// <param name="user">Edition user object</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>Text fragment data id of the text fragment</returns>
        private async Task<uint> _getTextFragmentDataId(EditionUserInfo user, uint textFragmentId)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QuerySingleAsync<uint>(
                    GetTextFragmentDataId.GetQuery,
                    new
                    {
                        user.EditionId,
                        TextFragmentId = textFragmentId
                    }
                );
            }
        }

        #endregion Text Fragment

        #endregion Private methods
    }
}