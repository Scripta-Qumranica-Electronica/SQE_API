using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace SQE.SqeHttpApi.Server.Services
{
    public interface IBroadcastService
    {
        Task Broadcast(uint editionId, string message);
    }

    public class BroadcastService : IBroadcastService
    {
        private readonly HubConnection _hubConnection;

        private readonly string secret;
        private readonly IHubProxy _api;
        private readonly bool activated;
        public BroadcastService()
        {
            activated = false;
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HTTP_SIGNALR_SECRET"))
                && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SIGNALR_ADDRESS")))
            {
                secret = Environment.GetEnvironmentVariable("HTTP_SIGNALR_SECRET");
                _hubConnection = new HubConnection(Environment.GetEnvironmentVariable("SIGNALR_ADDRESS"));
                _api = _hubConnection.CreateHubProxy("api");
                _hubConnection.Start();
                activated = true;
            }
        }

        public async Task Broadcast(uint editionId, string message)
        {
            if (activated)
                await _api.Invoke("HttpBroadcast", new {secret, editionId, message});
        }
    }
}