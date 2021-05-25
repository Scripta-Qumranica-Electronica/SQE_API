using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess;

namespace SQE.API.Server.Services
{
	public interface IUtilService
	{
		WktPolygonDTO            RepairWktPolygonAsync(string wktPolygon, string clientId = null);
		Task<DatabaseVersionDTO> GetDatabaseVersion();
		Task<APIVersionDTO>      GetAPIVersion();
	}

	public class UtilService : IUtilService
	{
		private readonly IConfiguration                   _config;
		private readonly IHubContext<MainHub, ISQEClient> _hubContext;
		private readonly IUserRepository                  _userRepo;

		public UtilService(
				IHubContext<MainHub, ISQEClient> hubContext
				, IUserRepository                userRepo
				, IConfiguration                 config)
		{
			_hubContext = hubContext;
			_userRepo = userRepo;
			_config = config;
		}

		public WktPolygonDTO RepairWktPolygonAsync(string wktPolygon, string clientId = null)
		{
			var repaired = GeometryValidation.ValidatePolygon(wktPolygon, "polygon", true);

			return new WktPolygonDTO { wktPolygon = repaired };
		}

		public async Task<DatabaseVersionDTO> GetDatabaseVersion()
		{
			var versionInfo = await _userRepo.GetDatabaseVersion();

			return new DatabaseVersionDTO
			{
					version = versionInfo.Version
					, lastUpdated = versionInfo.Date
					,
			};
		}

		public async Task<APIVersionDTO> GetAPIVersion()
		{
			var appSettings = _config.GetSection("AppSettings").Get<AppSettings>();

			return new APIVersionDTO
			{
					version = appSettings.ApiVersion
					, lastUpdated = DateTime.Parse(appSettings.ApiUpdateDate),
			};
		}
	}
}
