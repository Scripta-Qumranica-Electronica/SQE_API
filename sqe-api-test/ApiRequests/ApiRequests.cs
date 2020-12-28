using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CaseExtensions;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.ApiTest.Helpers;
using Xunit;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.ApiTest.ApiRequests
{
	/// <summary>
	///  An object containing the necessary data to make an HTTP request to this API endpoint
	/// </summary>
	public class HttpRequestObject<TInput>
	{
		public HttpMethod RequestVerb   { get; set; }
		public string     RequestString { get; set; }
		public TInput     Payload       { get; set; }
	}

	public interface IRequestObject
	{
		/// <summary>
		///  Issues the request to the API and stores the response.
		///  At least `http` or `realtime` must be provided.  If using a listener, you
		///  will receive a HubConnection as a response from this method. You must
		///  wait some appropriate amount of time for the desired listener message
		///  to be received and close the connection yourself on success/fail.
		/// </summary>
		/// <param name="http">
		///  An HttpClient to run the request on;
		///  may be null if no request should be made to the HTTP server.
		/// </param>
		/// <param name="realtime">
		///  A function to acquire a SignalR hub connection;
		///  may be null if no request should be made to the SignalR server.
		/// </param>
		/// <param name="auth">
		///  Whether to use authentication (default false).
		///  If no user1 is provided the default user "test" will be used.
		/// </param>
		/// <param name="requestUser">
		///  User object for authentication.
		///  If no User is provided, the default "test" user is used.
		/// </param>
		/// <param name="listenerUser">
		///  User object for authentication of the listener.
		///  If no User is provided, listenerUser = user1.
		/// </param>
		/// <param name="shouldSucceed">Whether the request is expected to succeed.</param>
		/// <param name="deterministic">
		///  Whether the request is expected to return the same response from multiple requests.
		///  This method will throw an error if the request is deterministic but the http and realtime responses differ.
		/// </param>
		/// <param name="requestRealtime">Whether a realtime request should be made.</param>
		/// <param name="listenToEdition">Whether a listener should register for messages on the edition id.</param>
		/// <param name="listeningFor">The desired listener methods.</param>
		/// <returns>HubConnection</returns>
		Task SendAsync(
				IEnumerable<ListenerMethods>        listeningFor
				, HttpClient                        http            = null
				, Func<string, Task<HubConnection>> realtime        = null
				, bool                              auth            = false
				, Request.UserAuthDetails           requestUser     = null
				, Request.UserAuthDetails           listenerUser    = null
				, bool                              shouldSucceed   = true
				, bool                              deterministic   = true
				, bool                              requestRealtime = true
				, bool                              listenToEdition = true);

		/// <summary>
		///  Issues the request to the API and stores the response.
		///  At least `http` or `realtime` must be provided.  If using a listener, you
		///  will receive a HubConnection as a response from this method. You must
		///  wait some appropriate amount of time for the desired listener message
		///  to be received and close the connection yourself on success/fail.
		/// </summary>
		/// <param name="http">
		///  An HttpClient to run the request on;
		///  may be null if no request should be made to the HTTP server.
		/// </param>
		/// <param name="realtime">
		///  A function to acquire a SignalR hub connection;
		///  may be null if no request should be made to the SignalR server.
		/// </param>
		/// <param name="auth">
		///  Whether to use authentication (default false).
		///  If no user1 is provided the default user "test" will be used.
		/// </param>
		/// <param name="requestUser">
		///  User object for authentication.
		///  If no User is provided, the default "test" user is used.
		/// </param>
		/// <param name="listenerUser">
		///  User object for authentication of the listener.
		///  If no User is provided, listenerUser = user1.
		/// </param>
		/// <param name="shouldSucceed">Whether the request is expected to succeed.</param>
		/// <param name="deterministic">
		///  Whether the request is expected to return the same response from multiple requests.
		///  This method will throw an error if the request is deterministic but the http and realtime responses differ.
		/// </param>
		/// <param name="requestRealtime">Whether a realtime request should be made.</param>
		/// <param name="listenToEdition">Whether a listener should register for messages on the edition id.</param>
		/// <param name="listeningFor">The desired listener method.</param>
		/// <returns>HubConnection</returns>
		Task SendAsync(
				HttpClient                          http            = null
				, Func<string, Task<HubConnection>> realtime        = null
				, bool                              auth            = false
				, Request.UserAuthDetails           requestUser     = null
				, Request.UserAuthDetails           listenerUser    = null
				, bool                              shouldSucceed   = true
				, bool                              deterministic   = true
				, bool                              requestRealtime = true
				, bool                              listenToEdition = true
				, ListenerMethods?                  listeningFor    = null);
	}

	/// <summary>
	///  An class used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
	/// </summary>
	/// <typeparam name="TInput">The type of the request payload</typeparam>
	/// <typeparam name="TOutput">The API endpoint return type</typeparam>
	/// <typeparam name="TListener">The API endpoint signalr broadcast type</typeparam>
	public abstract class RequestObject<TInput, TOutput> : IRequestObject
	{
		protected readonly
				Dictionary<ListenerMethods, (Func<bool> IsNull, Action<HubConnection> StartListener)
				> _listenerDict;

		private readonly   TInput     _payload;
		private readonly   HttpMethod _requestVerb;
		protected readonly string     RequestPath;
		protected          string     ListenerMethod = null;

		/// <summary>
		///  Provides a RequestObject used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
		/// </summary>
		/// <param name="payload">Payload to be sent to the API endpoint</param>
		protected RequestObject(TInput payload = default)
		{
			_payload = payload;

			var pathElements = GetType().ToString().Split(".").Last().Split('+', '_');

			RequestPath = "/"
						  + string.Join(
								  "/"
								  , pathElements.Skip(1)
												.Select(x => x.ToKebabCase())
												.Where(x => x != "null"));

			var verb = pathElements.First();

			_requestVerb = verb.ToLowerInvariant() switch
						   {
								   "get" => HttpMethod.Get
								   , "post" => HttpMethod.Post
								   , "put" => HttpMethod.Put
								   , "delete" => HttpMethod.Delete
								   , _ => throw new Exception("The HTTP request verb is incorrect")
								   ,
						   };

			_listenerDict = new Dictionary<ListenerMethods, (Func<bool>, Action<HubConnection>)>();
		}

		public HttpResponseMessage HttpResponseMessage   { get; protected set; }
		public TOutput             HttpResponseObject    { get; protected set; }
		public TOutput             SignalrResponseObject { get; protected set; }

		public async Task SendAsync(
				IEnumerable<ListenerMethods>        listeningFor
				, HttpClient                        http            = null
				, Func<string, Task<HubConnection>> realtime        = null
				, bool                              auth            = false
				, Request.UserAuthDetails           requestUser     = null
				, Request.UserAuthDetails           listenerUser    = null
				, bool                              shouldSucceed   = true
				, bool                              deterministic   = true
				, bool                              requestRealtime = true
				, bool                              listenToEdition = true)
		{
			// Throw an error if no transport protocol has been provided
			if ((http == null)
				&& (realtime == null))
			{
				throw new Exception(
						"You must choose at least one transport protocol for the request (http or realtime).");
			}

			// Throw an error is a listener is requested but auth has been rejected
			if ((listenerUser != null)
				&& !auth)
				throw new Exception("Setting up a listener requires auth");

			// Set up the initial variables and their values
			HubConnection signalrListener = null;
			string jwt1 = null;
			string jwt2 = null;

			// Generate any necessary JWT's
			if (auth)
			{
				jwt1 = http != null
						? await Request.GetJwtViaHttpAsync(http, requestUser ?? null)
						: await Request.GetJwtViaRealtimeAsync(realtime, requestUser ?? null);
			}

			var listenerMethodsList = listeningFor.ToList();

			if (auth
				&& listenerMethodsList.Any()
				&& (realtime != null))
				jwt2 = await Request.GetJwtViaRealtimeAsync(realtime, listenerUser);

			// Set up a SignalR listener if desired (this hub connection must be different than the one used to make
			// the API request.
			if (listenerMethodsList.Any()
				&& (realtime != null))
			{
				signalrListener = await realtime(jwt2);

				// Subscribe to messages on the edition
				listenToEdition &= GetEditionId().HasValue;

				if (listenToEdition)
					await signalrListener.InvokeAsync("SubscribeToEdition", GetEditionId().Value);

				// Register listeners for messages returned by this API request
				foreach (var listener in listenerMethodsList)
				{
					if (_listenerDict.TryGetValue(listener, out var val))
						val.StartListener(signalrListener);
				}

				// Reload the listener if connection is lost
				signalrListener.Closed += async error =>
										  {
											  await signalrListener.StartAsync();

											  // Subscribe to messages on the edition
											  if (listenToEdition)
											  {
												  await signalrListener.InvokeAsync(
														  "SubscribeToEdition"
														  , GetEditionId().Value);
											  }

											  // Register listeners for messages returned by this API request
											  foreach (var listener in listenerMethodsList)
											  {
												  if (_listenerDict.TryGetValue(
														  listener
														  , out var val))
													  val.StartListener(signalrListener);
											  }
										  };
			}

			// Run the HTTP request if desired and available
			var httpObj = GetHttpRequestObject();

			if ((http != null)
				&& (httpObj != null))
			{
				(HttpResponseMessage, HttpResponseObject) =
						await Request.SendHttpRequestAsync<TInput, TOutput>(
								http
								, httpObj.RequestVerb
								, httpObj.RequestString
								, httpObj.Payload
								, jwt1);

				if (shouldSucceed)
					HttpResponseMessage.EnsureSuccessStatusCode();
			}

			// Run the SignalR request if desired
			if ((realtime != null) && requestRealtime)
			{
				HubConnection signalR = null;

				try
				{
					signalR = await realtime(jwt1);

					SignalrResponseObject = await SignalrRequest<TOutput>()(signalR);
				}
				catch (Exception)
				{
					if (shouldSucceed)
						throw;
				}

				// If the request should succeed and an HTTP request was also made, check that they are the same
				if (shouldSucceed
					&& deterministic
					&& (http != null))
					SignalrResponseObject.ShouldDeepEqual(HttpResponseObject);

				// Cleanup
				signalR?.DisposeAsync();
			}

			// If no listener is running, return the response from the request
			if (!listenerMethodsList.Any()
				|| (GetRequestVerb() == HttpMethod.Get))
				return;

			// Otherwise, wait up to 20 seconds for the listener to receive the message before giving up
			var waitTime = 0;

			while (shouldSucceed
				   && OutstandingListeners(listenerMethodsList)
				   && (waitTime < 20))
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
				waitTime += 1;
			}

			// Dispose of the listener and check to see that all expected responses have been received
			signalrListener?.DisposeAsync(); // Cleanup

			if (shouldSucceed)
			{
				foreach (var listenerMethod in listenerMethodsList)
				{
					Assert.True(_listenerDict.TryGetValue(listenerMethod, out var listener));

					Assert.False(listener.IsNull());
				}
			}
		}

		public async Task SendAsync(
				HttpClient                          http            = null
				, Func<string, Task<HubConnection>> realtime        = null
				, bool                              auth            = false
				, Request.UserAuthDetails           requestUser     = null
				, Request.UserAuthDetails           listenerUser    = null
				, bool                              shouldSucceed   = true
				, bool                              deterministic   = true
				, bool                              requestRealtime = true
				, bool                              listenToEdition = true
				, ListenerMethods?                  listeningFor    = null)
		{
			await SendAsync(
					listeningFor.HasValue
							? new List<ListenerMethods> { listeningFor.Value }
							: new List<ListenerMethods>()
					, http
					, realtime
					, auth
					, requestUser
					, listenerUser
					, shouldSucceed
					, deterministic
					, requestRealtime
					, listenToEdition);
		}

		private bool OutstandingListeners(List<ListenerMethods> listenerMethodsList)
		{
			foreach (var listenerMethod in listenerMethodsList)
			{
				_listenerDict.TryGetValue(listenerMethod, out var listener);

				if (listener.IsNull())
					return true;
			}

			return false;
		}

		public virtual HttpRequestObject<TInput> GetHttpRequestObject()
			=> new HttpRequestObject<TInput>
			{
					RequestVerb = _requestVerb
					, RequestString = HttpPath()
					, Payload = _payload
					,
			};

		public virtual Func<HubConnection, Task<T>> SignalrRequest<T>()
				where T : TOutput
		{
			return signalR => _payload == null
						   ? signalR.InvokeAsync<T>(SignalrRequestString())
						   : signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
		}

		public string GetListenerMethod() => ListenerMethod;

		public HttpMethod GetRequestVerb() => _requestVerb;

		public virtual uint? GetEditionId() => null;

		/// <summary>
		///  Returns the HTTP request string with all route and query
		///  parameters interpolated.
		/// </summary>
		/// <returns></returns>
		protected virtual string HttpPath() => RequestPath;

		/// <summary>
		///  Formats the API endpoint method name for the SignalR server
		/// </summary>
		/// <returns></returns>
		protected string SignalrRequestString()
			=> _requestVerb.ToString().First().ToString().ToUpper()
			   + _requestVerb.ToString().Substring(1).ToLower()
			   + RequestPath.Replace("/", "_").ToPascalCase();
	}

	/// <summary>
	///  An empty request payload object
	/// </summary>
	public class EmptyInput { }

	/// <summary>
	///  An empty request response object
	/// </summary>
	public class EmptyOutput { }
}
