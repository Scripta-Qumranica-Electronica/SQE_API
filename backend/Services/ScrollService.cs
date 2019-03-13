using SQE.Backend.DataAccess;
using SQE.Backend.Server.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.Services
{
    public interface IScrollService
    {
        Task<ScrollVersion> GetScrollVersionAsync(int scrollId, int? userId, bool artefacts=false, bool fragments=false);
    }

    public class ScrollService: IScrollService
    {
        IScrollRepository _repo;

        public ScrollService(IScrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<ScrollVersion> GetScrollVersionAsync(int scrollId, int? userId, bool artefacts, bool fragments)
        {
            var scrollModel = _repo.ListScrollVersions(userId, new List<int> { scrollId });
            return null;
        }
    }
}
