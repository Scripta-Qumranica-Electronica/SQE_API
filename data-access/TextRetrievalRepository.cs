using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{

    public interface ITextRetrievalRepository
    {
        Task<Scroll> GetLineById(uint scrollVersionGroupId, uint lineId);
        Task<Scroll> GetFragmentById(uint scrollVersionGroupId, uint fragmentId);
    }

    public class TextRetrievalRepository : DBConnectionBase, ITextRetrievalRepository
    {
        public TextRetrievalRepository(IConfiguration config) : base(config) { }

        public async Task<Scroll> GetLineById(uint scrollVersionGroupId, uint lineId)
        {
            return await _getEntityById(scrollVersionGroupId, lineId, TextRetrieval.GetLineTextByIdQuery);
            
        }
        
        public async Task<Scroll> GetFragmentById(uint scrollVersionGroupId, uint fragmentId)
        {
            return await _getEntityById(scrollVersionGroupId, fragmentId, TextRetrieval.GetFragmentTextByIdQuery);
            
        }
        
        private async Task<Scroll> _getEntityById(uint scrollVersionId, uint entityId, string query)
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
                    query,
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

                        if (charAttribute.charAttributeId == lastCharAttribute?.charAttributeId) return lastScroll;
                        lastCharAttribute = charAttribute;
                        lastChar.attributes.Add(charAttribute);


                        return newScroll ? scroll : null;
                    },
                    param: new {EntityId = entityId, ScrollVersionGroupId = scrollVersionId},
                    splitOn: "fragmentId, lineId, signId, signCharId, charAttributeId");
                return scrolls.AsList()[0];
            }
        }
    }
    
}