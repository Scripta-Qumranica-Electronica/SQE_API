﻿using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Polly;


namespace SQE.SqeHttpApi.DataAccess
{
    public class DbConnectionBase
    {
        private readonly IConfiguration _config;
        private readonly DatabaseCommunicationCircuitBreakPolicy _circuitBreakPolicy;

        protected DbConnectionBase(IConfiguration config)
        {
            _config = config;
            _circuitBreakPolicy = new DatabaseCommunicationCircuitBreakPolicy();
        }

        private string ConnectionString
        {
            get
            {
                var db = _config.GetConnectionString("MysqlDatabase");
                var host = _config.GetConnectionString("MysqlHost");
                var port = _config.GetConnectionString("MysqlPort");
                var user = _config.GetConnectionString("MysqlUsername");
                var pwd = _config.GetConnectionString("MysqlPassword");
                return $"server={host};port={port};database={db};username={user};password={pwd};charset=utf8;";
            }
        }

        // This returns the ReliableMySqlConnection, which wraps MySqlConnection in a set of retry policies for handling
        // the transient database errors where MariaDB says the transaction should be retried and pauses all attemtps
        // to get a connection from the database when it errors out more that 5 times trying to get a connection.
        protected IDbConnection OpenConnection() => new ReliableMySqlConnection(ConnectionString, _circuitBreakPolicy);
    }
    
    // https://sergeyakopov.com/reliable-database-connections-and-commands-with-polly/ provided many tips
    // for implementing this Polly retry and circuit breaker system.
    
    public interface ICircuitBreakPolicy
    {
        void ExecuteRetryWithCircuitBreaker(Action operation);

        TResult ExecuteRetryWithCircuitBreaker<TResult>(Func<TResult> operation);

        Task ExecuteRetryWithCircuitBreaker(Func<Task> operation, CancellationToken cancellationToken);

