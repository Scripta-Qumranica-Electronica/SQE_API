using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using sqe_signalr_api.Hubs;
using sqe_signalr_api.Services;

namespace sqe_signalr_api
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            
            // Add a Redis backplane if we need to scale horizontally.
            // You must set sticky sessions in the load balancer.
            var redisBackplane = Environment.GetEnvironmentVariable("USE_REDIS") ?? "false";
            if (redisBackplane == "true")
            {
                var redisServer = Environment.GetEnvironmentVariable("REDIS_SERVER") ?? "localhost";
                var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
                var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? "sqesecret";
                var redisConn = $"{redisServer}:{redisPort},password={redisPassword},ssl=False,abortConnect=False";
                services.AddSignalR().AddRedis(redisConn);
            }
            else
            {
                services.AddSignalR();
            }
            
            
            services.AddScoped<IHttpService, HttpService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // TODO use reflection to directly connect to the sqe-http-api code rather than making http calls to it
            // If we import the sqe-http-api project into this one, then we can use reflection to find the
            // controller endpoints and call them directly from the SignalR Hub.  This code would not go here,
            // but rather in a separate service that runs once on startup.
            /*var controllers = Assembly.Load("sqe-http-api")
                .GetTypes()
                .Where(x => x.Namespace == "SQE.SqeHttpApi.Server.Controllers" 
                            && x.BaseType.FullName == "Microsoft.AspNetCore.Mvc.ControllerBase").ToList();*/
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            app.UseCors(builder => builder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed((host) => true)
                .AllowCredentials());
            app.UseFileServer();

            app.UseSignalR(routes =>
            {
                routes.MapHub<MainHub>("/api");
            });
        }
    }
}