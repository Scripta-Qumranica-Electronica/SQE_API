#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using SQE.DatabaseAccess.Models;

namespace SQE.DatabaseAccess.Helpers;

public interface IDatabaseAccessor
{
	void Dispose();
	void BeginTransaction();
	Task BeginTransactionAsync();
	void CommitTransaction();
	void RollbackTransaction();

	Task<IList<T>> QueryAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null);

	Task<IList<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(
			string                           sql
			, Func<TFirst, TSecond, TReturn> map
			, object?                        param          = null
			, IDbTransaction?                transaction    = null
			, bool                           buffered       = true
			, string                         splitOn        = "Id"
			, int?                           commandTimeout = null
			, CommandType?                   commandType    = null);

	Task<IList<TReturn>> QueryAsync<TReturn>(
			string                    sql
			, Type[]                  types
			, Func<object[], TReturn> map
			, object?                 param          = null
			, IDbTransaction?         transaction    = null
			, bool                    buffered       = true
			, string                  splitOn        = "Id"
			, int?                    commandTimeout = null
			, CommandType?            commandType    = null);

	Task<T> QueryFirstAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null);

	Task<T?> QueryFirstOrDefaultAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null);

	Task<T> QuerySingleAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null);

	Task<T?> QuerySingleOrDefaultAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null);

	Task<int> ExecuteAsync(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null);

	Task<List<AlteredRecord>> WriteToDatabaseAsync(
			UserInfo                editionUser
			, List<MutationRequest> mutationRequests);

	Task<List<AlteredRecord>> WriteToDatabaseAsync(
			UserInfo          editionUser
			, MutationRequest mutationRequest);
}

public class DatabaseAccessor(IDatabaseManager dbm, IDatabaseWriter dbw) : IDatabaseAccessor
{
	private readonly SemaphoreSlim   _transactionLock = new(1, 1);
	private          DbConnection?   _connection;
	private          IDbTransaction? _transaction;
	private          uint            _transactionNest;

	public void Dispose()
	{
		_transaction?.Dispose();
		_connection?.Close();
		_connection?.Dispose();
		_transactionLock.Dispose();
	}

	public void BeginTransaction()
	{
		_transactionLock.Wait();

		try
		{
			var conn = GetConnection();

			if (_transactionNest == 0)
				_transaction = conn.BeginTransaction();

			_transactionNest++;
		}
		finally
		{
			_transactionLock.Release();
		}
	}

	public async Task BeginTransactionAsync()
	{
		await _transactionLock.WaitAsync();

		try
		{
			var conn = await GetConnectionAsync();

			if (_transactionNest == 0)
				_transaction = await conn.BeginTransactionAsync();

			_transactionNest++;
		}
		finally
		{
			_transactionLock.Release();
		}
	}

	public void CommitTransaction()
	{
		_transactionLock.Wait();

		try
		{
			_transactionNest--;

			if (_transactionNest != 0)
				return;

			_transaction?.Commit();
			_transaction?.Dispose();
			_transaction = null;

			// Close and dispose connection to return to pool immediately
			CloseConnection();
		}
		finally
		{
			_transactionLock.Release();
		}
	}

	public void RollbackTransaction()
	{
		_transactionLock.Wait();

		try
		{
			_transaction?.Rollback();
			_transaction?.Dispose();
			_transaction = null;
			_transactionNest = 0; // Reset on rollback

			// Close and dispose connection to return to pool immediately
			_connection?.Close();
			_connection?.Dispose();
			_connection = null;
		}
		finally
		{
			_transactionLock.Release();
		}
	}

	public async Task<IList<T>> QueryAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		var conn = await GetConnectionAsync();

