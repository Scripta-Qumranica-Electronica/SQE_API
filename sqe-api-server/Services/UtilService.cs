using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Net.Security;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SQE.API.Server.Services
{	
	public interface IUtilService
	{
		WktPolygonDTO            RepairWktPolygonAsync(string wktPolygon, string clientId = null);
		Task<DatabaseVersionDTO> GetDatabaseVersion();
		Task<APIVersionDTO>      GetAPIVersion();
		Task<NoContentResult>	 ReportGithubIssueRequestAsync(string title, string body);
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

		public async Task<NoContentResult> ReportGithubIssueRequestAsync(string title, string body)
		{
			//var user  = "RidgeBatty";
			//var repo  = "Testing";
			//var token = "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";
			//var url = $"https://api.github.com/repos/{user}/{repo}/issues";//Paste ur url here
			var token = _config.GetConnectionString("GitToken");
			var url   = _config.GetConnectionString("GitURL");

			Debug.WriteLine("");
			Debug.WriteLine(url);

			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
			httpClient.DefaultRequestHeaders.Add("User-Agent", "request");
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token); //token123 is replaced by my token of course

			Debug.WriteLine(httpClient.DefaultRequestHeaders);

			var json = JsonConvert.SerializeObject(new { title=title, body=body });
			var data = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await httpClient.PostAsync(url, data);
			var result = response.Content.ReadAsStringAsync().Result;

			Debug.WriteLine(result);

			return new NoContentResult();
		}
	}
}
