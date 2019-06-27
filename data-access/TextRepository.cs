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
            var terminators = _getTerminators(user, TextRetrieval.GetLineTerminatorsQuery, lineId);

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
        
        // TODO:Get license and author data
        private async Task<TextEdition> _getEntityById(UserInfo user, uint startId, uint endId)
        {
            TextEdition lastEdition = null;
            TextFragment lastTextFragment = null;
            Line lastLine = null;
            Sign lastSign = null;
            SignChar lastChar = null;
            CharAttribute lastCharAttribute = null;
            
            
            using (var connection = OpenConnection())
            {
                
                var scrolls = await connection.QueryAsync<TextEdition, TextFragment, Line, Sign, SignChar, CharAttribute, TextEdition>(
                    TextRetrieval.GetTextChunkQuery,
                    map: (scroll, fragment, line, sign, signChar, charAttribute) =>
                    {
                        var newScroll = scroll.manuscriptId != lastEdition?.manuscriptId;

                        if (newScroll)
                        {
                            lastEdition = scroll;
                        }

                        if (fragment.textFragmentId != lastTextFragment?.textFragmentId)

                            lastEdition = scroll.manuscriptId == lastEdition?.manuscriptId ? lastEdition : scroll;
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

                        if (signChar.signCharId != lastChar?.signCharId)
                        {
                            lastChar = signChar;
                            lastSign.signChars.Add(signChar);
                        }

                        lastCharAttribute = charAttribute;
                        lastChar.attributes.Add(charAttribute);


                        return newScroll ? scroll : null;
                    },
                    param: new {startId = startId, endId = endId, editionId=user.editionId ?? 0},
                    splitOn: "textFragmentId, lineId, signId, signCharId, charAttributeId");
                //connection.Close();
                var formattedEdition = scrolls.AsList()[0];
                formattedEdition.addLicence();
                return formattedEdition;
            }
        }
    }
    
}