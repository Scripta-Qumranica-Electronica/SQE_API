using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Models;

namespace SQE.SqeHttpApi.Server.Services
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
                if (scroll.scrollId==0) throw new LineNotFoundException(lineId, editionId);
                return scroll;
            }

            public async Task<Scroll> GetFragmentById(uint fragmentId, uint editionId)
            {
                var scroll = await _repo.GetFragmentById(fragmentId, editionId);
                if (scroll.scrollId==0) throw new FragmentNotFoundException(fragmentId, editionId);
                return scroll;
            }

            public async Task<uint[]> GetLineIds(uint fragmentId, uint editionId)
            {
                var lineIds = _repo.GetLineIds(fragmentId, editionId);
                return lineIds.Result;
            }

            public async Task<uint[]> GetFragmentIds (uint editionId)
            {
                var fragmentIds = _repo.GetFragmentIds(editionId);
                return fragmentIds.Result;
            }
        }
    }
