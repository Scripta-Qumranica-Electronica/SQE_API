using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Polly;
using Serilog;

namespace SQE.DatabaseAccess.Helpers
{
    public static class Retries
    {
        // https://sergeyakopov.com/reliable-database-connections-and-commands-with-polly/ provided many tips
        // for implementing this Polly retry and circuit breaker system.

        public interface ICircuitBreakPolicy
        {
            void ExecuteRetryWithCircuitBreaker(Action operation);

            TResult ExecuteRetryWithCircuitBreaker<TResult>(Func<TResult> operation);

            Task ExecuteRetryWithCircuitBreaker(Func<Task> operation, CancellationToken cancellationToken);

            Task<TResult> ExecuteRetryWithCircuitBreaker<TResult>(Func<Task<TResult>> operation,
                CancellationToken cancellationToken);
        }

        /// <summary>
        ///     These are the policies for retrying db executions on transient errors from database interactions and for
        ///     pausing all attempts to get a connection when none are available from the database.
        ///     We only retry on the truly transient errors that explicitly suggest we should retry:
        ///     1205	HY000	ER_LOCK_WAIT_TIMEOUT	Lock wait timeout exceeded; try restarting transaction
        ///     1213	40001	ER_LOCK_DEADLOCK	Deadlock found when trying to get lock; try restarting transaction
        ///     1412	HY000	ER_TABLE_DEF_CHANGED	Table definition has changed, please retry transaction
        ///     TODO: Could we do anything useful for error 1927? Research and see if it should just be retried.
        /// </summary>
        public static class DatabaseCommunicationRetryPolicy
        {
            // The retries will take about 10 seconds max.
            // This usually would only occur with an edition copy,
            // which should take about 2 seconds for a very large edition.
            private const int RetryCount = 10;
            private const int WaitBetweenRetriesInMilliseconds = 400;

            private static readonly List<uint> _retrySqlExceptions = new List<uint> { 1205, 1213, 1412 };

            private static readonly AsyncPolicy _retryPolicyAsync = Policy
                .Handle<MySqlException>(
                    exception =>
                        _retrySqlExceptions.Contains(exception.Code)
                )
                .WaitAndRetryAsync(
                    RetryCount,
                    attempt => TimeSpan.FromMilliseconds(WaitBetweenRetriesInMilliseconds),
                    (exception, delay, retryCount, _) =>
                    {
                        Log.ForContext<DbConnectionBase>()
                            .Warning(
                                "Exception encountered, retry {retryCount} in {delay} seconds. {@exception}",
                                retryCount,
                                delay,
                                exception
                            );
                    }
                );

            private static readonly Policy _retryPolicy = Policy
                .Handle<MySqlException>(exception => _retrySqlExceptions.Contains(exception.Code))
                .WaitAndRetry(
                    RetryCount,
                    attempt => TimeSpan.FromMilliseconds(WaitBetweenRetriesInMilliseconds),
                    (exception, delay, retryCount, _) =>
                    {
                        Log.ForContext<DbConnectionBase>()
                            .Warning(
                                "Exception encountered, retry {retryCount} in {delay} seconds. {@exception}",
                                retryCount,
                                delay,
                                exception
                            );
                    }
                );

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

            public static async Task<TResult> ExecuteRetry<TResult>(Func<Task<TResult>> operation,
                CancellationToken cancellationToken)
            {
                return await _retryPolicyAsync.ExecuteAsync(operation.Invoke);
            }
        }

        /// <summary>
        ///     In both sync and async any command run with ExecuteRetry will be retried a maximum of 5 times when the
        ///     exception MariaDb errors 1205, 1213, or 1412 are received (all other exceptions bubble up immediately).
        ///     After max-retries the exception is allowed to bubble up.
        ///     In both sync and async any command run with ExecuteRetryWithCircuitBreaker will be retried a maximum of
        ///     5 times when the exception MariaDb errors 1040 or 1203 are received, after which any attempt to run that
        ///     command will immediately error without even attempting to run it for CircuitBreakerPause seconds.
        ///     TODO: log this activity when the system logger is set up.
        /// </summary>
        public class DatabaseCommunicationCircuitBreakPolicy
        {
            private const int RetryCount = 4;
            private const int WaitBetweenRetriesInMilliseconds = 500;
            private const int CircuitBreakerPause = 5;

            private static readonly List<uint> _pauseExceptions = new List<uint> { 1040, 1203 };
            private readonly Policy _circuitBreakerRetryPolicy;
            private readonly AsyncPolicy _circuitBreakerRetryPolicyAsync;
            private readonly Policy _circuitBreakPolicy;
            private readonly AsyncPolicy _circuitBreakPolicyAsync;

            public DatabaseCommunicationCircuitBreakPolicy()
            {
                _circuitBreakerRetryPolicyAsync = Policy
                    .Handle<MySqlException>(exception => _pauseExceptions.Contains(exception.Code))
                    .WaitAndRetryAsync(
                        RetryCount,
                        attempt => TimeSpan.FromMilliseconds(WaitBetweenRetriesInMilliseconds),
                        (exception, delay, retryCount, _) =>
                        {
                            Log.ForContext<DbConnectionBase>()
                                .Warning(
                                    "Exception encountered, retry {retryCount} in {delay} seconds. {@exception}",
                                    retryCount,
                                    delay,
                                    exception
                                );
                        }
                    );

                _circuitBreakerRetryPolicy = Policy
                    .Handle<MySqlException>(exception => _pauseExceptions.Contains(exception.Code))
                    .WaitAndRetry(
                        RetryCount,
                        attempt => TimeSpan.FromMilliseconds(WaitBetweenRetriesInMilliseconds),
                        (exception, delay, retryCount, _) =>
                        {
                            Log.ForContext<DbConnectionBase>()
                                .Warning(
                                    "Exception encountered, retry {retryCount} in {delay} seconds. {@exception}",
                                    retryCount,
                                    delay,
                                    exception
                                );
                        }
                    );

                _circuitBreakPolicyAsync = Policy
                    .Handle<MySqlException>(exception => _pauseExceptions.Contains(exception.Code))
                    .CircuitBreakerAsync(
                        RetryCount,
                        TimeSpan.FromSeconds(CircuitBreakerPause)
                    );

                _circuitBreakPolicy = Policy
                    .Handle<MySqlException>(exception => _pauseExceptions.Contains(exception.Code))
                    .CircuitBreaker(
                        RetryCount,
                        TimeSpan.FromSeconds(CircuitBreakerPause)
                    );
            }

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
                await _circuitBreakerRetryPolicyAsync.WrapAsync(_circuitBreakPolicyAsync)
                    .ExecuteAsync(operation.Invoke);
            }

            public async Task<TResult> ExecuteRetryWithCircuitBreaker<TResult>(Func<Task<TResult>> operation,
                CancellationToken cancellationToken)
            {
                return await _circuitBreakerRetryPolicyAsync.WrapAsync(_circuitBreakPolicyAsync)
                    .ExecuteAsync(operation.Invoke);
            }
        }
    }
}