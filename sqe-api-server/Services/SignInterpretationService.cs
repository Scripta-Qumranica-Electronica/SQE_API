using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
        Task<AttributeDTO> CreateEditionAttributeAsync(
            UserInfo user,
            CreateAttributeDTO newAttribute,
            string clientId = null);
        Task<AttributeDTO> UpdateEditionAttributeAsync(
            UserInfo user,
            uint attributeId,
            UpdateAttributeDTO updatedAttribute,
            string clientId = null);
        Task<NoContentResult> DeleteEditionAttributeAsync(
            UserInfo user,
            uint attributeId,
            string clientId = null);
        Task<SignInterpretationDTO> GetEditionSignInterpretationAsync(
            UserInfo user,
            uint signInterpretationId);
        Task<SignInterpretationDTO> CreateOrUpdateSignInterpretationCommentaryAsync(
            UserInfo user,
            uint signInterpretationId,
            CommentaryCreateDTO commentary,
            string clientId = null);
        Task<SignInterpretationDTO> CreateSignInterpretationAttributeAsync(
            UserInfo user,
            uint signInterpretationId,
            InterpretationAttributeCreateDTO attribute,
            string clientId = null);
        Task<SignInterpretationDTO> UpdateSignInterpretationAttributeAsync(
            UserInfo user,
            uint signInterpretationId,
            uint attributeValueId,
            InterpretationAttributeCreateDTO attribute,
            string clientId = null);
        Task<NoContentResult> DeleteSignInterpretationAttributeAsync(
            UserInfo user,
            uint signInterpretationAttributeId,
            uint attributeValueId,
            string clientId = null);
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

        public async Task<AttributeDTO> CreateEditionAttributeAsync(UserInfo user, CreateAttributeDTO newAttribute,
            string clientId = null)
        {
            var newAttributeId = await _attributeRepository.CreateEditionAttribute(
                user,
                newAttribute.attributeName,
                newAttribute.description,
                newAttribute.values.Select(x => new SignInterpretationAttributeValueInput()
                {
                    AttributeStringValue = x.value,
                    AttributeStringValueDescription = x.description,
                    Css = x.cssDirectives,
                }));

            var createdAttribute = (await _attributeRepository.GetEditionAttributeAsync(user, newAttributeId)).ToDTO().attributes.FirstOrDefault();

            // Broadcast the changes
            await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
                .CreatedAttribute(createdAttribute);

            return createdAttribute;
        }

        public async Task<AttributeDTO> UpdateEditionAttributeAsync(UserInfo user, uint attributeId,
            UpdateAttributeDTO updatedAttribute, string clientId = null)
        {
            var updatedAttributeId = await _attributeRepository.UpdateEditionAttribute(
                user,
                attributeId,
                null,
                null,
                updatedAttribute.createValues.Select(x => x.FromDTO()),
                updatedAttribute.updateValues.Select(x => x.FromDTO()),
                updatedAttribute.deleteValues);

            var updatedAttributeDetails = (await _attributeRepository.GetEditionAttributeAsync(user, updatedAttributeId)).ToDTO().attributes.FirstOrDefault();

            if (updatedAttributeId != attributeId) // Broadcast the changes as a create and a delete
            {
                await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
                    .CreatedAttribute(updatedAttributeDetails);
                await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
                    .DeletedAttribute(new DeleteDTO(EditionEntities.attribute, attributeId));

            }
            else // Broadcast the changes as an update
                await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
                    .UpdatedAttribute(updatedAttributeDetails);

            return updatedAttributeDetails;
        }

        public async Task<NoContentResult> DeleteEditionAttributeAsync(UserInfo user, uint attributeId, string clientId = null)
        {
            await _attributeRepository.DeleteEditionAttributeAsync(user, attributeId);

            // Broadcast the changes
            await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
                .DeletedAttribute(new DeleteDTO(EditionEntities.attribute, attributeId));

            return new NoContentResult();
        }

        public async Task<SignInterpretationDTO> GetEditionSignInterpretationAsync(UserInfo user, uint signInterpretationId)
        {
            var signInterpretation = await
                _signInterpretationRepository.GetSignInterpretationById(user, signInterpretationId);

            return signInterpretation.ToDTO();
        }

        public async Task<SignInterpretationDTO> CreateOrUpdateSignInterpretationCommentaryAsync(UserInfo user,
            uint signInterpretationId, CommentaryCreateDTO commentary, string clientId = null)
        {
            await _commentaryRepository.CreateOrUpdateCommentaryAsync(user, signInterpretationId, null,
                commentary.commentary);

            var updatedSignInterpretation = (await
                _signInterpretationRepository.GetSignInterpretationById(user, signInterpretationId)).ToDTO();

            // Broadcast the changes
            await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
                .UpdatedSignInterpretation(updatedSignInterpretation);

            return updatedSignInterpretation;
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
            await _attributeRepository.CreateSignInterpretationAttributesAsync(user, signInterpretationId, createAttribute);

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
            uint signInterpretationId, uint attributeValueId, InterpretationAttributeCreateDTO attribute,
            string clientId = null)
        {
            if (attribute.sequence.HasValue || attribute.value.HasValue)
                await _attributeRepository.UpdateAttributeForSignInterpretationAsync(user, signInterpretationId,
                    attributeValueId, attribute.sequence, attribute.value);

            if (!string.IsNullOrEmpty(attribute.commentary))
                await _commentaryRepository.CreateOrUpdateCommentaryAsync(user, signInterpretationId, attributeValueId,
                    attribute.commentary);

            var updatedSignInterpretation = (await
                _signInterpretationRepository.GetSignInterpretationById(user, signInterpretationId)).ToDTO();

            // Broadcast the changes
            await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
                .UpdatedSignInterpretation(updatedSignInterpretation);

            return updatedSignInterpretation;
        }

        public async Task<NoContentResult> DeleteSignInterpretationAttributeAsync(UserInfo user, uint signInterpretationId, uint attributeValueId, string clientId = null)
        {
            await _attributeRepository.DeleteAttributeFromSignInterpretationAsync(user, signInterpretationId,
                attributeValueId);

            var updatedSignInterpretation = (await
                _signInterpretationRepository.GetSignInterpretationById(user, signInterpretationId)).ToDTO();

            // Broadcast the changes
            await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
                .UpdatedSignInterpretation(updatedSignInterpretation);

            return new NoContentResult();
        }
    }
}