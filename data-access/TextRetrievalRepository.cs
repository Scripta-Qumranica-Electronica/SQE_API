using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;


namespace SQE.SqeHttpApi.DataAccess
{

    public interface ITextRetrievalRepository
    {
        Task<Scroll> GetLineById( uint lineId, uint editionId);
        Task<Scroll> GetFragmentById( uint fragmentId, uint editionId);
        Task<uint[]> GetLineIds(uint fragmentId, uint editionId);
        Task<uint[]> GetFragmentIds(uint editionId);
    }

    public class TextRetrievalRepository : DBConnectionBase, ITextRetrievalRepository
    {
        public TextRetrievalRepository(IConfiguration config) : base(config) { }

        public async Task<Scroll> GetLineById( uint lineId, uint editionId)
        {
            var terminators = _getTerminators(
                TextRetrieval.GetLineTerminatorsQuery,
                lineId,
                editionId);

            if (terminators.Length!=2) return new Scroll();

            return await _getEntityById(terminators[0], terminators[1], editionId);
            
        }
        
        public async Task<Scroll> GetFragmentById(uint fragmentId, uint editionId)
        {
            var terminators = _getTerminators(
                TextRetrieval.GetFragmentTerminatorsQuery,
                fragmentId,
                editionId);

            if (terminators.Length!=2) return new Scroll();

           return await _getEntityById(terminators[0], terminators[1], editionId);
            
        }

        public async Task<uint[]> GetLineIds(uint fragmentId, uint editionId)
        {
            using (var connection = OpenConnection())
            {
                var ids = await connection.QueryAsync<uint>(
                    TextRetrieval.GetLineIdsQuery,
                    param: new {fragmentId = fragmentId, editionId = editionId}
                );
                    connection.Close();
                    return ids.ToArray();
            }
        }

        public async Task<uint[]> GetFragmentIds(uint editionId)
        {
            using (var connection = OpenConnection())
            {
                var ids = await connection.QueryAsync<uint>(
                    TextRetrieval.GetFragmentIdsQuery,
                    param: new {editionId = editionId}
                );
                connection.Close();
                return ids.ToArray();
            }
        }


        private uint[] _getTerminators(string query, uint entityId, uint editionId)
        {
            uint[] terminators;
            using (var connection = OpenConnection())
            {
                terminators = (connection.Query<uint>(
                    query,
                    param: new {entityId = entityId, editionId = editionId})).ToArray();
                connection.Close();
            }

            return terminators;


        }
        
        private async Task<Scroll> _getEntityById(uint startId, uint endId, uint editionId)
        {
            Scroll lastScroll = null;
            Fragment lastFragment = null;
            Line lastLine = null;
            Sign lastSign = null;
            SignChar lastChar = null;
            CharAttribute lastCharAttribute = null;
            
            
            using (var connection = OpenConnection())
            {
                
                var scrolls = await connection.QueryAsync<Scroll, Fragment, Line, Sign, SignChar, CharAttribute, Scroll>(
                    TextRetrieval.GetTextChunkQuery,
                    map: (scroll, fragment, line, sign, signChar, charAttribute) =>
                    {
                        var newScroll = scroll.scrollId != lastScroll?.scrollId;

                        if (newScroll)
                        {
                            lastScroll = scroll;
                        }

                        if (fragment.fragmentId != lastFragment?.fragmentId)

                            lastScroll = scroll.scrollId == lastScroll?.scrollId ? lastScroll : scroll;
                        if (fragment.fragmentId != lastFragment?.fragmentId)
                        {
                            lastFragment = fragment;
                            lastScroll.fragments.Add(fragment);
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
                    param: new {startId = startId, endId = endId, editionId=editionId},
                    splitOn: "fragmentId, lineId, signId, signCharId, charAttributeId");
                connection.Close();
                return scrolls.AsList()[0];
            }
        }
    }
    
}