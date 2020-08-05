/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-api-server` project. Changes made
 * there will automatically be incorporated here the next time the 
 * `sqe-realtime-hub-builder` is run.
 */

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SQE.API.DTO;
using SQE.API.Server.Services;
using Microsoft.AspNetCore.SignalR;

using SQE.DatabaseAccess.Helpers;

using System.Text.Json;

using SQE.API.Server.Helpers;

namespace SQE.API.Server.RealtimeHubs
{
    public partial class MainHub
    {
        /// <summary>
        /// Replaces the sign interpretation in the route with the submitted sign interpretation DTO.
        /// This is the only way to change a sign interpretation's character. The endpoint will create
        /// a new sign interpretation id with the submitted information, remove the old sign interpretation
        /// id from the edition sign streams, and insert the new sign interpretation into its place in the stream. 
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being replaced</param>
        /// <param name="newSignInterpretation">New sign interpretation data to be added</param>
        /// <returns>The new sign interpretation</returns>
        [Authorize]
        public async Task<SignInterpretationDTO> PostV1EditionsEditionIdSignInterpretationsSignInterpretationId(uint editionId, uint signInterpretationId, SignInterpretationCreateDTO newSignInterpretation)

        {
            try
            {
                return null; //Not Implemented              
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        /// Deletes the sign interpretation in the route. The endpoint automatically manages the sign stream
        /// by connecting all the deleted sign's next and previous nodes.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being deleted</param>
        /// <returns>Ok or Error</returns>
        [Authorize]
        public async Task DeleteV1EditionsEditionIdSignInterpretationsSignInterpretationId(uint editionId, uint signInterpretationId)

        {
            try
            {
                null; //Not Implemented              
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        /// Updates the commentary of a sign interpretation
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation whose commentary is being changed</param>
        /// <param name="string">The new commentary for the sign interpretation</param>
        /// <returns>Ok or Error</returns>
        [Authorize]
        public async Task PutV1EditionsEditionIdSignInterpretationsSignInterpretationIdCommentary(uint editionId, uint signInterpretationId, string commentary)

        {
            try
            {
                null;  //Not Implemented              
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        /// This adds a new attribute to the specified sign interpretation.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation for adding a new attribute</param>
        /// <param name="newSignInterpretationAttributes">Details of the attribute to be added</param>
        /// <returns>The updated sign interpretation</returns>
        [Authorize]
        public async Task<SignInterpretationDTO> PostV1EditionsEditionIdSignInterpretationsSignInterpretationIdAttributes(uint editionId, uint signInterpretationId, InterpretationAttributeCreateListDTO newSignInterpretationAttributes)

        {
            try
            {
                return null;  //Not Implemented              
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        /// This changes the values of the specified sign interpretation attribute,
        /// mainly used to change commentary.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
        /// <param name="attributeId">Id of the attribute to be altered</param>
        /// <param name="alteredSignInterpretationAttribute">New details of the attribute</param>
        /// <returns>The updated sign interpretation</returns>
        [Authorize]
        public async Task<SignInterpretationDTO> PutV1EditionsEditionIdSignInterpretationsSignInterpretationIdAttributesAttributeId(uint editionId, uint signInterpretationId, uint attributeId, InterpretationAttributeCreateDTO alteredSignInterpretationAttribute)

        {
            try
            {
                return null;  //Not Implemented              
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        /// This deletes the specified attribute from the specified sign interpretation.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being alteres</param>
        /// <param name="attributeId">Id of the attribute being removed</param>
        /// <returns>Ok or Error</returns>
        [Authorize]
        public async Task DeleteV1EditionsEditionIdSignInterpretationsSignInterpretationIdAttributesAttributeId(uint editionId, uint signInterpretationId, uint attributeId)

        {
            try
            {
                null;  //Not Implemented              
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


    }
}
