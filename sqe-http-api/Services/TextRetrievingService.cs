using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public interface ITextRetrievingService
    {
        Task<Scroll> GetLineById(uint lineId, uint editionId);
        Task<Scroll> GetFragmentByIdAsync(uint fragmentId, uint editionId);
        Task<uint[]> GetLineIds(uint fragmentId, uint editionId);
        Task<TextFragmentListDTO> GetFragmentIdsAsync(uint editionId);
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

             // TODO: make a DTO for this
            public async Task<Scroll> GetFragmentByIdAsync(uint fragmentId, uint editionId)
            {
                var edition = await _repo.GetTextFragmentByIdAsync(fragmentId, editionId);
                if (edition.scrollId==0) // TODO: describe missing data better here.
                    throw new StandardErrors.DataNotFound("text fragment", fragmentId, "col_id");
                return edition;
            }

            public async Task<uint[]> GetLineIds(uint fragmentId, uint editionId)
            {
                var lineIds = await _repo.GetLineIds(fragmentId, editionId);
                return lineIds;
            }

            public async Task<TextFragmentListDTO> GetFragmentIdsAsync(uint editionId)
            {
                return new TextFragmentListDTO(
                    (await _repo.GetFragmentIds(editionId))
                        .Select(x => new TextFragmentDTO(x.ColId, x.ColName)).ToList()
                    );
            }
        }
    }
