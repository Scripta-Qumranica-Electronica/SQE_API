using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SQE.API.Server.Helpers;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Services;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.API.Server
{
	public class Startup
	{
		public Startup(IConfiguration configuration, IWebHostEnvironment env)
		{
			Configuration = configuration;
			Environment = env;

			// Run the startup checks to ensure all necessary external services are available.
			StartupChecks.RunAllChecks(configuration, env);
		}

		private IConfiguration      Configuration { get; }
		private IWebHostEnvironment Environment   { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddCors();

			services.AddHttpClient<IWordService, WordService>(Options.DefaultName)
					.ConfigurePrimaryHttpMessageHandler(
							() => new HttpClientHandler
							{
									ClientCertificateOptions =
											ClientCertificateOption.Manual
									, ServerCertificateCustomValidationCallback =
									(
											httpRequestMessage
											, cert
											, certChain
											, policyErrors) => true
									,
							});

			// configure DI for application services
			services.AddScoped<IUserService, UserService>();
			services.AddScoped<IEditionService, EditionService>();
			services.AddScoped<IImagedObjectService, ImagedObjectService>();
			services.AddScoped<IArtefactService, ArtefactService>();
			services.AddScoped<IImageService, ImageService>();
			services.AddScoped<ITextService, TextService>();
			services.AddScoped<IRoiService, RoiService>();
			services.AddScoped<IUtilService, UtilService>();
			services.AddScoped<ICatalogService, CatalogService>();
			services.AddScoped<ISearchService, SearchService>();
			services.AddScoped<IScriptService, ScriptService>();
			services.AddScoped<ISignInterpretationService, SignInterpretationService>();
			services.AddTransient<IWordService, WordService>();

			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			services.AddTransient<IUserRepository, UserRepository>();
			services.AddTransient<IEditionRepository, EditionRepository>();

			services.AddTransient<IImagedObjectRepository, ImagedObjectRepository>();

			services.AddTransient<IImageRepository, ImageRepository>();
			services.AddTransient<IArtefactRepository, ArtefactRepository>();
			services.AddTransient<IDatabaseWriter, DatabaseWriter>();
			services.AddTransient<ITextRepository, TextRepository>();
			services.AddTransient<IRoiRepository, RoiRepository>();

			services.AddTransient<ISignInterpretationRepository, SignInterpretationRepository>();

			services
					.AddTransient<ISignInterpretationCommentaryRepository,
							SignInterpretationCommentaryRepository>();

			services.AddTransient<IAttributeRepository, AttributeRepository>();
			services.AddTransient<ICatalogueRepository, CatalogueRepository>();

			services
					.AddTransient<ISignStreamMaterializationRepository,
							SignStreamMaterializationRepository>();

			services.AddTransient<ISearchRepository, SearchRepository>();
			services.AddTransient<IScriptRepository, ScriptRepository>();

			services.AddResponseCompression();

			services.Configure<BrotliCompressionProviderOptions>(
					options =>
					{
						// A custom compression level makes a huge difference CompressionLevel.Optimal uses level 11,
						// which is incredibly slow.  A level between 5–7 gives a similarly sized result at a considerably
						// faster speed. On one test Broti (CompressionLevel)5 compressed a 9.4 MB file to 1.57 MB, gzip
						// compressed it to 2.45 MB (albeit a little bit faster).
						options.Level = (CompressionLevel) 5;
					});

			services.Configure<GzipCompressionProviderOptions>(
					options => { options.Level = CompressionLevel.Optimal; });

			// When running integration tests, we do not actually send out emails. This checks ASPNETCORE_ENVIRONMENT
			// and if it is "IntegrationTests", then a Fake for IEmailSender is used instead of the real one.
			if (Environment.IsEnvironment("IntegrationTests"))
				services.AddSingleton<IEmailSender, FakeEmailSender>();
			else
				services.AddSingleton<IEmailSender, EmailSender>();

			// Configure routing.
			services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

			// configure strongly typed settings objects
			var appSettingsSection = Configuration.GetSection("AppSettings");
			services.Configure<AppSettings>(appSettingsSection);

			// configure jwt authentication
			var appSettings = appSettingsSection.Get<AppSettings>();
			var key = Encoding.ASCII.GetBytes(appSettings.Secret);

			services.AddAuthentication(
							x =>
							{
								x.DefaultAuthenticateScheme =
										JwtBearerDefaults.AuthenticationScheme;

								x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
							})
					.AddJwtBearer(
							x =>
							{
								x.RequireHttpsMetadata = false;
								x.SaveToken = true;

								x.TokenValidationParameters = new TokenValidationParameters
								{
										ValidateIssuerSigningKey = true
										, IssuerSigningKey = new SymmetricSecurityKey(key)
										, ValidateIssuer = false
										, ValidateAudience = false
										,
								};

								// From https://docs.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-2.2
								// We have to hook the OnMessageReceived event in order to
								// allow the JWT authentication handler to read the access
								// token from the query string when a WebSocket or
								// Server-Sent Events request comes in.
								x.Events = new JwtBearerEvents
								{
										OnMessageReceived = context =>
															{
																var accessToken =
																		context.Request.Query[
																				"access_token"];

																// If the request is for our hub...
																var path = context.HttpContext
																				  .Request.Path;

																if (!string.IsNullOrEmpty(
																			accessToken)
																	&& path.StartsWithSegments(
																			"/signalr"))

																		// Read the token out of the query string
																	context.Token = accessToken;

																return Task.CompletedTask;
															}
										,
								};
							});

			services.AddAuthorization(
					options =>
					{
						var defaultAuthorizationPolicyBuilder =
								new AuthorizationPolicyBuilder(
										JwtBearerDefaults.AuthenticationScheme);

						defaultAuthorizationPolicyBuilder = defaultAuthorizationPolicyBuilder
								.RequireAuthenticatedUser();

						options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
					});

			if (appSettings.HttpServer.ToLower() == "true")
			{
				services.AddSwaggerGen(
						c =>
						{
							c.SwaggerDoc(
									"v1"
									, new OpenApiInfo
									{
											Title = "SQE API"
											, Version = "v1"
											,
									});

							var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

							var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

							c.IncludeXmlComments(xmlPath);

							c.AddSecurityDefinition(
									"Bearer"
									, new OpenApiSecurityScheme
									{
											Description =
													"Add JWT Authorization header using the Bearer scheme. Example input: \"Bearer {token}\""
											, Name = "Authorization"
											, In = ParameterLocation.Header
											, Type = SecuritySchemeType.ApiKey
											,
									});

							c.AddSecurityRequirement(
									new OpenApiSecurityRequirement
									{
											{
													new OpenApiSecurityScheme
													{
															Reference =
																	new OpenApiReference
																	{
																			Type =
																					ReferenceType
																							.SecurityScheme
																			, Id =
																					"Bearer"
																			,
																	}
															, Scheme = "apikey"
															, Name = "Bearer"
															, In = ParameterLocation.Header
															,
													}
													, new List<string>()
											}
											,
									});
						});

				services.AddControllers();
			}

			// Add a Redis backplane if we need to scale horizontally.
			// You must set sticky sessions in the load balancer.
			// Remember that the docs warn to only use a Redis backplane in the same network as the SignalR Hubs,
			// otherwise you may experience latency issues.
			if ((appSettings.UseRedis.ToLower() == "true")
				&& !Environment.IsEnvironment("IntegrationTests"))
			{
				if (string.IsNullOrEmpty(Configuration.GetConnectionString("RedisHost")))
					throw new Exception("Must set a value in appsettings.json for RedisHost");

				if (string.IsNullOrEmpty(Configuration.GetConnectionString("RedisPort")))
					throw new Exception("Must set a value in appsettings.json for RedisPort");

				if (string.IsNullOrEmpty(Configuration.GetConnectionString("RedisPassword")))
					throw new Exception("Must set a value in appsettings.json for RedisPassword");

				var redisHost = Configuration.GetConnectionString("RedisHost");
				var redisPort = Configuration.GetConnectionString("RedisPort");

				var redisPassword = Configuration.GetConnectionString("RedisPassword");

				var redisConn =
						$"{redisHost}:{redisPort},password={redisPassword},ssl=False,abortConnect=False";

				services.AddSignalR(o => o.EnableDetailedErrors = true).AddRedis(redisConn);
			}
			else
				services.AddSignalR(o => o.EnableDetailedErrors = true);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app)
		{
			app.UseMiddleware<RequestDiagnosticsMiddleware>();

			if (Environment.IsDevelopment())
				app.UseDeveloperExceptionPage();

			// TODO: when we know the deployment details we should reassess the use of compression here.
			app.UseResponseCompression();

			app.UseSerilogRequestLogging();

			app.UseCors(
					builder => builder.AllowAnyHeader()
									  .AllowAnyMethod()
									  .SetIsOriginAllowed(host => true)
									  .AllowCredentials());

			app.UseHttpException();

			// Get app settings
			var appSettingsSection = Configuration.GetSection("AppSettings");
			var appSettings = appSettingsSection.Get<AppSettings>();

			if (appSettings.HttpServer.ToLower() == "true")
			{
				// Enable middleware to serve generated Swagger as a JSON endpoint.
				app.UseSwagger();

				// Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
				// specifying the Swagger JSON endpoint.
				app.UseSwaggerUI(
						c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "SQE API v1"); });
			}

			app.UseRouting();

			app.UseAuthorization();
			app.UseAuthentication();

			app.UseEndpoints(
					endpoints =>
					{
						endpoints.MapHub<MainHub>("/signalr");

						if (appSettings.HttpServer.ToLower() == "true")
							endpoints.MapControllers();
					});
		}
	}

	public class RequestDiagnosticsMiddleware
	{
		private readonly RequestDelegate _next;

		public RequestDiagnosticsMiddleware(RequestDelegate next) => _next = next;

		public async Task InvokeAsync(HttpContext context)
		{
			// I don't yet called Controller/Action.
			//log the essential parts of the request here

			// Call the next delegate/middleware in the pipeline
			await _next(context);
		}
	}
}
