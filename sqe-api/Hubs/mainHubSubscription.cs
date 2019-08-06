using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.SqeApi.DataAccess.Helpers;

namespace SQE.SqeApi.Server.Hubs
{
    public partial class MainHub : Hub
    {
        /// <summary>
        /// The client subscribes to all changes for the specified editionId.
        /// </summary>
        /// <param name="editionId">The ID of the edition to receive updates</param>
        /// <returns></returns>
        public async Task SubscribeToEdition(uint editionId)
        {
            var user = _userService.GetCurrentUserObject(editionId);

            if (!(await user.MayRead()))
                throw new StandardErrors.NoReadPermissions(user);

            // If client is already subscribed to at least one editionId
            if (Context.Items.TryGetValue("subscriptions", out var clientSubscriptionsObject))
            {
                // It seems that Context.Items is hardcoded as Dict<object, object>.
                // Too bad I don't know a better way to deal with that.
                var clientSubscriptions = clientSubscriptionsObject as List<uint>;
                // If not already subscribed to this edition, then add it
                if (!clientSubscriptions.Contains(editionId))
                {
                    clientSubscriptions.Add(editionId);
                    await Groups.AddToGroupAsync(Context.ConnectionId, editionId.ToString());
                }
            }
            else // Create the subcription context item and add the editionId
            {
                Context.Items["editionId"] = new List<uint>() {editionId};
                // Add it to the editionIdId of this request
                await Groups.AddToGroupAsync(Context.ConnectionId, editionId.ToString());
            }
        }

        /// <summary>
        /// The client unsubscribes to all changes for the specified editionId.
        /// </summary>
        /// <param name="editionId">The ID of the edition to stop receiving updates</param>
        /// <returns></returns>
        public async Task UnsubscribeToEdition(uint editionId)
        {
            // If client is already subscribed to at least one editionId
            if (Context.Items.TryGetValue("subscriptions", out var clientSubscriptionsObject))
            {
                // It seems that Context.Items is hardcoded as Dict<object, object>.
                // Too bad I don't know a better way to deal with that.
                var clientSubscriptions = clientSubscriptionsObject as List<uint>;
                // If not already subscribed to this edition, then add it
                if (clientSubscriptions.Contains(editionId))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, editionId.ToString());
                    clientSubscriptions.RemoveAll((x) => x == editionId);
                }
            }
        }

        /// <summary>
        /// Get a list of all editions the client is currently subscribed to.
        /// </summary>
        /// <returns>A list of every editionId for which the client receives update</returns>
        public List<uint> ListEditionSubscriptions()
        {
            // If client is already subscribed to at least one editionId
            if (Context.Items.TryGetValue("subscriptions", out var clientSubscriptionsObject))
            {
                // It seems that Context.Items is hardcoded as Dict<object, object>.
                // Too bad I don't know a better way to deal with that.
                return clientSubscriptionsObject as List<uint>;
            }

            return null;
        }
    }
}