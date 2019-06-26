using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public interface ITextService
    {
        Task<TextEdition> GetLineByIdAsync(UserInfo user, uint lineId);
        Task<TextEdition> GetFragmentByIdAsync(UserInfo user, uint fragmentId);
        Task<LineDataListDTO> GetLineIdsAsync(UserInfo user, uint fragmentId);
        Task<TextFragmentListDTO> GetFragmentIdsAsync(UserInfo user);
    }
    public class TextService : ITextService
        {
            private ITextRepository _repo;
        
        
            public TextService(ITextRepository repo)
            {
                _repo = repo;
            }
            
            // TODO: make a DTO for this
             public async Task<TextEdition> GetLineByIdAsync(UserInfo user, uint lineId)
            {
                var scroll = await _repo.GetLineById(user, lineId);
                if (scroll.scrollId==0) 
                    throw new StandardErrors.DataNotFound("line", lineId, "line_id");
                return scroll;
            }

             // TODO: make a DTO for this
            public async Task<TextEdition> GetFragmentByIdAsync(UserInfo user, uint fragmentId)
            {
                var edition = await _repo.GetTextFragmentByIdAsync(user, fragmentId);
                if (edition.scrollId==0) // TODO: describe missing data better here.
                    throw new StandardErrors.DataNotFound("text fragment", fragmentId, "col_id");
                return edition;
            }

            public async Task<LineDataListDTO> GetLineIdsAsync(UserInfo user, uint fragmentId)
            {
                return new LineDataListDTO((await _repo.GetLineIdsAsync(user, fragmentId))
                    .Select(x => new LineDataDTO(x.lineId, x.lineName)).ToList());
            }

            public async Task<TextFragmentListDTO> GetFragmentIdsAsync(UserInfo user)
            {
                return new TextFragmentListDTO(
                    (await _repo.GetFragmentIds(user))
                        .Select(x => new TextFragmentDTO(x.ColId, x.ColName)).ToList()
                    );
            }
        }
    }
