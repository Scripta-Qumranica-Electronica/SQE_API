using System;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.ApiTest.ApiRequests;
using Xunit;

namespace SQE.ApiTest.Helpers
{
    public static class Request
    {
        public static async Task<(HttpResponseMessage, Toutput)> Send<Tinput, Toutput>(
            RequestObject<Tinput, Toutput> request,
            HttpClient http = null,
            HubConnection realtime = null,
            HubConnection listener = null, // TODO
            bool auth = false,
            string jwt = null,
            bool shouldSucceed = true)
        {
            HttpResponseMessage resp = null;
            Toutput msg = default(Toutput);
            Toutput rt = default(Toutput);
            Toutput lt = default(Toutput);

            if (listener != null && request.requestVerb != HttpMethod.Get)
            {
                // Set up listener

            }
            if (http != null)
            {
                var httpObj = request.GetHttpResponseObject();
                (resp, msg) = await HttpRequest.SendAsync<Tinput, Toutput>(
                    http,
                    httpObj.requestVerb,
                    httpObj.requestString,
                    httpObj.payload,
                    jwt
                );
                if (shouldSucceed)
                    resp.EnsureSuccessStatusCode();
            }

            if (realtime != null)
            {
                try
                {
                    rt = await request.signalrRequest()(realtime);
                }
                catch (Exception e)
                {
                    if (shouldSucceed)
                        throw e;
                }

                if (shouldSucceed && http != null)
                    rt.ShouldDeepEqual(msg);
            }

            if (listener == null || request.requestVerb == HttpMethod.Get)
            {
                return http != null ? (resp, msg) : (resp, rt);
            }
            else
            {
                if (shouldSucceed)
                {
                    var waitTime = 0;
                    while (lt == null && waitTime < 20)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        waitTime += 1;
                    }
                    Assert.NotNull(lt); // Do not try to listen with requests that return void!
                }
                return (resp, lt);
            }
        }
    }
}