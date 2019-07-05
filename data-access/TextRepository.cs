using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
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
    }

    public class TextRepository : DbConnectionBase, ITextRepository
    {
        public TextRepository(IConfiguration config) : base(config) { }

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