/*
 * This file is autogenerated from (solution root)/sqe-realtime-hub-builder/SubscriptionHub.cs.txt
 * Please edit that file if any changes need to be made.
 */

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.API.Server.Helpers;
using SQE.DatabaseAccess.Helpers;

namespace SQE.API.Server.RealtimeHubs
{
	// NOTE: Every connected client has a `Context` that (like an HTTP context) lives for the duration
	// of the connection.  The `Items` attribute is a Dict<object, object> container intended for storing
	// any data that should persist for the life of that connection. `Items` is used here to maintain a record
	// of which groups the client is currently subscribed to.  It is not possible to poll the `Groups` directly
	// to see which, if any, the client is registered to (via its client ID), thus `Context.Items` provides
	// this information. Such data could also be stored in any other type of datastore, but subscriptions for
	// for updates to an edition seemed so ephemeral that using some other store appeared unnecessary or
	// even undesirable—we want these connections to be as disposable as possible and we do not want subscriptions
	// associated with a user_id, only the client.

	public partial class MainHub
	{
		/// <summary>
		///  Override the default OnConnectedAsync to add the connection to the user's user_id
		///  group if the user is authenticated. The user_id group is used for messages that
		///  are above the level of a single edition.
		/// </summary>
		/// <returns></returns>
		public override async Task OnConnectedAsync()
		{
			try
			{
				var user = _userService.GetCurrentUserId(); // Get the user_id if possible

				// If the user is authenticated, add this connection to the user's user_id group.
				// ReSharper disable once EnforceIfStatementBraces
				if (user.HasValue)
					await Groups.AddToGroupAsync(
							Context.ConnectionId
							, $"user-{user.Value.ToString()}");

				await base.OnConnectedAsync();
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  The client subscribes to all changes for the specified editionId.
		/// </summary>
		/// <param name="editionId">The ID of the edition to receive updates</param>
		/// <returns></returns>
		public async Task SubscribeToEdition(uint editionId)
		{
			try
			{
				var user = await _userService.GetCurrentUserObjectAsync(editionId);

				if (!user.MayRead)
					throw new StandardExceptions.NoReadPermissionsException(user);

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
					Context.Items["editionId"] = new List<uint> { editionId };

					// Add it to the editionIdId of this request
					await Groups.AddToGroupAsync(Context.ConnectionId, editionId.ToString());
				}
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  The client unsubscribes to all changes for the specified editionId.
		/// </summary>
		/// <param name="editionId">The ID of the edition to stop receiving updates</param>
		/// <returns></returns>
		public async Task UnsubscribeToEdition(uint editionId)
		{
			try
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
						await Groups.RemoveFromGroupAsync(
								Context.ConnectionId
								, editionId.ToString());

						clientSubscriptions.RemoveAll(x => x == editionId);
					}
				}
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Get a list of all editions the client is currently subscribed to.
		/// </summary>
		/// <returns>A list of every editionId for which the client receives update</returns>
		public List<uint> ListEditionSubscriptions()
		{
			try
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
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}
	}
}
