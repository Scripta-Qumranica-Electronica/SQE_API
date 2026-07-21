using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services;

/// <summary>
///  Caches the (large, expensive) list of published editions in memory.
///
///  The published-edition list only changes when an edition is published or archived — both rare,
///  deliberate, admin-only events (in practice less than once a day). A public edition is frozen
///  forever, so the list is safe to build once and reuse until it is explicitly invalidated.
///
///  Invalidation is broadcast across API instances over the existing Redis backplane: when one
///  server publishes or archives an edition it publishes to a channel, and every server (including
///  the one that made the change) drops its cached copy and rebuilds it lazily on the next request.
///  When Redis is not configured (single-server / dev / IntegrationTests) the cache still works
///  locally; a direct DB publish is then picked up simply by restarting the server.
/// </summary>
public interface IPublishedEditionsCache
{
	/// <summary>
	///  Returns the cached published-edition list, building it via <paramref name="build" /> on a
	///  cache miss. Concurrent first-callers share a single build.
	/// </summary>
	Task<IReadOnlyList<Edition>> GetAsync(Func<Task<IEnumerable<Edition>>> build);

	/// <summary>
	///  Drops the cached list locally and, if Redis is configured, tells every other API instance
	///  to do the same. Call this after an edition is published or archived.
	/// </summary>
	Task NotifyChangedAsync();
}

public class PublishedEditionsCache : IPublishedEditionsCache
{
	private const string _channelName = "published-editions:invalidate";

	private readonly object      _lock = new();
	private readonly ISubscriber _subscriber;

	private volatile Task<IReadOnlyList<Edition>> _cache;

	public PublishedEditionsCache(IConnectionMultiplexer redis = null)
	{
		if (redis == null)
			return;

		_subscriber = redis.GetSubscriber();

		// A message on this channel means some server changed the published set: drop our copy.
		_subscriber.Subscribe(RedisChannel.Literal(_channelName), (_, _) => Clear());
	}

	public Task<IReadOnlyList<Edition>> GetAsync(Func<Task<IEnumerable<Edition>>> build)
	{
		var cached = _cache;

		if (cached != null)
			return cached;

		lock (_lock)
		{
			// Cache the in-flight Task (not just its result) so concurrent first-callers share a
			// single build instead of stampeding the database.
			_cache ??= BuildAsync(build);

			return _cache;
		}
	}

	public async Task NotifyChangedAsync()
	{
		Clear();

		if (_subscriber != null)
			await _subscriber.PublishAsync(RedisChannel.Literal(_channelName), "1");
	}

	private void Clear() => _cache = null;

	private async Task<IReadOnlyList<Edition>> BuildAsync(Func<Task<IEnumerable<Edition>>> build)
	{
		try
		{
			return (await build()).ToList();
		}
		catch
		{
			// Never cache a failed build; let the next request retry.
			Clear();

			throw;
		}
	}
}
