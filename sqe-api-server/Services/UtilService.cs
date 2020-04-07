using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Helpers;

namespace SQE.API.Server.Services
{
    public interface IUtilService
    {
        Task<WktPolygonDTO> RepairWktPolygonAsync(string wktPolygon, string clientId = null);
    }

    public class UtilService : IUtilService
    {
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;

        public UtilService(IHubContext<MainHub, ISQEClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task<WktPolygonDTO> RepairWktPolygonAsync(string wktPolygon, string clientId = null)
        {
            var repaired = await GeometryValidation.ValidatePolygonAsync(wktPolygon, "polygon", true);
            return new WktPolygonDTO() { wktPolygon = repaired };
        }
    }
}