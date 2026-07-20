using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace SQE.DatabaseAccess;

public class DbConnectionBase
{
	private readonly IConfiguration _config;
	private          IDbConnection  _connection;
	private          bool           _inTransaction;
	private          IDbTransaction _transaction;

	protected DbConnectionBase(IConfiguration config) => _config = config;

	private string ConnectionString
	{
		get
		{
			var db = _config.GetConnectionString("MysqlDatabase");
			var host = _config.GetConnectionString("MysqlHost");
			var port = _config.GetConnectionString("MysqlPort");
			var user = _config.GetConnectionString("MysqlUsername");
			var pwd = _config.GetConnectionString("MysqlPassword");

			return $"server={
				host
			};port={
				port
			};database={
				db
			};username={
				user
			};password={
				pwd
			};charset=utf8mb4;AllowUserVariables=True;Pooling=true;MinPoolSize=2;MaxPoolSize=4;";
		}
	}

	// This returns the ReliableMySqlConnection, which wraps MySqlConnection in a set of retry policies for handling
	// the transient database errors where MariaDB says the transaction should be retried and pauses all attemtps
	// to get a connection from the database when it errors out more that 5 times trying to get a connection.
	protected IDbConnection Connection => _connection ??= new MySqlConnection(ConnectionString);

	protected IDbConnection OpenConnection() => Connection;

	public void BeginTransaction()
	{
		if (_inTransaction)
			return;

		_transaction = Connection.BeginTransaction();
		_inTransaction = true;
	}

	public void Commit()
	{
		if (!_inTransaction)
			return;

		_transaction?.Commit();
		_transaction?.Dispose();
		_inTransaction = false;
	}

	public void Dispose()
	{
		_transaction?.Dispose();
		_connection?.Dispose();
	}

	/// <summary>
	///  Gets a managed connection that will either use the provided connection or create a new one.
	///  If a new connection is created, it will be disposed when the ManagedConnection is disposed.
	///  If an existing connection is provided, it will not be disposed.
	/// </summary>
	/// <param name="connection">Optional existing connection to use</param>
	/// <returns>A ManagedConnection that handles disposal correctly</returns>
	protected ManagedConnection GetManagedConnection(IDbConnection connection = null)
		=> new(connection);

	/// <summary>
	///  Manages a database connection, either using a provided connection or creating a new one.
	///  Only disposes the connection if it was created by this struct (not if it was provided).
	/// </summary>
	protected struct ManagedConnection : IDisposable
	{
		private readonly bool _shouldDispose;

		public IDbConnection Connection { get; }

		public ManagedConnection(IDbConnection providedConnection)
		{
			if (providedConnection != null)
			{
				Connection = providedConnection;
				_shouldDispose = false;
			}
			else
			{
				Connection = Connection;
				_shouldDispose = true;
			}
		}

		public void Dispose()
		{
			if (_shouldDispose)
				Connection?.Dispose();
		}
	}
}
