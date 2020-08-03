using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Serilog;
using SQE.DatabaseAccess.Helpers;

namespace SQE.DatabaseAccess
{
    /// <summary>
    ///     This wraps the DbConnection provided by MySqlConnection and adds the retry policies of
    ///     DatabaseCommunicationRetryPolicy to the Open() method in order to deal with transient errors that
    ///     prevent betting a database connection. It also overrides the CreateDbCommand to make it use
    ///     ReliableMySqlDbCommand, which is a wrapper for the DbCommand implementation of MySqlCommand that
    ///     also wraps those interactions in the retry policy.
    /// </summary>
    public class ReliableMySqlConnection : DbConnection
    {
        private readonly Retries.DatabaseCommunicationCircuitBreakPolicy _circuitBreakPolicy = new Retries.DatabaseCommunicationCircuitBreakPolicy();
        private readonly MySqlConnection _underlyingConnection;

        private string _connectionString;

        public ReliableMySqlConnection(string connectionString)
        {
            Log.Information($"Starting new connection: {connectionString}");
            _connectionString = connectionString;
            _underlyingConnection = new MySqlConnection(connectionString);
        }

        // Set the _connectionString for this class and the ConnectionString of the underlying MySqlConnection.
        public sealed override string ConnectionString
        {
            get => _connectionString;

            set
            {
                _connectionString = value;
                _underlyingConnection.ConnectionString = value;
            }
        }

        public override int ConnectionTimeout => _underlyingConnection.ConnectionTimeout;

        public override string Database => _underlyingConnection.Database;

        public override string DataSource => _underlyingConnection.DataSource;

        public override string ServerVersion => _underlyingConnection.ServerVersion;

        public override ConnectionState State => _underlyingConnection.State;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return _underlyingConnection.BeginTransaction(isolationLevel);
        }

        public override void ChangeDatabase(string databaseName)
        {
            _underlyingConnection.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
            _underlyingConnection.Close();
        }

        // Force usage of the ReliableMySqlDbCommand implementation of DbCommand
        protected override DbCommand CreateDbCommand()
        {
            return new ReliableMySqlDbCommand(_underlyingConnection.CreateCommand());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_underlyingConnection.State == ConnectionState.Open) _underlyingConnection.Close();

                _underlyingConnection.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        // Wrap the MySqlConnection Open() method in the retry policy with circuit breaker.
        public override void Open()
        {
            _circuitBreakPolicy.ExecuteRetryWithCircuitBreaker(() => _underlyingConnection.Open());
        }
    }

    /// <summary>
    ///     This provides a wrapper for the MySqlCommand implementation of DbCommand in order to wrap some methods
    ///     in the retry policy.
    /// </summary>
    public class ReliableMySqlDbCommand : DbCommand
    {
        private readonly MySqlCommand _underlyingSqlCommand;

        public ReliableMySqlDbCommand(MySqlCommand command)
        {
            _underlyingSqlCommand = command;
        }

        public ReliableMySqlDbCommand(MySqlCommand command, DbConnection connection) : this(command)
        {
            DbConnection = connection;
        }

        public ReliableMySqlDbCommand(MySqlCommand command, DbConnection connection, DbTransaction transaction)
            : this(command, connection)
        {
            DbTransaction = transaction;
        }

        public override string CommandText
        {
            get => _underlyingSqlCommand.CommandText;
            set => _underlyingSqlCommand.CommandText = value;
        }

        public override int CommandTimeout
        {
            get => _underlyingSqlCommand.CommandTimeout;
            set => _underlyingSqlCommand.CommandTimeout = value;
        }

        public override CommandType CommandType
        {
            get => _underlyingSqlCommand.CommandType;
            set => _underlyingSqlCommand.CommandType = value;
        }

        protected override DbConnection DbConnection
        {
            get => _underlyingSqlCommand.Connection;
            set => _underlyingSqlCommand.Connection =
                (MySqlConnection)value; // Cast to a MySqlConnection for safety
        }

        protected override DbParameterCollection DbParameterCollection => _underlyingSqlCommand.Parameters;

        protected override DbTransaction DbTransaction
        {
            get => _underlyingSqlCommand.Transaction;
            set => _underlyingSqlCommand.Transaction =
                (MySqlTransaction)value; // Cast to a MySqlTransaction for safety
        }

        public override bool DesignTimeVisible
        {
            get => _underlyingSqlCommand.DesignTimeVisible;
            set => _underlyingSqlCommand.DesignTimeVisible = value;
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get => _underlyingSqlCommand.UpdatedRowSource;
            set => _underlyingSqlCommand.UpdatedRowSource = value;
        }

        public override void Cancel()
        {
            _underlyingSqlCommand.Cancel();
        }

        protected override DbParameter CreateDbParameter()
        {
            return _underlyingSqlCommand.CreateParameter();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _underlyingSqlCommand.Dispose();

            GC.SuppressFinalize(this);
        }

        // Wrap ExecuteReader in the retry policy.
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return Retries.DatabaseCommunicationRetryPolicy.ExecuteRetry(
                () => _underlyingSqlCommand.ExecuteReader(behavior));
        }

        // Wrap ExecuteReaderAsync in the retry policy.
        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior,
            CancellationToken token)
        {
            return Retries.DatabaseCommunicationRetryPolicy.ExecuteRetry(
                () => _underlyingSqlCommand.ExecuteReaderAsync(behavior, token),
                token
            );
        }

        // Wrap ExecuteNonQuery in the retry policy.
        public override int ExecuteNonQuery()
        {
            return Retries.DatabaseCommunicationRetryPolicy.ExecuteRetry(() => _underlyingSqlCommand.ExecuteNonQuery());
        }

        // Wrap ExecuteNonQueryAsync in the retry policy.
        public override Task<int> ExecuteNonQueryAsync(CancellationToken token)
        {
            return Retries.DatabaseCommunicationRetryPolicy.ExecuteRetry(
                () => _underlyingSqlCommand.ExecuteNonQueryAsync(token),
                token
            );
        }

        // Wrap ExecuteScalar in the retry policy.
        public override object ExecuteScalar()
        {
            return Retries.DatabaseCommunicationRetryPolicy.ExecuteRetry(() => _underlyingSqlCommand.ExecuteScalar());
        }

        // Wrap ExecuteScalarAsync in the retry policy.
        public override Task<object> ExecuteScalarAsync(CancellationToken token)
        {
            return Retries.DatabaseCommunicationRetryPolicy.ExecuteRetry(
                () => _underlyingSqlCommand.ExecuteScalarAsync(token),
                token
            );
        }

        public override void Prepare()
        {
            Retries.DatabaseCommunicationRetryPolicy.ExecuteRetry(() => _underlyingSqlCommand.Prepare());
        }
    }
}