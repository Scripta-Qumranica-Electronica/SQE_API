using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess;
using Serilog;

namespace SQE.API.Server.Services
{
	public interface IUtilService
	{
		WktPolygonDTO            RepairWktPolygonAsync(string wktPolygon, string clientId = null);
		Task<DatabaseVersionDTO> GetDatabaseVersion();
		Task<APIVersionDTO>      GetAPIVersion();
		Task<NoContentResult>	 ReportGithubIssueRequestAsync(GithubIssueReportDTO payload);
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
					, lastUpdated = DateTime.Parse(appSettings.ApiUpdateDate)
					,
			};
		}

		public async Task<NoContentResult> ReportGithubIssueRequestAsync(GithubIssueReportDTO payload)
		{
			var log = Log.ForContext<UtilService>();
			var token = _config.GetConnectionString("GitHubAPIToken");
			var url   = _config.GetConnectionString("GitHubUrl");

			log.Debug("Reporting issue to github");
			log.Verbose("URL: {url}", url);

			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
			httpClient.DefaultRequestHeaders.Add("User-Agent", "request");

			httpClient.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue(
							"Token"
							, token); //token123 is replaced by my token of course

			var json = JsonConvert.SerializeObject(new { title=payload.title, body=$"url: {payload.url}\nUser: {payload.username}\n Report: {payload.comment}" });
			log.Verbose("Payload: {json}", json);

			var data = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await httpClient.PostAsync(url, data);
			var result = response.Content.ReadAsStringAsync().Result;

			if (!response.IsSuccessStatusCode)
			{
				log.Warning("Error returned from github: {result}", result);
			}
			else
			{
				log.Information("Issue reported to github");
			}

			return new NoContentResult(); // We do not notify the user when there's a problem - for now
		}
	}
}
