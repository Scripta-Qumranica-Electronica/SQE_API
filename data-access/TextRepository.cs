using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;


namespace SQE.SqeHttpApi.DataAccess
{

    public interface ITextRepository
    {
        Task<TextEdition> GetLineByIdAsync(UserInfo user, uint lineId);
        Task<TextEdition> GetTextFragmentByIdAsync(UserInfo user, uint textFragmentId);
        Task<List<LineData>> GetLineIdsAsync(UserInfo user, uint textFragmentId);
        Task<List<TextFragmentData>> GetFragmentDataAsync(UserInfo user);
        Task<TextFragmentData> CreateTextFragmentAsync(UserInfo user,
            string fragmentName, uint? previousFragmentId, uint? nextFragmentId);
    }

    public class TextRepository : DbConnectionBase, ITextRepository
    {
        private readonly IDatabaseWriter _databaseWriter;

        public TextRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
        {
            _databaseWriter = databaseWriter;
        }

        public async Task<TextEdition> GetLineByIdAsync(UserInfo user, uint lineId)
        {
            var terminators = _getTerminators(user, GetLineTerminators.GetQuery, lineId);

            if (terminators.Length!=2) 
                return new TextEdition();

            return await _getEntityById(user, terminators[0], terminators[1]);
            
        }
        
        public async Task<TextEdition> GetTextFragmentByIdAsync(UserInfo user, uint textFragmentId)
        {
            var terminators = _getTerminators(user, GetFragmentTerminators.GetQuery, textFragmentId);

            if (terminators.Length!=2) 
                return new TextEdition();

           return await _getEntityById(user, terminators[0], terminators[1]);
            
        }

