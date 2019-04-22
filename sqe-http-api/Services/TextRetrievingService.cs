using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Models;

namespace SQE.SqeHttpApi.Server.Services
{
    public interface ITextRetrievingService
    {
        Task<Scroll> GetLineById(uint scrollVersionGroupId, uint lineId);
        Task<Scroll> GetFragmentById(uint scrollVersionGroupId, uint fragmentId);
    }
    public class TextRetrievingService : ITextRetrievingService
        {
            private ITextRetrievalRepository _repo;
        
        
            public TextRetrievingService(ITextRetrievalRepository repo)
            {
                _repo = repo;
            }

            
            
             public async Task<Scroll> GetLineById(uint scrollVersionGroupId, uint lineId)
            {
                var scroll = await _repo.GetLineById(scrollVersionGroupId, lineId);
                return scroll;
            }

            public async Task<Scroll> GetFragmentById(uint scrollVersionGroupId, uint fragmentId)
            {
                var scroll = await _repo.GetFragmentById(scrollVersionGroupId, fragmentId);
                return scroll;
            }
        }
    }
