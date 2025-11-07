#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using Polly;
using Polly.Retry;
using Serilog;
using SQE.DatabaseAccess.Models;

namespace SQE.DatabaseAccess.Helpers;

/// <summary>
/// Decorator that adds Polly retry logic to DatabaseAccessor for transient database errors.
/// </summary>
public class DatabaseAccessorRetryDecorator : IDatabaseAccessor
{
	private readonly IDatabaseAccessor _inner;
	private readonly AsyncRetryPolicy _retryPolicy;

	public DatabaseAccessorRetryDecorator(IDatabaseAccessor inner)
	{
		_inner = inner;

		_retryPolicy = Policy
			.Handle<MySqlException>(ex => IsTransientError(ex))
			.WaitAndRetryAsync(
				retryCount: 10,
				sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(50 * retryAttempt),
				onRetry: (exception, timeSpan, retryCount, context) =>
				{
					Log.Warning(
						"Database operation failed with transient error. Retry {RetryCount}/10 after {Delay}ms. Error: {ErrorMessage}",
						retryCount,
						timeSpan.TotalMilliseconds,
						exception.Message
					);
				}
			);
	}

	/// <summary>
	/// Determines if a MySqlException is transient and should be retried.
	/// </summary>
	private static bool IsTransientError(MySqlException ex)
	{
		// MySQL error codes that indicate transient errors
		return ex.ErrorCode switch
		{
			MySqlErrorCode.LockWaitTimeout => true,           // Error 1205: Lock wait timeout exceeded
			MySqlErrorCode.LockDeadlock => true,              // Error 1213: Deadlock found when trying to get lock
			MySqlErrorCode.ConnectionCountError => true,      // Error 1203: User already has more than 'max_user_connections'
			MySqlErrorCode.LockOrActiveTransaction => true,
			_ when ex.Number == 1040 => true,                 // Error 1040: Too many connections (not in enum)
			_ => false,
		};
	}

	public void Dispose() => _inner.Dispose();

	public void BeginTransaction() => _inner.BeginTransaction();

	public Task BeginTransactionAsync() => _inner.BeginTransactionAsync();

	public void CommitTransaction() => _inner.CommitTransaction();

	public void RollbackTransaction() => _inner.RollbackTransaction();

	public Task<IList<T>> QueryAsync<T>(
		string sql,
		object? param = null,
		int? commandTimeout = null,
		CommandType? commandType = null)
	{
		return _retryPolicy.ExecuteAsync(() =>
			_inner.QueryAsync<T>(sql, param, commandTimeout, commandType));
	}

	public Task<IList<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(
		string sql,
		Func<TFirst, TSecond, TReturn> map,
		object? param = null,
		IDbTransaction? transaction = null,
		bool buffered = true,
		string splitOn = "Id",
		int? commandTimeout = null,
		CommandType? commandType = null)
	{
		return _retryPolicy.ExecuteAsync(() =>
			_inner.QueryAsync(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
	}

	public Task<IList<TReturn>> QueryAsync<TReturn>(
		string sql,
		Type[] types,
		Func<object[], TReturn> map,
		object? param = null,
		IDbTransaction? transaction = null,
		bool buffered = true,
		string splitOn = "Id",
		int? commandTimeout = null,
		CommandType? commandType = null)
	{
		return _retryPolicy.ExecuteAsync(() =>
			_inner.QueryAsync(sql, types, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
	}

	public Task<T> QueryFirstAsync<T>(
		string sql,
		object? param = null,
		int? commandTimeout = null,
		CommandType? commandType = null)
	{
		return _retryPolicy.ExecuteAsync(() =>
			_inner.QueryFirstAsync<T>(sql, param, commandTimeout, commandType));
	}

	public Task<T?> QueryFirstOrDefaultAsync<T>(
		string sql,
		object? param = null,
		int? commandTimeout = null,
		CommandType? commandType = null)
	{
		return _retryPolicy.ExecuteAsync(() =>
			_inner.QueryFirstOrDefaultAsync<T>(sql, param, commandTimeout, commandType));
	}

	public Task<T> QuerySingleAsync<T>(
		string sql,
		object? param = null,
		int? commandTimeout = null,
		CommandType? commandType = null)
	{
		return _retryPolicy.ExecuteAsync(() =>
			_inner.QuerySingleAsync<T>(sql, param, commandTimeout, commandType));
	}

	public Task<T?> QuerySingleOrDefaultAsync<T>(
		string sql,
		object? param = null,
		int? commandTimeout = null,
		CommandType? commandType = null)
	{
		return _retryPolicy.ExecuteAsync(() =>
			_inner.QuerySingleOrDefaultAsync<T>(sql, param, commandTimeout, commandType));
	}

	public Task<int> ExecuteAsync(
		string sql,
		object? param = null,
		int? commandTimeout = null,
		CommandType? commandType = null)
	{
		return _retryPolicy.ExecuteAsync(() =>
			_inner.ExecuteAsync(sql, param, commandTimeout, commandType));
	}

	public Task<List<AlteredRecord>> WriteToDatabaseAsync(
		UserInfo editionUser,
		List<MutationRequest> mutationRequests)
	{
		// Note: Write operations typically should NOT be retried automatically due to potential side effects.
		// We're passing these through without retry to avoid duplicate writes.
		return _inner.WriteToDatabaseAsync(editionUser, mutationRequests);
	}

	public Task<List<AlteredRecord>> WriteToDatabaseAsync(
		UserInfo editionUser,
		MutationRequest mutationRequest)
	{
		// Note: Write operations typically should NOT be retried automatically due to potential side effects.
		// We're passing these through without retry to avoid duplicate writes.
		return _inner.WriteToDatabaseAsync(editionUser, mutationRequest);
	}
}
