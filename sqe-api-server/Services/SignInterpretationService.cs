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
        Task<SignInterpretationDTO> GetEditionSignInterpretationAsync(UserInfo user, uint signInterpretationId);
        Task<SignInterpretationDTO> CreateSignInterpretationAttributeAsync(UserInfo user, uint signInterpretationId, InterpretationAttributeCreateDTO attribute, string clientId = null);
        Task<SignInterpretationDTO> UpdateSignInterpretationAttributeAsync(UserInfo user, uint signInterpretationAttributeId, InterpretationAttributeCreateDTO attribute, string clientId = null);
        Task DeleteSignInterpretationAttributeAsync(UserInfo user, uint signInterpretationAttributeId, string clientId = null);
    }

    public class SignInterpretationService : ISignInterpretationService
    {
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;
        private readonly IAttributeRepository _attributeRepository;
        private readonly ISignInterpretationRepository _signInterpretationRepository;
        private readonly ISignInterpretationCommentaryRepository _commentaryRepository;


        public SignInterpretationService(
            IHubContext<MainHub, ISQEClient> hubContext,
            IAttributeRepository attributeRepository,
            ISignInterpretationRepository signInterpretationRepository,
            ISignInterpretationCommentaryRepository commentaryRepository)
        {
            _hubContext = hubContext;
            _attributeRepository = attributeRepository;
            _signInterpretationRepository = signInterpretationRepository;
            _commentaryRepository = commentaryRepository;
        }

        public async Task<AttributeListDTO> GetEditionSignInterpretationAttributesAsync(UserInfo user)
        {
            return (await _attributeRepository.GetAllEditionAttributesAsync(user)).ToDTO();
        }

        public async Task<SignInterpretationDTO> GetEditionSignInterpretationAsync(UserInfo user, uint signInterpretationId)
        {
            var signInterpretation = await
                _signInterpretationRepository.GetSignInterpretationById(user, signInterpretationId);

            return signInterpretation.ToDTO();
        }

        public async Task<SignInterpretationDTO> CreateSignInterpretationAttributeAsync(UserInfo user,
            uint signInterpretationId, InterpretationAttributeCreateDTO attribute, string clientId = null)
        {
            var createAttribute = new SignInterpretationAttributeData()
            {
                AttributeValueId = attribute.attributeValueId,
                NumericValue = attribute.value,
                Sequence = attribute.sequence,
            };
            await _attributeRepository.CreateAttributesAsync(user, signInterpretationId, createAttribute);

            if (!string.IsNullOrEmpty(attribute.commentary))
            {
                var commentary = new SignInterpretationCommentaryData()
                {
                    AttributeId = attribute.attributeId,
                    Commentary = attribute.commentary,
                };
                await _commentaryRepository.CreateCommentaryAsync(user, signInterpretationId, commentary);
            }

            var updatedSignInterpretation = (await
                _signInterpretationRepository.GetSignInterpretationById(user, signInterpretationId)).ToDTO();

            // Broadcast the changes
            await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
                .UpdatedSignInterpretation(updatedSignInterpretation);

            return updatedSignInterpretation;
        }

        public async Task<SignInterpretationDTO> UpdateSignInterpretationAttributeAsync(UserInfo user,
            uint signInterpretationAttributeId, InterpretationAttributeCreateDTO attribute, string clientId = null)
        {
            return null;
        }

        public Task DeleteSignInterpretationAttributeAsync(UserInfo user, uint signInterpretationAttributeId, string clientId = null)
        {
            return null;
        }
    }
}