        Task<TResult> ExecuteRetryWithCircuitBreaker<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken);
    }
    
    /// <summary>
    /// These are the policies for retrying db executions on transient errors from database interactions and for
    /// pausing all attempts to get a connection when none are available from the database.
    /// 
    /// We only retry on the truly transient errors that explicitly suggest we should retry:
    /// 1205	HY000	ER_LOCK_WAIT_TIMEOUT	Lock wait timeout exceeded; try restarting transaction
    /// 1213	40001	ER_LOCK_DEADLOCK	Deadlock found when trying to get lock; try restarting transaction
    /// 1412	HY000	ER_TABLE_DEF_CHANGED	Table definition has changed, please retry transaction
    /// 
    /// TODO: Could we do anything useful for error 1927? Research and see if it should just be retried.
    /// </summary>
    public static class DatabaseCommunicationRetryPolicy
    {
        private const int RetryCount = 5;
        private const int WaitBetweenRetriesInMilliseconds = 200;
        private static readonly Random _random = new Random();

        private static readonly List<uint> _retrySqlExceptions = new List<uint>{ 1205, 1213, 1412 };

        private static readonly AsyncPolicy _retryPolicyAsync = Policy
            .Handle<MySqlException>(exception => _retrySqlExceptions.Contains(exception.Code))
            .WaitAndRetryAsync(
                retryCount: RetryCount,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(_waitTime(attempt))
            );
        private static readonly Policy _retryPolicy = Policy
            .Handle<MySqlException>(exception => _retrySqlExceptions.Contains(exception.Code))
            .WaitAndRetry(
                retryCount: RetryCount,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(_waitTime(attempt))
            );

        /// <summary>
        /// Each retry waits a bit longer (retryCount * WaitBetweenRetriesInMilliseconds), and a small amount of randomness
        /// is added to that wait time (somewhere between -100 and 100ms) so that competing commands (deadlock) are less
        /// likely to keep colliding with each other.
        /// </summary>
        /// <param name="retryCount">The current count of retries</param>
        /// <returns></returns>
        private static int _waitTime(int retryCount) =>
            (retryCount * WaitBetweenRetriesInMilliseconds) + _random.Next(-100, 100);

        public static void ExecuteRetry(Action operation)
        {
            _retryPolicy.Execute(operation.Invoke);
        }

        public static TResult ExecuteRetry<TResult>(Func<TResult> operation)
        {
            return _retryPolicy.Execute(operation.Invoke);
        }

        public static async Task ExecuteRetry(Func<Task> operation, CancellationToken cancellationToken)
        {
            await _retryPolicyAsync.ExecuteAsync(operation.Invoke);
        }

        public static async Task<TResult> ExecuteRetry<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken)
        {
            return await _retryPolicyAsync.ExecuteAsync(operation.Invoke);
        }
    }
    
    /// <summary>
    /// In both sync and async any command run with ExecuteRetry will be retried a maximum of 5 times when the
    /// exception MariaDb errors 1205, 1213, or 1412 are received (all other exceptions bubble up immediately).
    /// After max-retries the exception is allowed to bubble up.
    /// 
    /// In both sync and async any command run with ExecuteRetryWithCircuitBreaker will be retried a maximum of
    /// 5 times when the exception MariaDb errors 1040 or 1203 are received, after which any attempt to run that
    /// command will immediately error without even attempting to run it for CircuitBreakerPause seconds.
    ///
    /// TODO: log this activity when the system logger is set up.
    /// </summary>
    public class DatabaseCommunicationCircuitBreakPolicy
    {
        private const int RetryCount = 5;
        private const int WaitBetweenRetriesInMilliseconds = 200;
        private const int CircuitBreakerPause = 5;
        private readonly Random _random = new Random();
        private readonly AsyncPolicy _circuitBreakerRetryPolicyAsync;
        private readonly Policy _circuitBreakerRetryPolicy;
        private readonly AsyncPolicy _circuitBreakPolicyAsync;
        private readonly Policy _circuitBreakPolicy;

        private static readonly List<uint> _pauseExceptions = new List<uint>{ 1040, 1203 };
        public DatabaseCommunicationCircuitBreakPolicy()
        {
            _circuitBreakerRetryPolicyAsync = Policy
                .Handle<MySqlException>(exception => _pauseExceptions.Contains(exception.Code))
                .WaitAndRetryAsync(
                    retryCount: RetryCount,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(_waitTime(attempt))
                );
            
            _circuitBreakerRetryPolicy = Policy
                .Handle<MySqlException>(exception => _pauseExceptions.Contains(exception.Code))
                .WaitAndRetry(
                    retryCount: RetryCount,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(_waitTime(attempt))
                );

            _circuitBreakPolicyAsync = Policy
                .Handle<MySqlException>(exception => _pauseExceptions.Contains(exception.Code))
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: RetryCount, 
                    durationOfBreak: TimeSpan.FromSeconds(CircuitBreakerPause)
                );
            
            _circuitBreakPolicy = Policy
                .Handle<MySqlException>(exception => _pauseExceptions.Contains(exception.Code))
                .CircuitBreaker(
                    exceptionsAllowedBeforeBreaking: RetryCount, 
                    durationOfBreak: TimeSpan.FromSeconds(CircuitBreakerPause)
                );
        }

        /// <summary>
        /// Each retry waits a bit longer (retryCount * WaitBetweenRetriesInMilliseconds), and a small amount of randomness
        /// is added to that wait time (somewhere between -100 and 100ms) so that competing commands (deadlock) are less
        /// likely to keep colliding with each other.
        /// </summary>
        /// <param name="retryCount">The current count of retries</param>
        /// <returns></returns>
        private int _waitTime(int retryCount) =>
            (retryCount * WaitBetweenRetriesInMilliseconds) + _random.Next(-100, 100);
        
        public void ExecuteRetryWithCircuitBreaker(Action operation)
        {
            _circuitBreakerRetryPolicy.Wrap(_circuitBreakPolicy).Execute(operation.Invoke);
        }

        public TResult ExecuteRetryWithCircuitBreaker<TResult>(Func<TResult> operation)
        {
            return _circuitBreakerRetryPolicy.Wrap(_circuitBreakPolicy).Execute(operation.Invoke);
        }

        public async Task ExecuteRetryWithCircuitBreaker(Func<Task> operation, CancellationToken cancellationToken)
        {
            await _circuitBreakerRetryPolicyAsync.WrapAsync(_circuitBreakPolicyAsync).ExecuteAsync(operation.Invoke);
        }

        public async Task<TResult> ExecuteRetryWithCircuitBreaker<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken)
        {
            return await _circuitBreakerRetryPolicyAsync.WrapAsync(_circuitBreakPolicyAsync).ExecuteAsync(operation.Invoke);
        }
    }

    /// <summary>
    /// This wraps the DbConnection provided by MySqlConnection and adds the retry policies of
    /// DatabaseCommunicationRetryPolicy to the Open() method in order to deal with transient errors that
    /// prevent betting a database connection. It also overrides the CreateDbCommand to make it use
    /// ReliableMySqlDbCommand, which is a wrapper for the DbCommand implementation of MySqlCommand that
    /// also wraps those interactions in the retry policy.
    /// </summary>
    public class ReliableMySqlConnection : DbConnection
    {
        private readonly MySqlConnection _underlyingConnection;
        private readonly DatabaseCommunicationCircuitBreakPolicy _circuitBreakPolicy;

        private string _connectionString;

        public ReliableMySqlConnection(DatabaseCommunicationCircuitBreakPolicy circuitBreakPolicy)
        {
            _circuitBreakPolicy = circuitBreakPolicy;
            _underlyingConnection = new MySqlConnection(ConnectionString);
        }
        
        public ReliableMySqlConnection(string connectionString, DatabaseCommunicationCircuitBreakPolicy circuitBreakPolicy) : this(circuitBreakPolicy)
        {
            ConnectionString = connectionString;
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

        public override void Close() => _underlyingConnection.Close();
        
        // Force usage of the ReliableMySqlDbCommand implementation of DbCommand
        protected override DbCommand CreateDbCommand()
        {
            return new ReliableMySqlDbCommand(_underlyingConnection.CreateCommand());
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_underlyingConnection.State == ConnectionState.Open)
                {
                    _underlyingConnection.Close();
                }

                _underlyingConnection.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        // Wrap the MySqlConnection Open() method in the retry policy with circuit breaker.
        public override void Open()
        {
            _circuitBreakPolicy.ExecuteRetryWithCircuitBreaker(() =>  _underlyingConnection.Open());
        }
    }
    
    /// <summary>
    /// This provides a wrapper for the MySqlCommand implementation of DbCommand in order to wrap some methods
    /// in the retry policy.
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
            set => _underlyingSqlCommand.Connection = (MySqlConnection)value; // Cast to a MySqlConnection for safety
        }

        protected override DbParameterCollection DbParameterCollection => _underlyingSqlCommand.Parameters;

        protected override DbTransaction DbTransaction
        {
            get => _underlyingSqlCommand.Transaction;
            set => _underlyingSqlCommand.Transaction = (MySqlTransaction)value; // Cast to a MySqlTransaction for safety
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
        
        protected override DbParameter CreateDbParameter() => _underlyingSqlCommand.CreateParameter();
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _underlyingSqlCommand.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        // Wrap ExecuteReader in the retry policy.
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return DatabaseCommunicationRetryPolicy.ExecuteRetry(() => _underlyingSqlCommand.ExecuteReader(behavior));
        }

        // Wrap ExecuteReaderAsync in the retry policy.
        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken token)
        {
            return DatabaseCommunicationRetryPolicy.ExecuteRetry(() => _underlyingSqlCommand.ExecuteReaderAsync(behavior, token), token);
        }

        // Wrap ExecuteNonQuery in the retry policy.
        public override int ExecuteNonQuery()
        {
            return DatabaseCommunicationRetryPolicy.ExecuteRetry(() => _underlyingSqlCommand.ExecuteNonQuery());
        }
        
        // Wrap ExecuteNonQueryAsync in the retry policy.
        public override Task<int> ExecuteNonQueryAsync(CancellationToken token)
        {
            return DatabaseCommunicationRetryPolicy.ExecuteRetry(() => _underlyingSqlCommand.ExecuteNonQueryAsync(token), token);
        }

        // Wrap ExecuteScalar in the retry policy.
        public override object ExecuteScalar()
        {
            return DatabaseCommunicationRetryPolicy.ExecuteRetry(() => _underlyingSqlCommand.ExecuteScalar());
        }
        
        // Wrap ExecuteScalarAsync in the retry policy.
        public override Task<object> ExecuteScalarAsync(CancellationToken token)
        {
            return DatabaseCommunicationRetryPolicy.ExecuteRetry(() => _underlyingSqlCommand.ExecuteScalarAsync(token), token);
        }

        public override void Prepare()
        {
            DatabaseCommunicationRetryPolicy.ExecuteRetry(() => _underlyingSqlCommand.Prepare());
        }
    }
}
