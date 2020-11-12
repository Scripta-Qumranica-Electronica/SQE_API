using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Serialization;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
	public interface ISignInterpretationService
	{
		Task<AttributeListDTO> GetEditionSignInterpretationAttributesAsync(UserInfo user);

		Task<AttributeDTO> CreateEditionAttributeAsync(
				UserInfo             user
				, CreateAttributeDTO newAttribute
				, string             clientId = null);

		Task<AttributeDTO> UpdateEditionAttributeAsync(
				UserInfo             user
				, uint               attributeId
				, UpdateAttributeDTO updatedAttribute
				, string             clientId = null);

		Task<NoContentResult> DeleteEditionAttributeAsync(
				UserInfo user
				, uint   attributeId
				, string clientId = null);

		Task<SignInterpretationListDTO> CreateSignInterpretationAsync(
				UserInfo                      user
				, uint?                       signInterpretationId
				, SignInterpretationCreateDTO signInterpretation
				, string                      clientId = null);

		Task<SignInterpretationDeleteDTO> DeleteSignInterpretationAsync(
				UserInfo   user
				, uint     signInterpretationId
				, string[] optional
				, string   clientId = null);

		Task<SignInterpretationDTO> LinkSignInterpretationsAsync(
				UserInfo user
				, uint   firstSignInterpretationId
				, uint   secondSignInterpretationId
				, string clientId = null);

		Task<SignInterpretationDTO> UnlinkSignInterpretationsAsync(
				UserInfo user
				, uint   firstSignInterpretationId
				, uint   secondSignInterpretationId
				, string clientId = null);

		Task<SignInterpretationDTO> GetEditionSignInterpretationAsync(
				UserInfo user
				, uint   signInterpretationId);

		Task<SignInterpretationDTO> CreateOrUpdateSignInterpretationCommentaryAsync(
				UserInfo              user
				, uint                signInterpretationId
				, CommentaryCreateDTO commentary
				, string              clientId = null);

		Task<SignInterpretationDTO> CreateSignInterpretationAttributeAsync(
				UserInfo                           user
				, uint                             signInterpretationId
				, InterpretationAttributeCreateDTO attribute
				, string                           clientId = null);

		Task<SignInterpretationDTO> UpdateSignInterpretationAttributeAsync(
				UserInfo                           user
				, uint                             signInterpretationId
				, uint                             attributeValueId
				, InterpretationAttributeCreateDTO attribute
				, string                           clientId = null);

		Task<NoContentResult> DeleteSignInterpretationAttributeAsync(
				UserInfo user
				, uint   signInterpretationAttributeId
				, uint   attributeValueId
				, string clientId = null);

		Task<NoContentResult> MaterializeSignStreams(UserInfo user, uint[] editionIds);
	}

	public class SignInterpretationService : ISignInterpretationService
	{
		private readonly IAttributeRepository _attributeRepository;

		private readonly ISignInterpretationCommentaryRepository _commentaryRepository;

		private readonly IHubContext<MainHub, ISQEClient>     _hubContext;
		private readonly ISignStreamMaterializationRepository _materializationRepository;

		private readonly ISignInterpretationRepository _signInterpretationRepository;

		private readonly ITextRepository _textRepository;

		public SignInterpretationService(
				IHubContext<MainHub, ISQEClient>          hubContext
				, IAttributeRepository                    attributeRepository
				, ISignInterpretationRepository           signInterpretationRepository
				, ISignInterpretationCommentaryRepository commentaryRepository
				, ITextRepository                         textRepository
				, ISignStreamMaterializationRepository    materializationRepository)
		{
			_hubContext = hubContext;
			_attributeRepository = attributeRepository;
			_signInterpretationRepository = signInterpretationRepository;
			_commentaryRepository = commentaryRepository;
			_textRepository = textRepository;
			_materializationRepository = materializationRepository;
		}

		public async Task<AttributeListDTO>
				GetEditionSignInterpretationAttributesAsync(UserInfo user)
			=> (await _attributeRepository.GetAllEditionAttributesAsync(user)).ToDTO();

		public async Task<AttributeDTO> CreateEditionAttributeAsync(
				UserInfo             user
				, CreateAttributeDTO newAttribute
				, string             clientId = null)
		{
			var newAttributeId = await _attributeRepository.CreateEditionAttribute(
					user
					, newAttribute.attributeName
					, newAttribute.description
					, newAttribute.editable
					, newAttribute.removable
					, newAttribute.repeatable
					, newAttribute.batchEditable
					, newAttribute.values.Select(
							x => new SignInterpretationAttributeValueInput
							{
									AttributeStringValue = x.value
									, AttributeStringValueDescription = x.description
									, Css = x.cssDirectives
									,
							}));

			var createdAttribute =
					(await _attributeRepository.GetEditionAttributeAsync(user, newAttributeId))
					.ToDTO()
					.attributes.FirstOrDefault();

			// Broadcast the changes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .CreatedAttribute(createdAttribute);

			return createdAttribute;
		}

		public async Task<AttributeDTO> UpdateEditionAttributeAsync(
				UserInfo             user
				, uint               attributeId
				, UpdateAttributeDTO updatedAttribute
				, string             clientId = null)
		{
			var updatedAttributeId = await _attributeRepository.UpdateEditionAttribute(
					user
					, attributeId
					, null
					, null
					, updatedAttribute.editable
					, updatedAttribute.removable
					, updatedAttribute.repeatable
					, updatedAttribute.batchEditable
					, updatedAttribute.createValues.Select(x => x.FromDTO())
					, updatedAttribute.updateValues.Select(x => x.FromDTO())
					, updatedAttribute.deleteValues);

			var updatedAttributeDetails =
					(await _attributeRepository.GetEditionAttributeAsync(user, updatedAttributeId))
					.ToDTO()
					.attributes.FirstOrDefault();

			if (updatedAttributeId != attributeId) // Broadcast the changes as a create and a delete
			{
				await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
								 .CreatedAttribute(updatedAttributeDetails);

				await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
								 .DeletedAttribute(
										 new DeleteDTO(EditionEntities.attribute, attributeId));
			}
			else // Broadcast the changes as an update
			{
				await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
								 .UpdatedAttribute(updatedAttributeDetails);
			}

			return updatedAttributeDetails;
		}

		public async Task<NoContentResult> DeleteEditionAttributeAsync(
				UserInfo user
				, uint   attributeId
				, string clientId = null)
		{
			await _attributeRepository.DeleteEditionAttributeAsync(user, attributeId);

			// Broadcast the changes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .DeletedAttribute(
									 new DeleteDTO(EditionEntities.attribute, attributeId));

			return new NoContentResult();
		}

		/// <summary>
		///  This method creates a new sign interpretation.
		///  If a sign interpretation is provided then this creates an alternate
		///  reading of that sign interpretation, otherwise it creates a completely
		///  new sign for the sign interpretation to define.
		/// </summary>
		/// <param name="user">The requesting user's object</param>
		/// <param name="signInterpretationId">
		///  Optional sign interpretation, if null a new sign is
		///  created for the sign interpretation here, otherwise the sign interpretation is created
		///  as a variant of the signInterpretationId.
		/// </param>
		/// <param name="signInterpretation">Information about the new sign interpretation to be created</param>
		/// <param name="clientId"></param>
		/// <returns></returns>
		public async Task<SignInterpretationListDTO> CreateSignInterpretationAsync(
				UserInfo                      user
				, uint?                       signInterpretationId
				, SignInterpretationCreateDTO signInterpretation
				, string                      clientId = null)
		{
			var createdSignInterpretation = await _textRepository.CreateSignsAsync(
					user
					, signInterpretation.lineId
					, signInterpretation.ToSignData()
					, signInterpretation.previousSignInterpretationIds.ToList()
					, signInterpretation.nextSignInterpretationIds.ToList()
					, signInterpretationId
					, signInterpretation.breakPreviousAndNextSignInterpretations);

			// Prepare the response by gathering created sign interpretation(s) and previous sign interpretations
			var alteredSignInterpretations = await Task.WhenAll( // Await all async operations
					signInterpretation.previousSignInterpretationIds
									  .ToList() // Take list of previous sign interpretation ids
									  .Concat(
											  createdSignInterpretation.SelectMany(
													  x => // Concat all sign interpretation ids from createdSignInterpretation
															  x.SignInterpretations
															   .Where(
																	   y
																			   => y
																				  .SignInterpretationId
																				  .HasValue)
															   .Select(
																	   y => y.SignInterpretationId
																			 .Value)))
									  .Select(
											  async x => await GetEditionSignInterpretationAsync(
													  user
													  , x))); // Get the SignInterpretationDTO fpr each sign interpretation

			var response = new SignInterpretationListDTO
			{
					signInterpretations = alteredSignInterpretations.ToArray(),
			};

			// Broadcast the changes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .CreatedSignInterpretation(response);

			return response;
		}

		/// <summary>
		///  Deletes a sign interpretation from the edition.  If the list of strings in optional
		///  contains "delete-all-variants", then the system will search for all variant readings
		///  to the sign interpretation id and delete those as well.
		/// </summary>
		/// <param name="user">The requesting user's object</param>
		/// <param name="signInterpretationId">The id of the sign interpretation to be deleted</param>
		/// <param name="optional">
		///  A list of optional commands, "delete-all-variants" will
		///  delete all variant interpretations of the signInterpretationId as well
		/// </param>
		/// <param name="clientId"></param>
		/// <returns>
		///  A list of all sign interpretations that have changed as a result of the
		///  operation
		/// </returns>
		public async Task<SignInterpretationDeleteDTO> DeleteSignInterpretationAsync(
				UserInfo   user
				, uint     signInterpretationId
				, string[] optional
				, string   clientId = null)
		{
			var (deleted, updated) = await _textRepository.RemoveSignInterpretationAsync(
					user
					, signInterpretationId
					, optional.Contains("delete-all-variants"));

			var deletedList = deleted.ToArray();

			// Prepare the response by gathering created sign interpretation(s) and previous sign interpretations
			var formattedUpdates = await Task.WhenAll( // Await all async operations
					updated.Select(
							async x => await GetEditionSignInterpretationAsync(
									user
									, x))); // Get the SignInterpretationDTO fpr each sign interpretation

			var changes = new SignInterpretationListDTO
			{
					signInterpretations = formattedUpdates.ToArray(),
			};

			// Broadcast the changes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .UpdatedSignInterpretations(changes);

			// Broadcast the deletes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .DeletedSignInterpretation(
									 new DeleteDTO(
											 EditionEntities.signInterpretation
											 , deletedList.ToList()));

			return new SignInterpretationDeleteDTO
			{
					updates = changes
					, deletes = deletedList
					,
			};
		}

		public async Task<SignInterpretationDTO> LinkSignInterpretationsAsync(
				UserInfo user
				, uint   firstSignInterpretationId
				, uint   secondSignInterpretationId
				, string clientId = null)
		{
			await _textRepository.LinkSignInterpretationsAsync(
					user
					, firstSignInterpretationId
					, secondSignInterpretationId);

			var changedSignInterpretation =
					await GetEditionSignInterpretationAsync(user, firstSignInterpretationId);

			// Broadcast the changes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .UpdatedSignInterpretation(changedSignInterpretation);

			return changedSignInterpretation;
		}

		public async Task<SignInterpretationDTO> UnlinkSignInterpretationsAsync(
				UserInfo user
				, uint   firstSignInterpretationId
				, uint   secondSignInterpretationId
				, string clientId = null)
		{
			await _textRepository.UnlinkSignInterpretationsAsync(
					user
					, firstSignInterpretationId
					, secondSignInterpretationId);

			var changedSignInterpretation =
					await GetEditionSignInterpretationAsync(user, firstSignInterpretationId);

			// Broadcast the changes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .UpdatedSignInterpretation(changedSignInterpretation);

			return changedSignInterpretation;
		}

		public async Task<SignInterpretationDTO> GetEditionSignInterpretationAsync(
				UserInfo user
				, uint   signInterpretationId)
		{
			var signInterpretation =
					await _signInterpretationRepository.GetSignInterpretationById(
							user
							, signInterpretationId);

			return signInterpretation.ToDTO();
		}

		public async Task<SignInterpretationDTO> CreateOrUpdateSignInterpretationCommentaryAsync(
				UserInfo              user
				, uint                signInterpretationId
				, CommentaryCreateDTO commentary
				, string              clientId = null)
		{
			await _commentaryRepository.CreateOrUpdateCommentaryAsync(
					user
					, signInterpretationId
					, null
					, commentary.commentary);

			var updatedSignInterpretation =
					(await _signInterpretationRepository.GetSignInterpretationById(
							user
							, signInterpretationId)).ToDTO();

			// Broadcast the changes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .UpdatedSignInterpretation(updatedSignInterpretation);

			return updatedSignInterpretation;
		}

		public async Task<SignInterpretationDTO> CreateSignInterpretationAttributeAsync(
				UserInfo                           user
				, uint                             signInterpretationId
				, InterpretationAttributeCreateDTO attribute
				, string                           clientId = null)
		{
			var createAttribute = new SignInterpretationAttributeData
			{
					AttributeValueId = attribute.attributeValueId
					, Sequence = attribute.sequence
					,
			};

			await _attributeRepository.CreateSignInterpretationAttributesAsync(
					user
					, signInterpretationId
					, createAttribute);

			if (!string.IsNullOrEmpty(attribute.commentary))
			{
				var commentary = new SignInterpretationCommentaryData
				{
						AttributeId = attribute.attributeId
						, Commentary = attribute.commentary
						,
				};

				await _commentaryRepository.CreateCommentaryAsync(
						user
						, signInterpretationId
						, commentary);
			}

			var updatedSignInterpretation =
					(await _signInterpretationRepository.GetSignInterpretationById(
							user
							, signInterpretationId)).ToDTO();

			// Broadcast the changes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .UpdatedSignInterpretation(updatedSignInterpretation);

			return updatedSignInterpretation;
		}

		public async Task<SignInterpretationDTO> UpdateSignInterpretationAttributeAsync(
				UserInfo                           user
				, uint                             signInterpretationId
				, uint                             attributeValueId
				, InterpretationAttributeCreateDTO attribute
				, string                           clientId = null)
		{
			if (attribute.sequence.HasValue)
			{
				await _attributeRepository.UpdateAttributeForSignInterpretationAsync(
						user
						, signInterpretationId
						, attributeValueId
						, attribute.sequence);
			}

			if (!string.IsNullOrEmpty(attribute.commentary))
			{
				await _commentaryRepository.CreateOrUpdateCommentaryAsync(
						user
						, signInterpretationId
						, attributeValueId
						, attribute.commentary);
			}

			var updatedSignInterpretation =
					(await _signInterpretationRepository.GetSignInterpretationById(
							user
							, signInterpretationId)).ToDTO();

			// Broadcast the changes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .UpdatedSignInterpretation(updatedSignInterpretation);

			return updatedSignInterpretation;
		}

		public async Task<NoContentResult> DeleteSignInterpretationAttributeAsync(
				UserInfo user
				, uint   signInterpretationId
				, uint   attributeValueId
				, string clientId = null)
		{
			await _attributeRepository.DeleteAttributeFromSignInterpretationAsync(
					user
					, signInterpretationId
					, attributeValueId);

			var updatedSignInterpretation =
					(await _signInterpretationRepository.GetSignInterpretationById(
							user
							, signInterpretationId)).ToDTO();

			// Broadcast the changes
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .UpdatedSignInterpretation(updatedSignInterpretation);

			return new NoContentResult();
		}

		public async Task<NoContentResult> MaterializeSignStreams(UserInfo user, uint[] editionIds)
		{
			if (!user.SystemRoles.Contains(UserSystemRoles.USER_ADMIN))
				throw new StandardExceptions.NoSystemPermissionsException(user);

			if (editionIds.Length == 0)
				await _materializationRepository.MaterializeAllSignStreamsAsync();

			foreach (var editionId in editionIds)
				await _materializationRepository.RequestMaterializationAsync(editionId);

			return new NoContentResult();
		}
	}
}
