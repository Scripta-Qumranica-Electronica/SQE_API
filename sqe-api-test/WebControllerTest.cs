using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.IO;
using SQE.API.Server;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
	/// <summary>
	///  Fires up the application and provides an HTTP Client to access its controller endpoints.
	/// </summary>
	public partial class WebControllerTest : IClassFixture<WebApplicationFactory<Startup>>
	{
		private static     bool                           _images = true;
		protected readonly HttpClient                     _client;
		private readonly   DatabaseQuery                  _db;
		private readonly   WebApplicationFactory<Startup> _factory;
		private readonly   WKTReader                      _wkr = new WKTReader();

		public WebControllerTest(WebApplicationFactory<Startup> factory)
		{
			var projectDir = Directory.GetCurrentDirectory();

			var configPath =
					Path.Combine(projectDir, "../../../../sqe-api-server/appsettings.json");

			_factory = factory.WithWebHostBuilder(
					builder =>
					{
						// Setting the environment to IntegrationTests will turn off the emailer code.
						builder.UseEnvironment("IntegrationTests");

						builder.ConfigureAppConfiguration(
								(context, conf) => { conf.AddJsonFile(configPath); });
					});

			_client = _factory.CreateClient();
			_db = new DatabaseQuery();
		}

		/// <summary>
		///  Provides a SignalR HubConnection.  The connection will be authorized if a JWT is provided
		/// </summary>
		/// <param name="token">The JWT used to authorize the connection</param>
		/// <returns></returns>
		protected async Task<HubConnection> StartConnectionAsync(string token = null)
		{
			var hubConnection = new HubConnectionBuilder().WithUrl(
																  "ws://localhost/signalr"
																  , o =>
																	{
																		o.HttpMessageHandlerFactory
																				= _ => _factory
																					   .Server
																					   .CreateHandler();

																		if (!string.IsNullOrEmpty(
																				token))
																		{
																			o.AccessTokenProvider =
																					() => Task
																							.FromResult(
																									token);
																		}
																	})
														  .Build();

			await hubConnection.StartAsync();

			return hubConnection;
		}
	}
}
