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
///  Decorator that adds Polly retry logic to DatabaseAccessor for transient database errors.
/// </summary>
public class DatabaseAccessorRetryDecorator : IDatabaseAccessor
{
	private readonly IDatabaseAccessor _inner;
	private readonly AsyncRetryPolicy  _retryPolicy;

	public DatabaseAccessorRetryDecorator(IDatabaseAccessor inner)
	{
		_inner = inner;

		_retryPolicy = Policy.Handle<MySqlException>(ex => IsTransientError(ex))
							 .WaitAndRetryAsync(
									 10
									 , retryAttempt => TimeSpan.FromMilliseconds(50 * retryAttempt)
									 , (
											   exception
											   , timeSpan
											   , retryCount
											   , context) =>
									   {
										   Log.Warning(
												   "Database operation failed with transient error. Retry {RetryCount}/10 after {Delay}ms. Error: {ErrorMessage}"
												   , retryCount
												   , timeSpan.TotalMilliseconds
												   , exception.Message);
									   });
	}

	public bool InTransaction => _inner.InTransaction;

	// Apply the transient-error retry policy, but only when no transaction is open. Inside a
	// transaction the retry must happen at the transaction boundary (see WriteToDatabaseAsync),
	// never per statement: a transient failure such as a deadlock rolls back the whole transaction
	// on the server, so retrying a single statement would run it against a dead transaction and
	// could apply a partial, non-atomic write.
	private Task<T> WithRetry<T>(Func<Task<T>> operation)
		=> _inner.InTransaction
				? operation()
				: _retryPolicy.ExecuteAsync(operation);

	public void Dispose() => _inner.Dispose();

	public void BeginTransaction() => _inner.BeginTransaction();

	public Task BeginTransactionAsync() => _inner.BeginTransactionAsync();

	public void CommitTransaction() => _inner.CommitTransaction();

	public void RollbackTransaction() => _inner.RollbackTransaction();

	public Task<IList<T>> QueryAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		return WithRetry(() => _inner.QueryAsync<T>(
												 sql
												 , param
												 , commandTimeout
												 , commandType));
	}

	public Task<IList<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(
			string                           sql
			, Func<TFirst, TSecond, TReturn> map
			, object?                        param          = null
			, IDbTransaction?                transaction    = null
			, bool                           buffered       = true
			, string                         splitOn        = "Id"
			, int?                           commandTimeout = null
			, CommandType?                   commandType    = null)
	{
		return WithRetry(() => _inner.QueryAsync(
												 sql
												 , map
												 , param
												 , transaction
												 , buffered
												 , splitOn
												 , commandTimeout
												 , commandType));
	}

	public Task<IList<TReturn>> QueryAsync<TReturn>(
			string                    sql
			, Type[]                  types
			, Func<object[], TReturn> map
			, object?                 param          = null
			, IDbTransaction?         transaction    = null
			, bool                    buffered       = true
			, string                  splitOn        = "Id"
			, int?                    commandTimeout = null
			, CommandType?            commandType    = null)
	{
		return WithRetry(() => _inner.QueryAsync(
												 sql
												 , types
												 , map
												 , param
												 , transaction
												 , buffered
												 , splitOn
												 , commandTimeout
												 , commandType));
	}

	public Task<T> QueryFirstAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		return WithRetry(() => _inner.QueryFirstAsync<T>(
												 sql
												 , param
												 , commandTimeout
												 , commandType));
	}

	public Task<T?> QueryFirstOrDefaultAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		return WithRetry(() => _inner.QueryFirstOrDefaultAsync<T>(
												 sql
												 , param
												 , commandTimeout
												 , commandType));
	}

	public Task<T> QuerySingleAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		return WithRetry(() => _inner.QuerySingleAsync<T>(
												 sql
												 , param
												 , commandTimeout
												 , commandType));
	}

	public Task<T?> QuerySingleOrDefaultAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		return WithRetry(() => _inner.QuerySingleOrDefaultAsync<T>(
												 sql
												 , param
												 , commandTimeout
												 , commandType));
	}

	public Task<int> ExecuteAsync(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		return WithRetry(() => _inner.ExecuteAsync(
												 sql
												 , param
												 , commandTimeout
												 , commandType));
	}

	public Task<List<AlteredRecord>> WriteToDatabaseAsync(
			UserInfo                editionUser
			, List<MutationRequest> mutationRequests) =>

			// A write is a whole transaction (BeginTransaction .. Commit, rolled back on failure), so
			// it is safe and correct to retry it as a single unit: a transient failure replays every
			// statement atomically from a fresh transaction. When already nested inside another
			// transaction the outer boundary owns the retry, so WithRetry passes this through.
			WithRetry(() => _inner.WriteToDatabaseAsync(editionUser, mutationRequests));

	public Task<List<AlteredRecord>> WriteToDatabaseAsync(
			UserInfo          editionUser
			, MutationRequest mutationRequest) =>

			// See the list overload above: retried as one atomic transaction at the outermost boundary.
			WithRetry(() => _inner.WriteToDatabaseAsync(editionUser, mutationRequest));

	/// <summary>
	///  Determines if a MySqlException is transient and should be retried.
	/// </summary>
	private static bool IsTransientError(MySqlException ex)
	{
		// MySQL error codes that indicate transient errors
		return ex.ErrorCode switch
			   {
					   // Error 1205: Lock wait timeout exceeded
					   MySqlErrorCode.LockWaitTimeout => true

					   // Error 1213: Deadlock found when trying to get lock
					   , MySqlErrorCode.LockDeadlock => true

					   // Error 1203: User already has more than 'max_user_connections'
					   , MySqlErrorCode.ConnectionCountError => true

					   // Error 1192
					   , MySqlErrorCode.LockOrActiveTransaction => true

					   // Error 1040: Too many connections (not in enum)
					   , _ when ex.Number == 1040 => true
					   , _                        => false
					   ,
			   };
	}
}
