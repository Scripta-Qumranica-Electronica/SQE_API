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
                    new { editionUser.EditionId, UserId = editionUser.userId }
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
                        // Get all text fragments in the edition for sorting operations later on
                        var editionTextFragments = await GetFragmentDataAsync(editionUser);

                        // Check to make sure the new named text fragment doesn't conflict with existing ones (the frontend will resolve this)
                        if (editionTextFragments.Any(x => x.TextFragmentName == fragmentName))
                            throw new StandardExceptions.ConflictingDataException("textFragmentName");

                        // Determine the desired position of the new text fragment
                        var newTextFragmentPosition =
                            _getNewTextFragmentPosition(previousFragmentId, nextFragmentId, editionTextFragments);

                        // Create the new text fragment abstract id
                        var newTextFragmentId = await _createTextFragmentIdAsync();

                        // Add the new text fragment to the edition manuscript
                        await _addTextFragmentToManuscript(editionUser, newTextFragmentId);

                        // Create the data entry for the new text fragment
                        await _createTextFragmentDataAsync(editionUser, newTextFragmentId, fragmentName);

                        // Shift the position of any text fragments that have been displaced by the new one
                        await _shiftTextFragmentsPosition(
                            editionUser,
                            editionTextFragments,
                            newTextFragmentPosition,
                            1
                        );

                        // Now set the position for the new text fragment
                        await _createTextFragmentPosition(editionUser, newTextFragmentId, newTextFragmentPosition);

                        // End the transaction (it was all or nothing)
                        transactionScope.Complete();

                        // Package the new text fragment to return to user
                        return new TextFragmentData
                        {
                            TextFragmentId = newTextFragmentId,
                            TextFragmentName = fragmentName,
                            Position = newTextFragmentPosition,
                            TextFragmentSequenceId = 0
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

                        var newScroll = manuscript.manuscriptId != lastEdition?.manuscriptId;

                        if (newScroll) lastEdition = manuscript;

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

                        lastChar.attributes.Add(charAttribute);

                        if (roi != null
                            && roi.SignInterpretationRoiId != lastInterpretationRoi?.SignInterpretationRoiId)
                        {
                            lastInterpretationRoi = roi;
                            lastChar.signInterpretationRois.Add(roi);
                        }

                        return newScroll ? manuscript : null;
                    },
                    new { startId, endId, editionUser.EditionId },
                    splitOn:
                    "textFragmentId, lineId, signId, nextSignInterpretationId, signInterpretationId, interpretationAttributeId, SignInterpretationRoiId"
                );
                var formattedEdition = scrolls.AsList()[0];
                formattedEdition.AddLicence();
                return formattedEdition;
            }
        }

        #region Text Fragment

        private static ushort _getNewTextFragmentPosition(uint? previousFragmentId,
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
            uint textFragmentId,
            ushort position)
        {
            // Set the parameters for the mutation object
            var fragmentPositionParameters = new DynamicParameters();
            fragmentPositionParameters.Add("@text_fragment_id", textFragmentId);
            fragmentPositionParameters.Add("@position", position);

            // Create the mutation object
            var createTextFragmentPositionMutation = new MutationRequest(
                MutateType.Create,
                fragmentPositionParameters,
                "text_fragment_sequence"
            );

            // Commit the mutation
            var textFragmentMutationResults =
                await _databaseWriter.WriteToDatabaseAsync(
                    editionUser,
                    new List<MutationRequest> { createTextFragmentPositionMutation }
                );

            // Ensure that the entry was created
            if (textFragmentMutationResults.Count != 1
                || !textFragmentMutationResults.First().NewId.HasValue)
                throw new StandardExceptions.DataNotWrittenException(
                    "create text fragment position"
                );

            return textFragmentMutationResults.First().NewId.Value;
        }

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