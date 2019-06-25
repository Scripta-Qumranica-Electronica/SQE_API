using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.Server.Helpers;
using Swashbuckle.AspNetCore.Swagger;

namespace SQE.SqeHttpApi.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
            
            // Run the startup checks to ensure all necessary external services are available.
            StartupChecks.RunAllChecks(configuration, env);
        }

        private IConfiguration Configuration { get; }
        private IHostingEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddCors();

            // configure DI for application services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEditionService, EditionService>();
            services.AddScoped<IImagedObjectService, ImagedObjectService>();
            services.AddScoped<IArtefactService, ArtefactService>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<ITextRetrievingService, TextRetrievingService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IEditionRepository, EditionRepository>();
            services.AddTransient<IImagedObjectRepository, ImagedObjectRepository>();
            services.AddTransient<IImageRepository, ImageRepository>();
            services.AddTransient<IArtefactRepository, ArtefactRepository>();
            services.AddTransient<IDatabaseWriter, DatabaseWriter>();
            services.AddTransient<ITextRetrievalRepository, TextRetrievalRepository>();
            
            // When running integration tests, we do not actually send out emails. This checks ASPNETCORE_ENVIRONMENT
            // and if it is "IntegrationTests", then a Fake for IEmailSender is used instead of the real one.
            Console.WriteLine(Environment.EnvironmentName);
            if (Environment.IsEnvironment("IntegrationTests"))
                services.AddSingleton<IEmailSender, FakeEmailSender>();
            else
                services.AddSingleton<IEmailSender, EmailSender>();

            // Configure routing.
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });

            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            // configure jwt authentication
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddAuthorization(options =>
            {
                var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(
             JwtBearerDefaults.AuthenticationScheme);
                defaultAuthorizationPolicyBuilder =
                    defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
                options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();

            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "SQE API", Version = "v1" });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    Description = "Add JWT Authorization header using the Bearer scheme. Example input: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", new string[]{}}
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
//            app.UseCors(
//                options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
//            );

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SQE API v1");
            });
            
            app.UseHttpException();

            app.UseMvc();
        }
    }
}
