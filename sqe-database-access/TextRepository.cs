using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;
using static SQE.DatabaseAccess.Helpers.SignFactory;

namespace SQE.DatabaseAccess
{
    public interface ITextRepository
    {
        #region Line
        Task<LineData> CreateLineAsync(EditionUserInfo editionUser,
            LineData lineData,
            uint fragmentId,
            uint anchorBefore = 0,
            uint anchorAfter = 0);
        Task<TextEdition> GetLineByIdAsync(EditionUserInfo editionUser, uint lineId);
        Task<List<LineData>> GetLineIdsAsync(EditionUserInfo editionUser, uint textFragmentId);
        Task<uint> RemoveLineAsync(EditionUserInfo editionUser, uint lineId);
        Task<LineData> UpdateLineAsync(EditionUserInfo editionUser,
            uint lineId,
            string lineName);
        #endregion

        #region Sign and its Interpretation

        Task<List<SignInterpretationData>> AddSignInterpretationsAsync(EditionUserInfo editionUser,
            uint? signId,
            List<SignInterpretationData> signInterpretations,
            List<uint> anchorsBefore,
            List<uint> anchorsAfter);


        Task<List<SignData>> CreateSignsAsync(EditionUserInfo editionUser,
            uint lineId,
            List<SignData> signs,
            List<uint> anchorsBefore,
            List<uint> anchorsAfter
        );

        Task<List<uint>> GetAllSignInterpretationIdsForSignIdAsync(EditionUserInfo editionUser, uint signId);

        Task<uint> RemoveSignInterpretationAsync(EditionUserInfo editionUser,
            uint signInterpretationId);

        Task<uint> RemoveSignAsync(EditionUserInfo editionUser, uint signId);




        #endregion

        #region Text fragment

        Task<TextFragmentData> CreateTextFragmentAsync(EditionUserInfo editionUser,
            TextFragmentData textFragmentData,
            uint? previousFragmentId,
            uint? nextFragmentId);
        Task<List<ArtefactDataModel>> GetArtefactsAsync(EditionUserInfo editionUser, uint textFragmentId);
        Task<TextEdition> GetTextFragmentByIdAsync(EditionUserInfo editionUser, uint textFragmentId);
        Task<List<TextFragmentData>> GetFragmentDataAsync(EditionUserInfo editionUser);
        Task<uint> RemoveTextFragmentAsync(EditionUserInfo editionUser, uint textFragmentId);
        Task<TextFragmentData> UpdateTextFragmentAsync(EditionUserInfo editionUser,
            uint textFragmentId,
            string fragmentName,
            uint? previousFragmentId,
            uint? nextFragmentId);

        #endregion
    }

    public class TextRepository : DbConnectionBase, ITextRepository
    {

        #region Interna

        private readonly IDatabaseWriter _databaseWriter;

        private readonly AttributeRepository _attributeRepository;
        private readonly SignInterpretationCommentaryRepository _commentaryRepository;
        private readonly RoiRepository _roiRepository;


        public TextRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
        {
            _databaseWriter = databaseWriter;

            // Because some functions set or remove attributes, commentaries, or ROIs we sometimes need
            // objects of the repositories. If we don't want to create them from the beginning,
            // we would have to store the configuration to make it accessible for creation the object elsewhere
            _attributeRepository = new AttributeRepository(config, databaseWriter);
            _commentaryRepository = new SignInterpretationCommentaryRepository(config, databaseWriter);
            _roiRepository = new RoiRepository(config, databaseWriter);


        }

        public IDbConnection Connection => OpenConnection();
        #endregion


        #region Public Methods

        #region Line

