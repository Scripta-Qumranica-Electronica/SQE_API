
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


    public static partial class Delete
    {


        public class V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId
        : RequestObject<EmptyInput, EmptyOutput, DeleteDTO>
        {
            public readonly uint EditionId;
            public readonly uint AttributeId;

            /// <summary>
            /// Delete an attribute from an edition
            /// </summary>
            /// <param name="editionId">The ID of the edition being edited</param>
            /// <param name="attributeId">The ID of the attribute to delete</param>
            /// <returns></returns>
            /// <exception cref="NotImplementedException"></exception>
            public V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(uint editionId, uint attributeId)
                : base(null)
            {
                this.EditionId = editionId;
                this.AttributeId = attributeId;
                this.listenerMethod = "DeletedAttribute";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/attribute-id", $"/{AttributeId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, AttributeId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId
        : RequestObject<EmptyInput, EmptyOutput, SignInterpretationDTO>
        {
            public readonly uint EditionId;
            public readonly uint SignInterpretationId;
            public readonly uint AttributeValueId;

            /// <summary>
            /// This deletes the specified attribute value from the specified sign interpretation.
            /// </summary>
            /// <param name="editionId">ID of the edition being changed</param>
            /// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
            /// <param name="attributeValueId">Id of the attribute being removed</param>
            /// <returns>Ok or Error</returns>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(uint editionId, uint signInterpretationId, uint attributeValueId)
                : base(null)
            {
                this.EditionId = editionId;
                this.SignInterpretationId = signInterpretationId;
                this.AttributeValueId = attributeValueId;
                this.listenerMethod = "UpdatedSignInterpretation";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/sign-interpretation-id", $"/{SignInterpretationId.ToString()}").Replace("/attribute-value-id", $"/{AttributeValueId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, SignInterpretationId, AttributeValueId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }
    }

    public static partial class Get
    {


        public class V1_Editions_EditionId_SignInterpretationsAttributes
        : RequestObject<EmptyInput, AttributeListDTO, EmptyOutput>
        {
            public readonly uint EditionId;

            /// <summary>
            /// Retrieve a list of all possible attributes for an edition
            /// </summary>
            /// <param name="editionId">The ID of the edition being searched</param>
            /// <returns>A list of and edition's attributes and their details</returns>
            public V1_Editions_EditionId_SignInterpretationsAttributes(uint editionId)
                : base(null)
            {
                this.EditionId = editionId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId
        : RequestObject<EmptyInput, SignInterpretationDTO, EmptyOutput>
        {
            public readonly uint EditionId;

            /// <summary>
            /// Retrieve the details of a sign interpretation in an edition
            /// </summary>
            /// <param name="editionId">The ID of the edition being searched</param>
            /// <param name="signInterpretationId">The desired sign interpretation id</param>
            /// <returns>The details of the desired sign interpretation</returns>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId(uint editionId)
                : base(null)
            {
                this.EditionId = editionId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }
    }

    public static partial class Post
    {


        public class V1_Editions_EditionId_SignInterpretationsAttributes
        : RequestObject<CreateAttributeDTO, AttributeDTO, AttributeDTO>
        {
            public readonly uint EditionId;
            public readonly CreateAttributeDTO Payload;

            /// <summary>
            /// Create a new attribute for an edition
            /// </summary>
            /// <param name="editionId">The ID of the edition being edited</param>
            /// <param name="newAttribute">The details of the new attribute</param>
            /// <returns>The details of the newly created attribute</returns>
            public V1_Editions_EditionId_SignInterpretationsAttributes(uint editionId, CreateAttributeDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.Payload = payload;
                this.listenerMethod = "CreatedAttribute";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes
        : RequestObject<InterpretationAttributeCreateDTO, SignInterpretationDTO, SignInterpretationDTO>
        {
            public readonly uint EditionId;
            public readonly uint SignInterpretationId;
            public readonly InterpretationAttributeCreateDTO Payload;

            /// <summary>
            /// This adds a new attribute to the specified sign interpretation.
            /// </summary>
            /// <param name="editionId">ID of the edition being changed</param>
            /// <param name="signInterpretationId">ID of the sign interpretation for adding a new attribute</param>
            /// <param name="newSignInterpretationAttributes">Details of the attribute to be added</param>
            /// <returns>The updated sign interpretation</returns>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(uint editionId, uint signInterpretationId, InterpretationAttributeCreateDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.SignInterpretationId = signInterpretationId;
                this.Payload = payload;
                this.listenerMethod = "UpdatedSignInterpretation";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/sign-interpretation-id", $"/{SignInterpretationId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, SignInterpretationId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }
    }

    public static partial class Put
    {


        public class V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId
        : RequestObject<UpdateAttributeDTO, AttributeDTO, AttributeDTO>
        {
            public readonly uint EditionId;
            public readonly uint AttributeId;
            public readonly UpdateAttributeDTO Payload;

            /// <summary>
            /// Change the details of an attribute in an edition
            /// </summary>
            /// <param name="editionId">The ID of the edition being edited</param>
            /// <param name="attributeId">The ID of the attribute to update</param>
            /// <param name="updatedAttribute">The details of the updated attribute</param>
            /// <returns></returns>
            /// <exception cref="NotImplementedException"></exception>
            public V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(uint editionId, uint attributeId, UpdateAttributeDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.AttributeId = attributeId;
                this.Payload = payload;
                this.listenerMethod = "UpdatedAttribute";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/attribute-id", $"/{AttributeId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, AttributeId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary
        : RequestObject<CommentaryCreateDTO, SignInterpretationDTO, SignInterpretationDTO>
        {
            public readonly uint EditionId;
            public readonly uint SignInterpretationId;
            public readonly CommentaryCreateDTO Payload;

            // /// <summary>
            // /// Creates a new sign interpretation 
            // /// </summary>
            // /// <param name="editionId">ID of the edition being changed</param>
            // /// <param name="newSignInterpretation">New sign interpretation data to be added</param>
            // /// <returns>The new sign interpretation</returns>
            // [HttpPost("v1/editions/{editionId}/sign-interpretations")]
            // public async Task<ActionResult<SignInterpretationDTO>> PostNewSignInterpretation([FromRoute] uint editionId,
            //     [FromBody] SignInterpretationCreateDTO newSignInterpretation)
            // {
            //     throw new NotImplementedException(); //Not Implemented
            // }
            //
            // /// <summary>
            // /// Deletes the sign interpretation in the route. The endpoint automatically manages the sign stream
            // /// by connecting all the deleted sign's next and previous nodes.
            // /// </summary>
            // /// <param name="editionId">ID of the edition being changed</param>
            // /// <param name="signInterpretationId">ID of the sign interpretation being deleted</param>
            // /// <returns>Ok or Error</returns>
            // [HttpDelete("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}")]
            // public async Task<ActionResult> DeleteSignInterpretation([FromRoute] uint editionId,
            //     [FromRoute] uint signInterpretationId)
            // {
            //     throw new NotImplementedException(); //Not Implemented
            // }

            /// <summary>
            /// Updates the commentary of a sign interpretation
            /// </summary>
            /// <param name="editionId">ID of the edition being changed</param>
            /// <param name="signInterpretationId">ID of the sign interpretation whose commentary is being changed</param>
            /// <param name="commentary">The new commentary for the sign interpretation</param>
            /// <returns>Ok or Error</returns>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary(uint editionId, uint signInterpretationId, CommentaryCreateDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.SignInterpretationId = signInterpretationId;
                this.Payload = payload;
                this.listenerMethod = "UpdatedSignInterpretation";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/sign-interpretation-id", $"/{SignInterpretationId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, SignInterpretationId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId
        : RequestObject<InterpretationAttributeCreateDTO, SignInterpretationDTO, SignInterpretationDTO>
        {
            public readonly uint EditionId;
            public readonly uint SignInterpretationId;
            public readonly uint AttributeValueId;
            public readonly InterpretationAttributeCreateDTO Payload;

            /// <summary>
            /// This changes the values of the specified sign interpretation attribute,
            /// mainly used to change commentary.
            /// </summary>
            /// <param name="editionId">ID of the edition being changed</param>
            /// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
            /// <param name="attributeValueId">Id of the attribute value to be altered</param>
            /// <param name="alteredSignInterpretationAttribute">New details of the attribute</param>
            /// <returns>The updated sign interpretation</returns>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(uint editionId, uint signInterpretationId, uint attributeValueId, InterpretationAttributeCreateDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.SignInterpretationId = signInterpretationId;
                this.AttributeValueId = attributeValueId;
                this.Payload = payload;
                this.listenerMethod = "UpdatedSignInterpretation";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/sign-interpretation-id", $"/{SignInterpretationId.ToString()}").Replace("/attribute-value-id", $"/{AttributeValueId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, SignInterpretationId, AttributeValueId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }
    }

}