        public async Task<List<LineData>> GetLineIdsAsync(UserInfo user, uint textFragmentId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<LineData>(
                    GetLineData.Query,
                    param: new {TextFragmentId = textFragmentId, EditionId = user.editionId, UserId = user.userId}
                )).ToList();
                    //connection.Close();
            }
        }

        public async Task<List<TextFragmentData>> GetFragmentDataAsync(UserInfo user)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<TextFragmentData>(GetFragmentData.GetQuery,
                    param: new {EditionId = user.editionId, UserId = user.userId ?? 0}
                )).ToList();
            }
        }

        // TODO: adding a text fragment at a specific location does not yet work, fix it
        public async Task<TextFragmentData> CreateTextFragmentAsync(UserInfo user,
            string fragmentName, uint? previousFragmentId, uint? nextFragmentId)
        {
            using (var transactionScope = new TransactionScope(
                    TransactionScopeOption.Required,
                    new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted }
                )
            )
            {
                using (var connection = OpenConnection())
                {
                    // Get all text fragments for sorting operations later on
                    var textFragmentIds = (await connection.QueryAsync<TextFragmentData>(GetFragmentData.GetQuery,
                        param: new {EditionId = user.editionId, UserId = user.userId ?? 0}
                    )).ToList();
    
                    // Check to make sure the new named text fragment doesn't conflict with existing ones (the frontend will resolve this)
                    if (textFragmentIds.Any(x => x.TextFragmentName == fragmentName))
                        throw new StandardErrors.ConflictingData("textFragmentName");
    
                    ushort? nextPosition = null;
                    if (nextFragmentId.HasValue) // We know the existing text fragment that the new one will displace
                    {
                        // Verify that the nextFragmentId exists and take its position as the position for the new text fragment
                        var nextTextFragment = textFragmentIds.Where(x => x.TextFragmentId == nextFragmentId);
                        if (nextTextFragment.Count() != 1)
                            throw new StandardErrors.ImproperInputData("textFragmentId");
                        nextPosition = nextTextFragment.First().Position;
                    }
    
                    if (previousFragmentId.HasValue) // We know the text fragment after which the new one should be placed
                    {
                        ushort? previousPosition;
                        // Make sure the previousFragmentId exists
                        var previousTextFragment = textFragmentIds.Where(x => x.TextFragmentId == previousFragmentId).ToList();
                        if (previousTextFragment.Count() != 1)
                            throw new StandardErrors.ImproperInputData("textFragmentId");
                        previousPosition = previousTextFragment.First().Position;
                        if (nextPosition.HasValue
                        ) // If there is also a nextPosition, verify that previousPosition and nextPosition are sequential
                        {
                            if (previousPosition + 1 != nextPosition)
                                throw new StandardErrors.ImproperInputData("textFragmentId");
                        }
                        else // There is no nextPosition, so assume it should be one higher than the previousFragmentId
                        {
                            nextPosition = (ushort) (previousPosition.Value + 1);
                        }
                    }
                    else if (!nextFragmentId.HasValue) // Neither previousFragmentId nor nextFragmentId have been set
                        // so put the new text fragment at the end of the manuscript.
                    {
                        nextPosition = (ushort) (textFragmentIds.Any() ? textFragmentIds.Last().Position + 1 : 1);
                    }
    
                    // Create the new text fragment id
                    var createNewTextFragmentId = await connection.ExecuteAsync(CreateTextFragment.GetQuery);
                    if (createNewTextFragmentId == 0)
                        throw new StandardErrors.DataNotWritten("create new textFragment");
    
                    // Get the new text fragmentid
                    var getNewTextFragmentId = await connection.QueryAsync<uint>(LastInsertId.GetQuery);
                    if (getNewTextFragmentId.Count() != 1)
                        throw new StandardErrors.DataNotWritten("create new textFragment");
                    var newTextFragmentId = getNewTextFragmentId.First();
    
                    // Create the data entry for the new text fragment
                    var createTextFragmentParameters = new DynamicParameters();
                    createTextFragmentParameters.Add("@name", fragmentName);
                    createTextFragmentParameters.Add("@text_fragment_id", newTextFragmentId);
                    var createTextFragmentMutation = new MutationRequest(MutateType.Create,
                        createTextFragmentParameters, "text_fragment_data");
                    var createTextFragmentResponse = await _databaseWriter.WriteToDatabaseAsync(user,
                        new List<MutationRequest>() {createTextFragmentMutation});
                    if (createTextFragmentResponse.Count() != 1 || !createTextFragmentResponse.First().NewId.HasValue)
                        throw new StandardErrors.DataNotWritten("create new textFragment data");
    
                    // Shift the position of any text fragments that have been displaced by the new one
                    var textFragmentShiftMutations = textFragmentIds.Where(x => x.Position >= nextPosition)
                        .Select(x =>
                        {
                            var parameters = new DynamicParameters();
                            parameters.Add("@position", x.Position + 1);
                            parameters.Add("@text_fragment_id", x.TextFragmentId);
                            return new MutationRequest(MutateType.Update, parameters,
                                "text_fragment_sequence", x.TextFragmentSequenceId);
                        }).ToList();
                    var fragmentShiftParameters = new DynamicParameters();
    
                    // Also set the position for the new text fragment
                    fragmentShiftParameters.Add("@text_fragment_id", newTextFragmentId);
                    fragmentShiftParameters.Add("@position", nextPosition);
                    textFragmentShiftMutations.Add(new MutationRequest(MutateType.Create, fragmentShiftParameters,
                        "text_fragment_sequence"));
                    var shiftMutationResults =
                        await _databaseWriter.WriteToDatabaseAsync(user, textFragmentShiftMutations);
                    if (shiftMutationResults.Count() != textFragmentShiftMutations.Count())
                        throw new StandardErrors.DataNotWritten(
                            "shift textFragment sequence when creating new text fragment");
    
                    // Get the manuscript id of the current edition
                    var manuscriptId = await connection.QueryAsync<uint>(ManuscriptOfEdition.GetQuery,
                        new {EditionId = user.editionId.Value});
    
                    // Link the manuscript to the new text fragment
                    var manuscriptToTextFragmentParameters = new DynamicParameters();
                    manuscriptToTextFragmentParameters.Add("@manuscript_id", manuscriptId);
                    manuscriptToTextFragmentParameters.Add("@text_fragment_id", newTextFragmentId);
                    var manuscriptToTextFragmentResults =
                        await _databaseWriter.WriteToDatabaseAsync(user, new List<MutationRequest>()
                        {
                            new MutationRequest(MutateType.Create, manuscriptToTextFragmentParameters,
                                "manuscript_to_text_fragment")
                        });
                    if (manuscriptToTextFragmentResults.Count != 1)
                        throw new StandardErrors.DataNotWritten("manuscript id to new text fragment link");
                    
                    // End the transaction
                    transactionScope.Complete();
    
                    // Package the new text fragment to return to user
                    return new TextFragmentData()
                    {
                        TextFragmentId = newTextFragmentId,
                        TextFragmentName = fragmentName,
                        Position = nextPosition.Value,
                        TextFragmentSequenceId = 0
                    };
                }
            }
        }


        private uint[] _getTerminators(UserInfo user, string query, uint entityId)
        {
            uint[] terminators;
            using (var connection = OpenConnection())
            {
                terminators = (connection.Query<uint>(
                    query,
                    param: new {EntityId = entityId, EditionId = user.editionId ?? 0, UserId = user.userId ?? 0})).ToArray();
                connection.Close();
            }

            return terminators;


        }
        
        // TODO: Get collaborators from edition_editors if collaborators in the edition table is null.
        private async Task<TextEdition> _getEntityById(UserInfo user, uint startId, uint endId)
        {
            TextEdition lastEdition = null;
            TextFragment lastTextFragment = null;
            Line lastLine = null;
            Sign lastSign = null;
            NextSignInterpretation lastNextSignInterpretation = null;
            SignInterpretation lastChar = null;
            CharAttribute lastCharAttribute = null;
            
            
            using (var connection = OpenConnection())
            {
                var scrolls = await connection.QueryAsync<TextEdition>(
                    GetTextChunk.GetQuery, 
                    types: new Type[]{typeof(TextEdition), typeof(TextFragment), typeof(Line), typeof(Sign), 
                        typeof(NextSignInterpretation), typeof(SignInterpretation), typeof(CharAttribute), 
                        typeof(SignInterpretationROI)},
                    map: objects =>
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

                        if (newScroll)
                        {
                            lastEdition = manuscript;
                        }

                        if (fragment.textFragmentId != lastTextFragment?.textFragmentId)

                            lastEdition = manuscript.manuscriptId == lastEdition?.manuscriptId ? lastEdition : manuscript;
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

                        if (nextSignInterpretation.nextSignInterpretationId != lastNextSignInterpretation?.nextSignInterpretationId)
                        {
                            lastNextSignInterpretation = nextSignInterpretation;
                            lastSign.nextSignInterpretations.Add(nextSignInterpretation);
                        }

                        if (signInterpretation.signInterpretationId != lastChar?.signInterpretationId)
                        {
                            lastChar = signInterpretation;
                            lastSign.signInterpretations.Add(signInterpretation);
                        }

                        lastCharAttribute = charAttribute;
                        lastChar.attributes.Add(charAttribute);

                        if(roi != null)
                            lastChar.signInterpretationRois.Add(roi);
                        
                        return newScroll ? manuscript : null;
                    },
                    param: new {startId = startId, endId = endId, editionId=user.editionId ?? 0},
                    splitOn: "textFragmentId, lineId, signId, nextSignInterpretationId, signInterpretationId, interpretationAttributeId, SignInterpretationRoiId");
                //connection.Close();
                var formattedEdition = scrolls.AsList()[0];
                formattedEdition.addLicence();
                return formattedEdition;
            }
        }
    }
    
}