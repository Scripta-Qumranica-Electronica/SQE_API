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
            Func<string, Task<HubConnection>> realtime = null,
            bool listener = false,
            bool auth = false,
            string jwt = null,
            bool shouldSucceed = true)
        {
            HttpResponseMessage resp = null;
            Toutput msg = default(Toutput);
            Toutput rt = default(Toutput);
            Toutput lt = default(Toutput);
            HubConnection signalrListener;

            if (listener
                && request.requestVerb != HttpMethod.Get
                && realtime != null
                && !string.IsNullOrEmpty(request.listenerMethod)
                && request.GetType().IsSubclassOf(typeof(EditionRequestObject<Tinput, Toutput>)))
            {
                var editionRequest = request as EditionRequestObject<Tinput, Toutput>;
                signalrListener = await realtime(auth ? jwt : null);
                await signalrListener.InvokeAsync("SubscribeToEdition", editionRequest?.editionId);
                signalrListener.On<Toutput>(request.listenerMethod, (receivedData) => lt = receivedData);
            }

            if (http != null)
            {
                var httpObj = request.GetHttpResponseObject();
                (resp, msg) = await HttpRequest.SendAsync<Tinput, Toutput>(
                    http,
                    httpObj.requestVerb,
                    httpObj.requestString,
                    httpObj.payload,
                    auth ? jwt : null
                );
                if (shouldSucceed)
                    resp.EnsureSuccessStatusCode();
            }

            if (realtime != null)
            {
                try
                {
                    var signalr = await realtime(auth ? jwt : null);
                    rt = await request.signalrRequest<Toutput>()(signalr);
                }
                catch (Exception e)
                {
                    if (shouldSucceed)
                        throw e;
                }

                if (shouldSucceed && http != null)
                    rt.ShouldDeepEqual(msg);
            }

            if (!listener || request.requestVerb == HttpMethod.Get)
            {
                return (resp, http != null ? msg : rt);
            }

            var waitTime = 0;
            while (lt == null && waitTime < 20)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                waitTime += 1;
            }
            Assert.NotNull(lt); // Do not try to listen with requests that return void!
            return (resp, lt);
        }
    }
}