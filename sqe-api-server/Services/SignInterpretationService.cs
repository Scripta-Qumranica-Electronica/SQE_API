using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Serialization;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
    public interface ISignInterpretationService
    {
        Task<AttributeListDTO> GetEditionSignInterpretationAttributesAsync(UserInfo user);
        Task DeleteSignInterpretationAttributeAsync(UserInfo user, uint signInterpretationAttributeId);
    }

    public class SignInterpretationService : ISignInterpretationService
    {
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;
        private readonly IAttributeRepository _attributeRepository;


        public SignInterpretationService(IHubContext<MainHub, ISQEClient> hubContext, IAttributeRepository attributeRepository)
        {
            _hubContext = hubContext;
            _attributeRepository = attributeRepository;
        }

        public async Task<AttributeListDTO> GetEditionSignInterpretationAttributesAsync(UserInfo user)
        {
            return (await _attributeRepository.GetAllEditionAttributesAsync(user)).ToDTO();
        }

        public Task DeleteSignInterpretationAttributeAsync(UserInfo user, uint signInterpretationAttributeId)
        {
            return null;
        }
    }
}