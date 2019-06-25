using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public interface ITextRetrievingService
    {
        Task<Scroll> GetLineById(uint lineId, uint editionId);
        Task<Scroll> GetFragmentById(uint fragmentId, uint editionId);
        Task<uint[]> GetLineIds(uint fragmentId, uint editionId);
        Task<uint[]> GetFragmentIds(uint editionId);
    }
    public class TextRetrievingService : ITextRetrievingService
        {
            private ITextRetrievalRepository _repo;
        
        
            public TextRetrievingService(ITextRetrievalRepository repo)
            {
                _repo = repo;
            }

            
            
             public async Task<Scroll> GetLineById(uint lineId, uint editionId)
            {
                var scroll = await _repo.GetLineById(lineId, editionId);
                if (scroll.scrollId==0) 
                    throw new StandardErrors.DataNotFound("line", lineId, "line_id");
                return scroll;
            }

            public async Task<Scroll> GetFragmentById(uint fragmentId, uint editionId)
            {
                var scroll = await _repo.GetFragmentById(fragmentId, editionId);
                if (scroll.scrollId==0) // TODO: describe missing data better here.
                    throw new StandardErrors.DataNotFound("text fragment", fragmentId, "col_id");
                return scroll;
            }

            public async Task<uint[]> GetLineIds(uint fragmentId, uint editionId)
            {
                var lineIds = await _repo.GetLineIds(fragmentId, editionId);
                return lineIds;
            }

            public async Task<uint[]> GetFragmentIds (uint editionId)
            {
                var fragmentIds = await _repo.GetFragmentIds(editionId);
                return fragmentIds;
            }
        }
    }
