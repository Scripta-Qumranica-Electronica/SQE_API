using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
    public interface ISignInterpretationService
    {
        Task DeleteSignInterpretationAttributeAsync(UserInfo user, uint signInterpretationAttributeId);
    }

    public class SignInterpretationService : ISignInterpretationService
    {
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;

        public SignInterpretationService(IHubContext<MainHub, ISQEClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task DeleteSignInterpretationAttributeAsync(UserInfo user, uint signInterpretationAttributeId)
        {
            return null;
        }
    }
}