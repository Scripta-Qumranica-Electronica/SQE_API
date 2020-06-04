using System.Collections;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SQE.DatabaseAccess.Helpers;

namespace SQE.API.Server.Helpers
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
                await next.Invoke(context);
            }
            catch (ApiException httpException)
            {
                context.Response.StatusCode = (int)httpException.StatusCode;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(
                    JsonConvert.SerializeObject(
                        new ApiExceptionError(nameof(httpException), httpException.Error,
                            httpException is IExceptionWithData exceptionWithData
                                ? exceptionWithData.CustomReturnedData
                                : null)
                    )
                );
            }
        }

        public class ApiExceptionError
        {
            public ApiExceptionError(string internalErrorName, string msg, IDictionary data)
            {
                this.internalErrorName = internalErrorName;
                this.msg = msg;
                this.data = data;
            }

            public string internalErrorName { get; }
            public string msg { get; }
            public IDictionary data { get; }
        }
    }
}