		try
		{
			var result = (await conn.QueryAsync<T>(
					sql
					, param
					, _transaction
					, commandTimeout
					, commandType)).ToList();

			return result;
		}
		finally
		{
			await CloseConnectionAsync();
		}
	}

	public async Task<IList<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(
			string                           sql
			, Func<TFirst, TSecond, TReturn> map
			, object?                        param          = null
			, IDbTransaction?                transaction    = null
			, bool                           buffered       = true
			, string                         splitOn        = "Id"
			, int?                           commandTimeout = null
			, CommandType?                   commandType    = null)
	{
		var conn = await GetConnectionAsync();

		try
		{
			var result = (await conn.QueryAsync(
					sql
					, map
					, param
					, _transaction
					, buffered
					, splitOn
					, commandTimeout
					, commandType)).ToList();

			return result;
		}
		finally
		{
			await CloseConnectionAsync();
		}
	}

	public async Task<IList<TReturn>> QueryAsync<TReturn>(
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
		var conn = await GetConnectionAsync();

		try
		{
			var result = (await conn.QueryAsync(
					sql
					, types
					, map
					, param
					, _transaction
					, buffered
					, splitOn
					, commandTimeout
					, commandType)).ToList();

			return result;
		}
		finally
		{
			await CloseConnectionAsync();
		}
	}

	public async Task<T> QueryFirstAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		var conn = await GetConnectionAsync();

		try
		{
			var result = await conn.QueryFirstAsync<T>(
					sql
					, param
					, _transaction
					, commandTimeout
					, commandType);

			return result;
		}
		finally
		{
			await CloseConnectionAsync();
		}
	}

	public async Task<T?> QueryFirstOrDefaultAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		var conn = await GetConnectionAsync();

		try
		{
			var result = await conn.QueryFirstOrDefaultAsync<T>(
					sql
					, param
					, _transaction
					, commandTimeout
					, commandType);

			return result;
		}
		finally
		{
			await CloseConnectionAsync();
		}
	}

	public async Task<T> QuerySingleAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		var conn = await GetConnectionAsync();

		try
		{
			var result = await conn.QuerySingleAsync<T>(
					sql
					, param
					, _transaction
					, commandTimeout
					, commandType);

			return result;
		}
		finally
		{
			await CloseConnectionAsync();
		}
	}

	public async Task<T?> QuerySingleOrDefaultAsync<T>(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		var conn = await GetConnectionAsync();

		try
		{
			var result = await conn.QuerySingleOrDefaultAsync<T>(
					sql
					, param
					, _transaction
					, commandTimeout
					, commandType);

			return result;
		}
		finally
		{
			await CloseConnectionAsync();
		}
	}

	public async Task<int> ExecuteAsync(
			string         sql
			, object?      param          = null
			, int?         commandTimeout = null
			, CommandType? commandType    = null)
	{
		var conn = await GetConnectionAsync();

		try
		{
			var result = await conn.ExecuteAsync(
					sql
					, param
					, _transaction
					, commandTimeout
					, commandType);

			return result;
		}
		finally
		{
			await CloseConnectionAsync();
		}
	}

	public async Task<List<AlteredRecord>> WriteToDatabaseAsync(
			UserInfo                editionUser
			, List<MutationRequest> mutationRequests)
	{
		await BeginTransactionAsync();

		try
		{
			var result = await dbw.WriteToDatabaseAsync(editionUser, mutationRequests, this);
			CommitTransaction();

			return result;
		}
		catch
		{
			RollbackTransaction();

			throw;
		}
	}

	public async Task<List<AlteredRecord>> WriteToDatabaseAsync(
			UserInfo          editionUser
			, MutationRequest mutationRequest)
	{
		await BeginTransactionAsync();

		try
		{
			var result = await dbw.WriteToDatabaseAsync(editionUser, mutationRequest, this);
			CommitTransaction();

			return result;
		}
		catch
		{
			RollbackTransaction();

			throw;
		}
	}

	private DbConnection GetConnection()
	{
		_connection ??= dbm.GetConnection();

		if (_connection.State != ConnectionState.Open)
			_connection.Open();

		return _connection;
	}

	private async Task<DbConnection> GetConnectionAsync()
	{
		_connection ??= await dbm.GetConnectionAsync();

		if (_connection.State != ConnectionState.Open)
			await _connection.OpenAsync();

		return _connection;
	}

	private void CloseConnection()
	{
		if (_connection == null
			|| _transaction != null)
			return;

		_connection.Close();
		_connection.Dispose();
		_connection = null;
	}

	private async Task CloseConnectionAsync()
	{
		if (_connection == null
			|| _transaction != null)
			return;

		await _connection.CloseAsync();
		_connection.Dispose();
		_connection = null;
	}
}
