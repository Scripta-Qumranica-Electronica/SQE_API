using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SQE.SqeHttpApi.DataAccess.Helpers;
using Newtonsoft.Json;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHttpException(this IApplicationBuilder application)
        {
            return application.UseMiddleware<HttpExceptionMiddleware>();
        }
    }

    internal class HttpExceptionMiddleware
    {
        private readonly RequestDelegate next;

        public HttpExceptionMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await this.next.Invoke(context);
            }
            catch (ApiException httpException)
            {
                context.Response.StatusCode = (int) httpException.StatusCode;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(
                    JsonConvert.SerializeObject(
                        new ApiExceptionError(nameof(httpException), httpException.Error))); 
            }
        }

        private class ApiExceptionError
        {
            public string internalErrorName { get; set; }
            public string msg { get; set; }

            public ApiExceptionError(string internalErrorName, string msg)
            {
                this.internalErrorName = internalErrorName;
                this.msg = msg;
            }
        }
    }
}