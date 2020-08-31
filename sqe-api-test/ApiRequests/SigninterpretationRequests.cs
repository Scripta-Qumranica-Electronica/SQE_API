/*
 * This file is automatically generated by the GenerateTestRequestObjects
 * project in the Utilities folder. Do not edit this file directly as
 * its contents may be overwritten at any point.
 *
 * Should a class here need to be altered for any reason, you should look
 * first to the auto generation program for possible updating to include
 * the needed special case. Otherwise, it is possible to create your own
 * manually written ApiRequest object, though this is generally discouraged.
 */


using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Delete
    {
        public class V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId
            : RequestObject<EmptyInput, EmptyOutput>
        {
            private readonly uint _attributeId;
            private readonly uint _editionId;

            /// <summary>
            ///     Delete an attribute from an edition
            /// </summary>
            /// <param name="editionId">The ID of the edition being edited</param>
            /// <param name="attributeId">The ID of the attribute to delete</param>
            /// <returns></returns>
            /// <exception cref="NotImplementedException"></exception>
            public V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(uint editionId, uint attributeId)

            {
                _editionId = editionId;
                _attributeId = attributeId;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.DeletedAttribute, (DeletedAttributeIsNull, DeletedAttributeListener));
            }

            public Listeners AvailableListeners { get; }

            public DeleteDTO DeletedAttribute { get; private set; }

            private void DeletedAttributeListener(HubConnection signalrListener)
            {
                signalrListener.On<DeleteDTO>("DeletedAttribute",
                    receivedData => DeletedAttribute = receivedData);
            }

            private bool DeletedAttributeIsNull()
            {
                return DeletedAttribute == null;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/attribute-id", $"/{_attributeId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _attributeId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }

            public class Listeners
            {
                public ListenerMethods DeletedAttribute = ListenerMethods.DeletedAttribute;
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId
            : RequestObject<EmptyInput, EmptyOutput>
        {
            private readonly uint _attributeValueId;
            private readonly uint _editionId;
            private readonly uint _signInterpretationId;

            /// <summary>
            ///     This deletes the specified attribute value from the specified sign interpretation.
            /// </summary>
            /// <param name="editionId">ID of the edition being changed</param>
            /// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
            /// <param name="attributeValueId">Id of the attribute being removed</param>
            /// <returns>Ok or Error</returns>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(
                uint editionId, uint signInterpretationId, uint attributeValueId)

            {
                _editionId = editionId;
                _signInterpretationId = signInterpretationId;
                _attributeValueId = attributeValueId;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.UpdatedSignInterpretation,
                    (UpdatedSignInterpretationIsNull, UpdatedSignInterpretationListener));
            }

            public Listeners AvailableListeners { get; }

            public SignInterpretationDTO UpdatedSignInterpretation { get; private set; }

            private void UpdatedSignInterpretationListener(HubConnection signalrListener)
            {
                signalrListener.On<SignInterpretationDTO>("UpdatedSignInterpretation",
                    receivedData => UpdatedSignInterpretation = receivedData);
            }

            private bool UpdatedSignInterpretationIsNull()
            {
                return UpdatedSignInterpretation == null;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/sign-interpretation-id", $"/{_signInterpretationId.ToString()}")
                    .Replace("/attribute-value-id", $"/{_attributeValueId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _signInterpretationId,
                    _attributeValueId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }

            public class Listeners
            {
                public ListenerMethods UpdatedSignInterpretation = ListenerMethods.UpdatedSignInterpretation;
            }
        }
    }

    public static partial class Get
    {
        public class V1_Editions_EditionId_SignInterpretationsAttributes
            : RequestObject<EmptyInput, AttributeListDTO>
        {
            private readonly uint _editionId;


            /// <summary>
            ///     Retrieve a list of all possible attributes for an edition
            /// </summary>
            /// <param name="editionId">The ID of the edition being searched</param>
            /// <returns>A list of and edition's attributes and their details</returns>
            public V1_Editions_EditionId_SignInterpretationsAttributes(uint editionId)

            {
                _editionId = editionId;
            }


            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId
            : RequestObject<EmptyInput, SignInterpretationDTO>
        {
            private readonly uint _editionId;
            private readonly uint _signInterpretationId;


            /// <summary>
            ///     Retrieve the details of a sign interpretation in an edition
            /// </summary>
            /// <param name="editionId">The ID of the edition being searched</param>
            /// <param name="signInterpretationId">The desired sign interpretation id</param>
            /// <returns>The details of the desired sign interpretation</returns>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId(uint editionId,
                uint signInterpretationId)

            {
                _editionId = editionId;
                _signInterpretationId = signInterpretationId;
            }


            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/sign-interpretation-id", $"/{_signInterpretationId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _signInterpretationId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }
    }

    public static partial class Post
    {
        public class V1_Editions_EditionId_SignInterpretationsAttributes
            : RequestObject<CreateAttributeDTO, AttributeDTO>
        {
            private readonly uint _editionId;
            private readonly CreateAttributeDTO _payload;

            /// <summary>
            ///     Create a new attribute for an edition
            /// </summary>
            /// <param name="editionId">The ID of the edition being edited</param>
            /// <param name="newAttribute">The details of the new attribute</param>
            /// <returns>The details of the newly created attribute</returns>
            public V1_Editions_EditionId_SignInterpretationsAttributes(uint editionId, CreateAttributeDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.CreatedAttribute, (CreatedAttributeIsNull, CreatedAttributeListener));
            }

            public Listeners AvailableListeners { get; }

            public AttributeDTO CreatedAttribute { get; private set; }

            private void CreatedAttributeListener(HubConnection signalrListener)
            {
                signalrListener.On<AttributeDTO>("CreatedAttribute",
                    receivedData => CreatedAttribute = receivedData);
            }

            private bool CreatedAttributeIsNull()
            {
                return CreatedAttribute == null;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }

            public class Listeners
            {
                public ListenerMethods CreatedAttribute = ListenerMethods.CreatedAttribute;
            }
        }

        public class V1_Editions_EditionId_SignInterpretations
            : RequestObject<SignInterpretationCreateDTO, SignInterpretationListDTO>
        {
            private readonly uint _editionId;
            private readonly SignInterpretationCreateDTO _payload;

            /// <summary>
            ///     Creates a new sign interpretation
            /// </summary>
            /// <param name="editionId">ID of the edition being changed</param>
            /// <param name="newSignInterpretation">New sign interpretation data to be added</param>
            /// <returns>The new sign interpretation</returns>
            public V1_Editions_EditionId_SignInterpretations(uint editionId, SignInterpretationCreateDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.CreatedSignInterpretation,
                    (CreatedSignInterpretationIsNull, CreatedSignInterpretationListener));
            }

            public Listeners AvailableListeners { get; }

            public SignInterpretationListDTO CreatedSignInterpretation { get; private set; }

            private void CreatedSignInterpretationListener(HubConnection signalrListener)
            {
                signalrListener.On<SignInterpretationListDTO>("CreatedSignInterpretation",
                    receivedData => CreatedSignInterpretation = receivedData);
            }

            private bool CreatedSignInterpretationIsNull()
            {
                return CreatedSignInterpretation == null;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }

            public class Listeners
            {
                public ListenerMethods CreatedSignInterpretation = ListenerMethods.CreatedSignInterpretation;
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes
            : RequestObject<InterpretationAttributeCreateDTO, SignInterpretationDTO>
        {
            private readonly uint _editionId;
            private readonly InterpretationAttributeCreateDTO _payload;
            private readonly uint _signInterpretationId;

            /// <summary>
            ///     This adds a new attribute to the specified sign interpretation.
            /// </summary>
            /// <param name="editionId">ID of the edition being changed</param>
            /// <param name="signInterpretationId">ID of the sign interpretation for adding a new attribute</param>
            /// <param name="newSignInterpretationAttributes">Details of the attribute to be added</param>
            /// <returns>The updated sign interpretation</returns>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(uint editionId,
                uint signInterpretationId, InterpretationAttributeCreateDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _signInterpretationId = signInterpretationId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.UpdatedSignInterpretation,
                    (UpdatedSignInterpretationIsNull, UpdatedSignInterpretationListener));
            }

            public Listeners AvailableListeners { get; }

            public SignInterpretationDTO UpdatedSignInterpretation { get; private set; }

            private void UpdatedSignInterpretationListener(HubConnection signalrListener)
            {
                signalrListener.On<SignInterpretationDTO>("UpdatedSignInterpretation",
                    receivedData => UpdatedSignInterpretation = receivedData);
            }

            private bool UpdatedSignInterpretationIsNull()
            {
                return UpdatedSignInterpretation == null;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/sign-interpretation-id", $"/{_signInterpretationId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR =>
                    signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _signInterpretationId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }

            public class Listeners
            {
                public ListenerMethods UpdatedSignInterpretation = ListenerMethods.UpdatedSignInterpretation;
            }
        }
    }

    public static partial class Put
    {
        public class V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId
            : RequestObject<UpdateAttributeDTO, AttributeDTO>
        {
            private readonly uint _attributeId;
            private readonly uint _editionId;
            private readonly UpdateAttributeDTO _payload;

            /// <summary>
            ///     Change the details of an attribute in an edition
            /// </summary>
            /// <param name="editionId">The ID of the edition being edited</param>
            /// <param name="attributeId">The ID of the attribute to update</param>
            /// <param name="updatedAttribute">The details of the updated attribute</param>
            /// <returns></returns>
            /// <exception cref="NotImplementedException"></exception>
            public V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(uint editionId, uint attributeId,
                UpdateAttributeDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _attributeId = attributeId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.UpdatedAttribute, (UpdatedAttributeIsNull, UpdatedAttributeListener));
            }

            public Listeners AvailableListeners { get; }

            public AttributeDTO UpdatedAttribute { get; private set; }

            private void UpdatedAttributeListener(HubConnection signalrListener)
            {
                signalrListener.On<AttributeDTO>("UpdatedAttribute",
                    receivedData => UpdatedAttribute = receivedData);
            }

            private bool UpdatedAttributeIsNull()
            {
                return UpdatedAttribute == null;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/attribute-id", $"/{_attributeId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _attributeId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }

            public class Listeners
            {
                public ListenerMethods UpdatedAttribute = ListenerMethods.UpdatedAttribute;
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary
            : RequestObject<CommentaryCreateDTO, SignInterpretationDTO>
        {
            private readonly uint _editionId;
            private readonly CommentaryCreateDTO _payload;
            private readonly uint _signInterpretationId;

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
            ///     Updates the commentary of a sign interpretation
            /// </summary>
            /// <param name="editionId">ID of the edition being changed</param>
            /// <param name="signInterpretationId">ID of the sign interpretation whose commentary is being changed</param>
            /// <param name="commentary">The new commentary for the sign interpretation</param>
            /// <returns>Ok or Error</returns>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary(uint editionId,
                uint signInterpretationId, CommentaryCreateDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _signInterpretationId = signInterpretationId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.UpdatedSignInterpretation,
                    (UpdatedSignInterpretationIsNull, UpdatedSignInterpretationListener));
            }

            public Listeners AvailableListeners { get; }

            public SignInterpretationDTO UpdatedSignInterpretation { get; private set; }

            private void UpdatedSignInterpretationListener(HubConnection signalrListener)
            {
                signalrListener.On<SignInterpretationDTO>("UpdatedSignInterpretation",
                    receivedData => UpdatedSignInterpretation = receivedData);
            }

            private bool UpdatedSignInterpretationIsNull()
            {
                return UpdatedSignInterpretation == null;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/sign-interpretation-id", $"/{_signInterpretationId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR =>
                    signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _signInterpretationId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }

            public class Listeners
            {
                public ListenerMethods UpdatedSignInterpretation = ListenerMethods.UpdatedSignInterpretation;
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId
            : RequestObject<InterpretationAttributeCreateDTO, SignInterpretationDTO>
        {
            private readonly uint _attributeValueId;
            private readonly uint _editionId;
            private readonly InterpretationAttributeCreateDTO _payload;
            private readonly uint _signInterpretationId;

            /// <summary>
            ///     This changes the values of the specified sign interpretation attribute,
            ///     mainly used to change commentary.
            /// </summary>
            /// <param name="editionId">ID of the edition being changed</param>
            /// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
            /// <param name="attributeValueId">Id of the attribute value to be altered</param>
            /// <param name="alteredSignInterpretationAttribute">New details of the attribute</param>
            /// <returns>The updated sign interpretation</returns>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(
                uint editionId, uint signInterpretationId, uint attributeValueId,
                InterpretationAttributeCreateDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _signInterpretationId = signInterpretationId;
                _attributeValueId = attributeValueId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.UpdatedSignInterpretation,
                    (UpdatedSignInterpretationIsNull, UpdatedSignInterpretationListener));
            }

            public Listeners AvailableListeners { get; }

            public SignInterpretationDTO UpdatedSignInterpretation { get; private set; }

            private void UpdatedSignInterpretationListener(HubConnection signalrListener)
            {
                signalrListener.On<SignInterpretationDTO>("UpdatedSignInterpretation",
                    receivedData => UpdatedSignInterpretation = receivedData);
            }

            private bool UpdatedSignInterpretationIsNull()
            {
                return UpdatedSignInterpretation == null;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/sign-interpretation-id", $"/{_signInterpretationId.ToString()}")
                    .Replace("/attribute-value-id", $"/{_attributeValueId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _signInterpretationId,
                    _attributeValueId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }

            public class Listeners
            {
                public ListenerMethods UpdatedSignInterpretation = ListenerMethods.UpdatedSignInterpretation;
            }
        }
    }
}