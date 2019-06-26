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
        Task<TextEdition> GetLineById(UserInfo user, uint lineId);
        Task<TextEdition> GetTextFragmentByIdAsync(UserInfo user, uint textFragmentId);
        Task<List<LineData>> GetLineIdsAsync(UserInfo user, uint fragmentId);
        Task<List<TextFragment>> GetFragmentIds(UserInfo user);
    }

    public class TextRepository : DbConnectionBase, ITextRepository
    {
        public TextRepository(IConfiguration config) : base(config) { }

        public async Task<TextEdition> GetLineById(UserInfo user, uint lineId)
        {
            var terminators = _getTerminators(user, TextRetrieval.GetLineTerminatorsQuery, lineId);

            if (terminators.Length!=2) 
                return new TextEdition();

            return await _getEntityById(user, terminators[0], terminators[1]);
            
        }
        
        public async Task<TextEdition> GetTextFragmentByIdAsync(UserInfo user, uint textFragmentId)
        {
            var terminators = _getTerminators(user, TextRetrieval.GetFragmentTerminatorsQuery, textFragmentId);

            if (terminators.Length!=2) 
                return new TextEdition();

           return await _getEntityById(user, terminators[0], terminators[1]);
            
        }

        public async Task<List<LineData>> GetLineIdsAsync(UserInfo user, uint fragmentId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<LineData>(
                    TextRetrieval.GetLineIdsQuery,
                    param: new {fragmentId = fragmentId, editionId = user.editionId, UserId = user.userId}
                )).ToList();
                    //connection.Close();
            }
        }

        public async Task<List<TextFragment>> GetFragmentIds(UserInfo user)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<TextFragment>(
                    TextRetrieval.GetFragmentIdsQuery,
                    param: new {editionId = user.editionId, UserId = user.userId}
                )).ToList();
                // connection.Close(); // using will close this for you, or so the docs say.
            }
        }


        private uint[] _getTerminators(UserInfo user, string query, uint entityId)
        {
            uint[] terminators;
            using (var connection = OpenConnection())
            {
                terminators = (connection.Query<uint>(
                    query,
                    param: new {EntityId = entityId, EditionId = user.editionId ?? 0})).ToArray();
                connection.Close();
            }

            return terminators;


        }
        
        // TODO:Get license and author data
        private async Task<TextEdition> _getEntityById(UserInfo user, uint startId, uint endId)
        {
            TextEdition lastEdition = null;
            Fragment lastFragment = null;
            Line lastLine = null;
            Sign lastSign = null;
            SignChar lastChar = null;
            CharAttribute lastCharAttribute = null;
            
            
            using (var connection = OpenConnection())
            {
                
                var scrolls = await connection.QueryAsync<TextEdition, Fragment, Line, Sign, SignChar, CharAttribute, TextEdition>(
                    TextRetrieval.GetTextChunkQuery,
                    map: (scroll, fragment, line, sign, signChar, charAttribute) =>
                    {
                        var newScroll = scroll.scrollId != lastEdition?.scrollId;

                        if (newScroll)
                        {
                            lastEdition = scroll;
                        }

                        if (fragment.fragmentId != lastFragment?.fragmentId)

                            lastEdition = scroll.scrollId == lastEdition?.scrollId ? lastEdition : scroll;
                        if (fragment.fragmentId != lastFragment?.fragmentId)
                        {
                            lastFragment = fragment;
                            lastEdition.fragments.Add(fragment);
                        }

                        if (line.lineId != lastLine?.lineId)
                        {
                            lastLine = line;
                            lastFragment.lines.Add(line);

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