        /// <summary>
        /// Creates a new line in an edition and inserts it into the fragment identified by fragmentId.
        /// It automatically creates the line start and end signs
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="lineData">line data object which must contain the line name and may contain
        /// signs automatically added to to the line (except terminators, which are automatically
        /// set)</param>
        /// <param name="fragmentId">Id of the fragment the line should be inserted into</param>
        /// <param name="anchorBefore">The interpretation id anchor before</param>
        /// <param name="anchorAfter">The interpretation id anchor aftere</param>
        /// <returns>An instance of Line</returns>
        public async Task<LineData> CreateLineAsync(EditionUserInfo editionUser,
            LineData lineData,
            uint fragmentId,
            uint anchorBefore = 0,
            uint anchorAfter = 0)
        {
            return await DatabaseCommunicationRetryPolicy.ExecuteRetry(
                async () =>
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        // Create the new text fragment abstract id
                        var newLineId = await _simpleInsertAsync(
                            TableData.Table.line);
                        ;

                        lineData.LineId = newLineId;

                        // Add the new text fragment to the edition manuscript
                        await _addLineToTextFragment(editionUser, newLineId, fragmentId);

                        // Create the data entry for the new text fragment
                        await _setLineDataAsync(editionUser, newLineId, lineData.LineName);

                        lineData.Signs.Insert(0,
                            CreateTerminatorSign(TableData.Table.line, TableData.TerminatorType.Start));
                        lineData.Signs.Add(CreateTerminatorSign(TableData.Table.line,
                            TableData.TerminatorType.End));

                        lineData.Signs = await CreateSignsAsync(editionUser,
                            lineData.LineId.GetValueOrDefault(),
                            lineData.Signs,
                            anchorBefore > 0 ?
                                new List<uint>() { anchorBefore } :
                                new List<uint>(),
                            anchorAfter > 0 ?
                                new List<uint>() { anchorAfter } :
                                new List<uint>());


                        // End the transaction (it was all or nothing)
                        transactionScope.Complete();

                        // Return the new line to user
                        return lineData;
                    }
                }
            );
            return null;
        }

        /// <summary>
        /// Gets the text of a line in an edition
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="lineId">Line id</param>
        /// <returns>A detailed text object</returns>
        public async Task<TextEdition> GetLineByIdAsync(EditionUserInfo editionUser, uint lineId)
        {
            var terminators = _getTerminators(editionUser, TableData.Table.line, lineId);

            if (!terminators.IsValid)
                return new TextEdition();

            return await _getEntityById(editionUser, terminators);
        }

        /// <summary>
        /// Get a list of all lines in a text fragment.
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
        /// Removes the line with the given Id together with all its signs.
        /// </summary>
        /// <param name="editionUser"></param>
        /// <param name="lineId">Id of line</param>
        /// <returns>Id of removed line</returns>
        public async Task<uint> RemoveLineAsync(EditionUserInfo editionUser, uint lineId)
        {
            var signIds = await _getChildrenIds(editionUser, TableData.Table.line, lineId);
            foreach (var signId in signIds)
            {
                await RemoveSignAsync(editionUser, signId);
            }
            return await _removeElementAsync(editionUser, TableData.Name(TableData.Table.line), lineId);
        }

        public async Task<LineData> UpdateLineAsync(EditionUserInfo editionUser,
            uint lineId,
            string lineName)
        {

            await _setLineDataAsync(
                editionUser,
                lineId,
                lineName,
                false);
            return new LineData() { LineId = lineId, LineName = lineName };

        }

        #endregion

        #region Sign and its interpretation


        public async Task<List<uint>> GetAllSignInterpretationIdsForSignIdAsync(EditionUserInfo editionUser, uint signId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<uint>(
                    GetSignInterpretationIdsForSignIdQuery.GetQuery,
                    new
                    {
                        editionUser.EditionId,
                        SignId = signId
                    })).ToList();
            }
        }



        /// <summary>
        /// Creates the signs from the information provided by the sign objects and adds them as
        /// a path between the given anchors.
        /// If more than one sign interpretation is provided for a sign, forking paths are created
        /// fromthe different interpretations
        /// </summary>
        /// <param name="editionUser"></param>
        /// <param name="lineId"></param>
        /// <param name="signs"></param>
        /// <param name="anchorsBefore"></param>
        /// <param name="anchorsAfter"></param>
        /// <returns></returns>
        public async Task<List<SignData>> CreateSignsAsync(EditionUserInfo editionUser,
            uint lineId,
            List<SignData> signs,
            List<uint> anchorsBefore,
            List<uint> anchorsAfter
        )
        {
            var newSigns = new List<SignData>();
            SignData previousSignData = null;
            // Stores for each sign the actual anchors afte which it should be injected
            // into th reading stream
            var internalAnchorsBefore = anchorsBefore;
            foreach (var sign in signs)
            {

                // First, create a simple entry in the sign table
                var newSignData = await _createSignAsync(editionUser, lineId);
                // Add the given sign interpretations which also inject the sign in the reading stream
                newSignData.SignInterpretations = await AddSignInterpretationsAsync(editionUser,
                    newSignData.SignId,
                    sign.SignInterpretations,
                    internalAnchorsBefore,
                    anchorsAfter);

                // Set the new sign interpretation ids as anchors before the next sign
                internalAnchorsBefore = newSignData.SignInterpretations.Select(
                    si => si.SignInterpretationId.GetValueOrDefault()).ToList();

                // If already a sign had been set adhjust its nextSignInterpretations
                // TODO do we need this here? (Ingo)
                if (previousSignData != null)
                {
                    // Create an hashset of next sign interpretations from the new anchors before
                    var nextSignInterpretations = internalAnchorsBefore.Select(
                        signInterpretationId => new NextSignInterpretation(
                        signInterpretationId,
                        (uint)editionUser.EditionEditorId)).Distinct().ToHashSet();

                    // Store this hashset into each signInterpretation of the previous set sign 
                    previousSignData.SignInterpretations.ForEach(
                        signInterpretation => signInterpretation.NextSignInterpretations = nextSignInterpretations);

                }
                previousSignData = newSignData;
                newSigns.Add(newSignData);
            }

            return newSigns;
        }


        /// <summary>
        /// Adds interpretations to an existing sign.
        /// The given interpretations are connected as parallel paths to the given anchors.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signId">Id of the sign</param>
        /// <param name="signInterpretations">List of sign interpretation objects</param>
        /// <param name="anchorsBefore">Ids of the anchors before</param>
        /// <param name="anchorsAfter">Ids of the ancors after</param>
        /// <returns>List of sign Interpretation objects with the new ids</returns>
        /// <exception cref="DataNotWrittenException"></exception>
        public async Task<List<SignInterpretationData>> AddSignInterpretationsAsync(EditionUserInfo editionUser,
            uint? signId,
            List<SignInterpretationData> signInterpretations,
            List<uint> anchorsBefore,
            List<uint> anchorsAfter)
        {

            using (var connection = OpenConnection())
            {
                foreach (var signInterpretation in signInterpretations)
                {
                    // Flag which marks if the sign interpretation had to be created from the scratch
                    var newSignInterpretation = true;
                    var createSignInterpretationIdParameters = new DynamicParameters();
                    createSignInterpretationIdParameters.Add("@SignId", signId);
                    createSignInterpretationIdParameters.Add("@Character", signInterpretation.Character);

                    var addSignInterpretationResult = await connection.ExecuteAsync(
                        AddSignInterpretationQuery.GetQuery, createSignInterpretationIdParameters
                    );

                    // If the creation fails than the raw sign interpretation could could already exist
                    if (addSignInterpretationResult == 0)
                    {
                        signInterpretation.SignInterpretationId = await connection.QuerySingleOrDefaultAsync<uint>(
                            GetSignInterpretationIdQuery.GetQuery,
                            createSignInterpretationIdParameters);

                        // If no fitting sign interpretation is found throw an error
                        if (signInterpretation.SignInterpretationId == null)
                        {
                            throw new StandardExceptions.DataNotWrittenException($"add new sign interpretation");
                        }


                        newSignInterpretation = false;

                    }
                    else // Store the new sign interpretation id
                    {
                        signInterpretation.SignInterpretationId =
                            await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);
                    }

                    // Now insert the new sign interpretation into the path
                    var positionDataRequestFactory = await PositionDataRequestFactory.CreateInstanceAsync(
                        connection,
                        StreamType.SignInterpretationStream,
                        signInterpretation.SignInterpretationId.GetValueOrDefault(),
                        editionUser.EditionId,
                        false);

                    // If the sign interpretation already existed than we have to move it.
                    positionDataRequestFactory.AddAction(
                        newSignInterpretation ? PositionAction.CreatePathFromItems : PositionAction.MoveInBetween);
                    positionDataRequestFactory.AddAnchorsAfter(anchorsAfter);
                    positionDataRequestFactory.AddAnchorsBefore(anchorsBefore);
                    var positionRequests = await positionDataRequestFactory.CreateRequestsAsync();
                    await _databaseWriter.WriteToDatabaseAsync(editionUser, positionRequests);

                    // Add the attributes
                    var attributes = newSignInterpretation ?
                        await _attributeRepository.CreateAttributesAsync(
                            editionUser,
                            signInterpretation.SignInterpretationId.GetValueOrDefault(),
                        signInterpretation.Attributes)
                        : await _attributeRepository.ReplaceSignInterpretationAttributesAsync(
                            editionUser,
                            signInterpretation.SignInterpretationId.GetValueOrDefault(),
                            signInterpretation.Attributes);
                    // We have to store the create attributes  because the now contain also the new ids.
                    signInterpretation.Attributes.Clear();
                    signInterpretation.Attributes.AddRange(attributes);

                    // Do the same with the commentaries
                    var commentaries = newSignInterpretation ?
                        await _commentaryRepository.CreateCommentariesAsync(
                        editionUser,
                        signInterpretation.SignInterpretationId.GetValueOrDefault(),
                        signInterpretation.Commentaries)
                        : await _commentaryRepository.ReplaceSignInterpretationCommentaries(
                            editionUser,
                            signInterpretation.SignInterpretationId.GetValueOrDefault(),
                            signInterpretation.Commentaries);
                    signInterpretation.Commentaries.Clear();
                    signInterpretation.Commentaries.AddRange(commentaries);

                    // Do the same with ROIs
                    if (signInterpretation.SignInterpretationRois.Count <= 0) continue;
                    signInterpretation.SignInterpretationRois.ForEach(
                        roi => roi.SignInterpretationId = signInterpretation.SignInterpretationId);
                    var rois = newSignInterpretation
                        ? await _roiRepository.CreateRoisAsync(
                            editionUser,
                            signInterpretation.SignInterpretationRois)
                        : await _roiRepository.ReplaceSignInterpretationRoisAsync(
                            editionUser,
                            signInterpretation.SignInterpretationRois);
                    signInterpretation.Commentaries.Clear();
                    signInterpretation.Commentaries.AddRange(commentaries);

                }
            }

            return signInterpretations;
        }

        /// <summary>
        /// Removes all attributes, commentaries, rois, and position data of the sign interpretation
        /// connected with the given edition and by this removing
        /// it from the given edition without touching it in respect to other editions
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <returns>Id of the sign interpretation</returns>
        public async Task<uint> RemoveSignInterpretationAsync(EditionUserInfo editionUser,
            uint signInterpretationId)
        {
            // Remove all attributes
            await _attributeRepository.DeleteAllAttributesForSignInterpretationAsync(editionUser, signInterpretationId);
            // Remove all commentaries
            await _commentaryRepository.DeleteAllCommentariesForSignInterpretationAsync(editionUser,
                signInterpretationId);
            // Remove all ROIs
            await _roiRepository.DeleteAllRoisForSignInterpretationAsync(editionUser, signInterpretationId);

            // Take out from path
            using (var connection = OpenConnection())
            {
                var positionDataRequest = await PositionDataRequestFactory.CreateInstanceAsync(
                    connection,
                    StreamType.SignInterpretationStream,
                    signInterpretationId,
                    editionUser.EditionId,
                    true);
                positionDataRequest.AddAction(PositionAction.TakeOutPathOfItems);
                var requests = await positionDataRequest.CreateRequestsAsync();
                _databaseWriter.WriteToDatabaseAsync(editionUser, requests);
            }

            return signInterpretationId;
        }

        /// <summary>
        /// Removes the sign with the given Id together with all its interpretation.s
        /// </summary>
        /// <param name="editionUser"></param>
        /// <param name="signId">Id of sign</param>
        /// <returns>Id of removed sign</returns>
        public async Task<uint> RemoveSignAsync(EditionUserInfo editionUser, uint signId)
        {
            var signInterpretationIds = await GetAllSignInterpretationIdsForSignIdAsync(editionUser, signId);
            foreach (var signInterpretationId in signInterpretationIds)
            {
                await RemoveSignInterpretationAsync(editionUser, signInterpretationId);
            }
            return await _removeElementAsync(editionUser, "line_to_sign", signId);
            return signId;
        }

        #endregion

        #region Text Fragment


        /// <summary>
        /// Creates a new text fragment in an edition. If previousFragmentId or nextFragmentId are null, the missing
        /// value will be automatically calculated. If both are null, then the new text fragment is added to the end
        /// of the list of text fragments.
        /// Each text fragment must have at least one line to hold break signs and to be accessible
        /// by the textual system; thus the function automatically creates an empty line.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="textFragmentData">Text fragment data object which must have set the
        /// name of the text fragments. If it contains lines they will be automatically injected,
        /// otherwise an empty line with name "1" is added. Terminators are automatically set
        /// and thus should not be included in this data object.</param>
        /// <param name="previousFragmentId">Id of the text fragment that should directly precede the new text fragment,
        /// may be null</param>
        /// <param name="nextFragmentId">Id of the text fragment that should directly follow the new text fragment,
        /// <param name="firstLineName">name of the first, empty line</param>
        /// may be null</param>
        /// <returns></returns>
        public async Task<TextFragmentData> CreateTextFragmentAsync(EditionUserInfo editionUser,
            TextFragmentData textFragmentData,
            uint? previousFragmentId,
            uint? nextFragmentId)
        {
            return await DatabaseCommunicationRetryPolicy.ExecuteRetry(
                async () =>
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        // Create the new text fragment abstract id
                        var newTextFragmentId = await _simpleInsertAsync(
                            TableData.Table.text_fragment);

                        // Add the new text fragment to the edition manuscript
                        await _addTextFragmentToManuscript(editionUser, newTextFragmentId);

                        // Create the data entry for the new text fragment
                        await _setTextFragmentDataAsync(editionUser,
                            newTextFragmentId,
                            textFragmentData.TextFragmentName);

                        // Now set the position for the new text fragment
                        (previousFragmentId, nextFragmentId) = await _createTextFragmentPosition(
                            editionUser,
                            previousFragmentId,
                            newTextFragmentId,
                            nextFragmentId
                        );


                        var newLines = new List<LineData>();
                        foreach (var line in textFragmentData.Lines)
                        {
                            newLines.Add(await CreateLineAsync(
                                editionUser,
                                line,
                                newTextFragmentId));
                        }



                        // End the transaction (it was all or nothing)
                        transactionScope.Complete();

                        // Set the new values to the text fragment
                        textFragmentData.Lines = newLines;
                        textFragmentData.TextFragmentId = newTextFragmentId;
                        textFragmentData.TextFragmentEditorId = editionUser.EditionEditorId;

                        return textFragmentData;
                    }
                }
            );
        }


        /// <summary>
        /// Gets a list of all artefacts with ROI's linked to text in the text fragment
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
        /// Gets the text of a text fragment in an edition
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="textFragmentId">Text fragment id</param>
        /// <returns>A detailed text object</returns>
        public async Task<TextEdition> GetTextFragmentByIdAsync(EditionUserInfo editionUser, uint textFragmentId)
        {
            var terminators = _getTerminators(editionUser, TableData.Table.text_fragment, textFragmentId);

            if (!terminators.IsValid)
                return new TextEdition();

            return await _getEntityById(editionUser, terminators);
        }

        /// <summary>
        /// Get a list of all the text fragments in an edition
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <returns>A list of all text fragments in the edition</returns>
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

        /// <summary>
        /// Removes the text fragment with the given Id together with all its lines and their signs.
        /// </summary>
        /// <param name="editionUser"></param>
        /// <param name="textFragmentId">Id of text frgament</param>
        /// <returns>Id of removed text fragment</returns>
        public async Task<uint> RemoveTextFragmentAsync(EditionUserInfo editionUser, uint textFragmentId)
        {
            var lineIds = await _getChildrenIds(editionUser, TableData.Table.text_fragment, textFragmentId);
            foreach (var lineId in lineIds)
            {
                await RemoveLineAsync(editionUser, lineId);
            }
            return await _removeElementAsync(editionUser, TableData.Name(TableData.Table.text_fragment), textFragmentId); ;
        }


        /// <summary>
        /// Updates the details of a text fragment. If previousFragmentId or nextFragmentId are null, the missing
        /// value will be automatically calculated. If both are null, the text fragment will not be moved.
        /// If the fragmentName is null or "", the name will not be altered.
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
                    (previousFragmentId, nextFragmentId) = await _moveTextFragments(editionUser, textFragmentId,
                        previousFragmentId, nextFragmentId);

                // End the transaction (it was all or nothing)
                transactionScope.Complete();

                // Package the new text fragment to return to user
                return new TextFragmentData
                {
                    TextFragmentId = textFragmentId,
                    TextFragmentName = fragmentName,
                    PreviousTextFragmentId = previousFragmentId,
                    NextTextFragmentId = nextFragmentId,
                    TextFragmentEditorId = editionUser.EditionEditorId.Value
                };
            }
        }

        #endregion

        #endregion

        #region Private methods

        #region Common helpers

        private Terminators _getTerminators(EditionUserInfo editionUser, TableData.Table table, uint elementId)
        {

            var query = $@"SELECT DISTINCT sign_interpretation_id 
                        {TableData.FromQueryPart(table, addPublicEdition: true)}
                        AND attribute_value_id in @Breaks
                        ORDER BY attribute_value_id";
            using (var connection = OpenConnection())
            {
                return new Terminators(connection.Query<uint>(
                        query,
                        new
                        {
                            ElementId = elementId,
                            UserId = editionUser.userId,
                            Breaks = TableData.Terminators(table)
                        }
                    )
                    .ToArray());
            }
        }


        private async Task<TextEdition> _getEntityById(EditionUserInfo editionUser, Terminators terminators)
        {
            TextEdition lastEdition = null;
            TextFragmentData lastTextFragment = null;
            LineData lastLineData = null;
            SignData lastSignData = null;
            NextSignInterpretation lastNextSignInterpretation = null;
            SignInterpretationData lastChar = null;
            SignInterpretationRoiData lastInterpretationRoi = null;


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
                        typeof(TextEdition), typeof(TextFragmentData), typeof(LineData), typeof(SignData),
                        typeof(NextSignInterpretation), typeof(SignInterpretationData), typeof(SignInterpretationAttributeData),
                        typeof(SignInterpretationRoiData)
                    },
                    objects =>
                    {
                        var manuscript = objects[0] as TextEdition;
                        var fragment = objects[1] as TextFragmentData;
                        var line = objects[2] as LineData;
                        var sign = objects[3] as SignData;
                        var nextSignInterpretation = objects[4] as NextSignInterpretation;
                        var signInterpretation = objects[5] as SignInterpretationData;
                        var charAttribute = objects[6] as SignInterpretationAttributeData;
                        var roi = objects[7] as SignInterpretationRoiData;

                        var newManuscript = manuscript.manuscriptId != lastEdition?.manuscriptId;

                        if (newManuscript) lastEdition = manuscript;

                        if (fragment.TextFragmentId != lastTextFragment?.TextFragmentId)

                            lastEdition = manuscript.manuscriptId == lastEdition?.manuscriptId
                                ? lastEdition
                                : manuscript;
                        if (fragment.TextFragmentId != lastTextFragment?.TextFragmentId)
                        {
                            lastTextFragment = fragment;
                            lastEdition.fragments.Add(fragment);
                        }

                        if (line.LineId != lastLineData?.LineId)
                        {
                            lastLineData = line;
                            lastTextFragment.Lines.Add(line);
                        }

                        if (sign.SignId != lastSignData?.SignId)
                        {
                            lastSignData = sign;
                            lastLineData.Signs.Add(sign);
                        }

                        if (nextSignInterpretation.NextSignInterpretationId
                            != lastNextSignInterpretation?.NextSignInterpretationId)
                            lastNextSignInterpretation = nextSignInterpretation;

                        if (signInterpretation.SignInterpretationId != lastChar?.SignInterpretationId)
                        {
                            lastChar = signInterpretation;
                            lastSignData.SignInterpretations.Add(signInterpretation);
                        }

                        lastChar.NextSignInterpretations.Add(nextSignInterpretation);

                        charAttribute.AttributeString = attributeDict.TryGetValue(
                            charAttribute.AttributeValueId.GetValueOrDefault(),
                            out var val
                        )
                            ? val
                            : null;

                        //NOTE (by Ingo): I added this check to prevent that sign interpretations are stored
                        // several times when there are more than 1 next sign interpretation ids
                        if (!lastChar.Attributes.Exists(
                            a => a.AttributeValueId==charAttribute.AttributeValueId)
                        )
                            lastChar.Attributes.Add(charAttribute);

                        if (roi == null
                            || roi.SignInterpretationRoiId == lastInterpretationRoi?.SignInterpretationRoiId
                        ) return newManuscript ? manuscript : null;

                        lastInterpretationRoi = roi;
                        lastChar.SignInterpretationRois.Add(roi);

                        return newManuscript ? manuscript : null;
                    },
                    new { terminators.StartId, terminators.EndId, editionUser.EditionId },
                    splitOn:
                    "textFragmentId, lineId, signId, nextSignInterpretationId, signInterpretationId, SignInterpretationAttributeId, SignInterpretationRoiId"
                );
                var formattedEdition = scrolls.AsList()[0];
                formattedEdition.AddLicence();
                return formattedEdition;
            }
        }

        private async Task<List<uint>> _getChildrenIds(EditionUserInfo user, TableData.Table table, uint elementId)
        {

            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<uint>(
                    TableData.GetChildrenIdsQuery(table),
                    new { user.EditionId, ElementId = elementId }
                )).ToList();
            }
        }

        /// <summary>
        /// Gets the  data id for an element id
        /// </summary>
        /// <param name="user">Edition user object</param>
        /// <param name="table">Name of the table</param>
        /// <param name="elementId">Id of the text fragment</param>
        /// <returns>Text fragment data id of the text fragment</returns>
        private async Task<uint> _getElementDataId(EditionUserInfo user, TableData.Table table, uint elementId)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QuerySingleAsync<uint>(
                    TableData.GetDataIdQuery(table),
                    new { user.EditionId, ElementId = elementId }
                );
            }
        }


        private async Task<uint> _removeElementAsync(EditionUserInfo editionUser, string tableName, uint elementId)
        {

            var removeRequest = new MutationRequest(
                MutateType.Delete,
                new DynamicParameters(),
                tableName,
                elementId
            );

            var writeResults = await _databaseWriter.WriteToDatabaseAsync(
                editionUser,
                new List<MutationRequest> { removeRequest }
            );

            if (writeResults.Count != 1)
                throw new StandardExceptions.DataNotWrittenException($"delete {tableName}");

            return elementId;

        }

        /// <summary>
        /// Helper to create a new record of those tables which only have an id-field like Line, Sign ...."
        /// The error string is created automatically using the table namen
        /// </summary>
        /// <param name="table">TableData reference</param>
        /// <returns>New id</returns>
        private async Task<uint> _simpleInsertAsync(TableData.Table table)
        {
            var tableName = TableData.Name(table);
            var insertQuery = $"INSERT INTO {tableName} () VALUES ()";
            using (var connection = OpenConnection())
            {
                // Create the new t id
                var createTableId = await connection.ExecuteAsync(insertQuery);
                if (createTableId == 0)
                    throw new StandardExceptions.DataNotWrittenException($"create new {tableName}");

                // Get the new id
                var getNewTableId = (await connection.QueryAsync<uint>(LastInsertId.GetQuery)).ToList();
                if (getNewTableId.Count != 1)
                    throw new StandardExceptions.DataNotWrittenException($"create new {tableName}");

                return getNewTableId.First();
            }
        }

        /// <summary>
        /// Adds an element of text (sign, line, fragment) to its parent element.
        /// The text of the error is automatically set using the names of the tables.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="table">Name of the element table</param>
        /// <param name="elementId">Id of the element</param>
        /// <param name="parentId">Id of parent</param>
        /// <returns></returns>
        /// <exception cref="DataNotWrittenException"></exception>
        private async Task _addElementToParentAsync(EditionUserInfo editionUser,
            TableData.Table table, uint? elementId, uint? parentId)
        {
            using (var connection = OpenConnection())
            {
                // Link the parent to the element
                var parentTable = TableData.Parent(table);
                var parentToElementParameters = new DynamicParameters();
                parentToElementParameters.Add($"@{parentTable}_id", parentId);
                parentToElementParameters.Add($"@{table}_id", elementId);
                var manuscriptToTextFragmentResults =
                    await _databaseWriter.WriteToDatabaseAsync(
                        editionUser,
                        new List<MutationRequest>
                        {
                            new MutationRequest(
                                MutateType.Create,
                                parentToElementParameters,
                                $"{parentTable}_to_{table}e"
                            )
                        }
                    );

                // Check for success
                if (manuscriptToTextFragmentResults.Count != 1)
                    throw new StandardExceptions.DataNotWrittenException(
                        $"{parentTable} id to new {table} link");
            }
        }

        /// <summary>
        /// Set the name of an element
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="table">The name of the table of the element</param>
        /// <param name="elementId">Id of the element to set</param>
        /// <param name="elementName">Name to be set</param>
        /// <param name="create">Boolean whether a new text fragment should be created for this name. Set to
        /// false if you are updating existing data.</param>
        /// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
        private async Task _setElementDataAsync(EditionUserInfo editionUser,
            TableData.Table table,
            uint elementId,
            string elementName,
            bool create = true)
        {
            // Set the parameters for the mutation object
            var createTextFragmentParameters = new DynamicParameters();
            createTextFragmentParameters.Add("@name", elementName);
            createTextFragmentParameters.Add($"@{table}_id", elementId);

            // Create the mutation object
            var createElementMutation = new MutationRequest(
                create ? MutateType.Create : MutateType.Update,
                createTextFragmentParameters,
                $"{table}_data",
                create ? null : (uint?)(await _getElementDataId(editionUser, table, elementId))
            );

            // Commit the mutation
            var createElementResponse = await _databaseWriter.WriteToDatabaseAsync(
                editionUser,
                new List<MutationRequest> { createElementMutation }
            );

            // Ensure that the entry was created
            if (createElementResponse.Count != 1
                || (create && !createElementResponse.First().NewId.HasValue))
                throw new StandardExceptions.DataNotWrittenException($"create new {TableData.Name(table)} data");
        }


        /// <summary>
        /// Creates a new element in an edition and inserts it into the parent identified by parentId.
        /// If an name of the element is given, the name is also set
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="parentId">Id of the fragment the line should be inserted into</param>
        /// <param name="elementName">Name of the new line;
        /// may be null</param>
        /// <returns>Id of the created Element</returns>
        public async Task<uint> _createElementAsync(EditionUserInfo editionUser,
            TableData.Table table,
            string elementName,
            uint parentId)
        {
            // Create the new element abstract id
            var newElementId = await _simpleInsertAsync(
                table);

            // Add the new element to the parent
            await _addElementToParentAsync(
                editionUser,
                table,
                newElementId,
                parentId);

            // Create the data entry for the new element
            if (elementName != null)
                await _setElementDataAsync(editionUser, table, newElementId, elementName);


            return newElementId;
        }

        #endregion

        #region Line

        /// <summary>
        /// Adds a line to a fragment.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="lineId">Id of line</param>
        /// <param name="textFragmentId">Id of the text fragment to be added</param>
        /// <returns></returns>
        private async Task _addLineToTextFragment(EditionUserInfo editionUser, uint lineId, uint textFragmentId)
        {
            _addElementToParentAsync(editionUser,
                TableData.Table.line,
                lineId,
                textFragmentId);
        }

        /// <summary>
        /// Gets the line data id for a line id
        /// </summary>
        /// <param name="user">Edition user object</param>
        /// <param name="lineId">Id of the line</param>
        /// <returns>Line data id of the line</returns>
        private async Task<uint> _getLineDataId(EditionUserInfo user, uint lineId)
        {
            return await _getElementDataId(user, TableData.Table.line, lineId);
        }

        /// <summary>
        /// Set the name of a line
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="lineId">Id of the text fragment to set</param>
        /// <param name="lineName">Name to be set</param>
        /// <param name="create">Boolean whether a new text fragment should be created for this name. Set to
        /// false if you are updating existing data.</param>
        private async Task _setLineDataAsync(EditionUserInfo editionUser,
            uint lineId,
            string lineName,
            bool create = true)
        {
            await _setElementDataAsync(
                editionUser,
                TableData.Table.line,
                lineId,
                lineName,
                create);
        }



        #endregion

        #region Sign and sign interpretation

        /// <summary>
        /// Adds a sign to a line.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signId">Id of the sign</param>
        /// <param name="lineId">Id of the line</param>
        /// <returns></returns>
        private async Task _addSignToLine(EditionUserInfo editionUser, uint? signId, uint? lineId)
        {
            _addElementToParentAsync(editionUser,
                TableData.Table.sign,
                signId,
                lineId);
        }

        /// <summary>
        /// Creates a new sign without all other sign data.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="lineId">Id of line to which the sign should belong</param>
        /// <returns></returns>
        public async Task<SignData> _createSignAsync(EditionUserInfo editionUser,
            uint lineId
        )
        {
            return await DatabaseCommunicationRetryPolicy.ExecuteRetry(
                async () =>
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        // Create the new text fragment abstract id
                        var newSignId = await _simpleInsertAsync(
                            TableData.Table.sign);
                        ;

                        // Add the new text fragment to the edition manuscript
                        await _addSignToLine(editionUser, newSignId, lineId);

                        // End the transaction (it was all or nothing)
                        transactionScope.Complete();

                        // Package the new text fragment to return to user
                        return new SignData
                        {
                            SignId = newSignId
                        };
                    }
                }
            );
            return null;
        }








        #endregion


        #region Text Fragment





        /// <summary>
        /// Set the name of a text fragment
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="textFragmentId">Id of the text fragment to set</param>
        /// <param name="textFragmentName">Name to be set</param>
        /// <param name="create">Boolean whether a new text fragment should be created for this name. Set to
        /// false if you are updating existing data.</param>
        /// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
        private async Task _setTextFragmentDataAsync(EditionUserInfo editionUser,
            uint textFragmentId,
            string textFragmentName,
            bool create = true)
        {
            await _setElementDataAsync(
                editionUser,
                TableData.Table.text_fragment,
                textFragmentId,
                textFragmentName,
                create);

        }

        /// <summary>
        /// Set the position of a newly created text fragment, if anchorBefore or anchorAfter are null, they
        /// will be automatically created.  If both are null, then the fragment is positioned at the end of
        /// the list of text fragments.
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

            positionDataRequestFactory.AddAction(PositionAction.DisconnectNeighbouringAnchors);
            positionDataRequestFactory.AddAction(PositionAction.CreatePathFromItems);
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
            positionDataRequestFactory.AddAction(PositionAction.MoveInBetween);
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
                new List<uint>() { textFragmentId },
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
                _verifyTextFragmentsSequence(fragments, anchorBefore, anchorAfter);

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
                            true);
                        var before = tempFac.AnchorsBefore; // Get the text fragment(s) directly before it
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
                    var after = tempFac.AnchorsAfter; // Get the text fragment(s) directly after it
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
                new List<uint>() { textFragmentId },
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
        private static void _verifyTextFragmentsSequence(
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

                if (fragment.TextFragmentId != anchorAfter) continue;
                anchorAfterExists = true;
                // Check for correct sequence of anchors if applicable
                if (anchorBefore.HasValue && (!anchorBeforeIdx.HasValue || anchorBeforeIdx.Value + 1 != idx))
                    throw new StandardExceptions.InputDataRuleViolationException(
                        "the previous and next text fragment ids must be sequential");
            }

            if (anchorBefore.HasValue && !anchorBeforeExists)
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
            uint manuscriptId;
            using (var connection = OpenConnection())
            {
                // Get the manuscript id of the current edition
                manuscriptId = await connection.QuerySingleAsync<uint>(
                    ManuscriptOfEdition.GetQuery,
                    new { editionUser.EditionId }
                );
            }

            _addElementToParentAsync(editionUser,
                TableData.Table.text_fragment,
                textFragmentId,
                manuscriptId
            );

        }

        /// <summary>
        ///     Gets the text fragment data id for a text fragment id
        /// </summary>
        /// <param name="user">Edition user object</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>Text fragment data id of the text fragment</returns>
        private async Task<uint> _getTextFragmentDataId(EditionUserInfo user, uint textFragmentId)
        {
            return await _getElementDataId(user, TableData.Table.text_fragment, textFragmentId);
        }

        #endregion Text Fragment

        #endregion Private methods
    }
}