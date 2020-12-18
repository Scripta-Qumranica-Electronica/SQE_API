using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Polly.CircuitBreaker;
using SQE.DatabaseAccess;
using Xunit;
// ReSharper disable ArrangeRedundantParentheses

namespace SQE.ApiTest
{
	public class DbConnectionBaseTest
	{
		// TODO: probably we should have tests for ReliableMySqlConnection and ReliableMySqlDbCommand
		// They are in use and all other tests pass, but some things, like ReliableMySqlDbCommand.Prepare() or
		// ReliableMySqlConnection.DataSource, never get tested (I don't necessarily know what they all should do).
		// Perhaps something could be wrong there and we would not see it for a very long time.
		private const int RetryCount        = 21;
		private const int CircuitBreakCount = 20;

		private static void ThrowMySqlException(Counter counter, uint sqlErrorCode)
		{
			counter.Count += 1;
			var mySqlExceptionType = typeof(MySqlException).GetTypeInfo();

			var internalConstructor = (from consInfo in mySqlExceptionType.DeclaredConstructors
									   let paramInfos = consInfo.GetParameters()
									   where (paramInfos.Length == 3)
											 && (paramInfos[0].ParameterType == typeof(uint))
											 && (paramInfos[1].ParameterType == typeof(string))
											 && (paramInfos[2].ParameterType == typeof(string))
									   select consInfo).Single();

			var exception =
					internalConstructor.Invoke(
									new object[] { sqlErrorCode, "Nothing done.", "deadlock" }) as
							Exception;

			throw exception;
		}

		private static string ThrowMySqlExceptionWithReturn(Counter counter, uint sqlErrorCode)
		{
			counter.Count += 1;
			var mySqlExceptionType = typeof(MySqlException).GetTypeInfo();

			var internalConstructor = (from consInfo in mySqlExceptionType.DeclaredConstructors
									   let paramInfos = consInfo.GetParameters()
									   where (paramInfos.Length == 3)
											 && (paramInfos[0].ParameterType == typeof(uint))
											 && (paramInfos[1].ParameterType == typeof(string))
											 && (paramInfos[2].ParameterType == typeof(string))
									   select consInfo).Single();

			var exception =
					internalConstructor.Invoke(
									new object[] { sqlErrorCode, "Nothing done.", "deadlock" }) as
							Exception;

			throw exception;
		}

		private static async Task ThrowMySqlExceptionAsync(Counter counter, uint sqlErrorCode)
		{
			await Task.CompletedTask;
			counter.Count += 1;
			var mySqlExceptionType = typeof(MySqlException).GetTypeInfo();

			var internalConstructor = (from consInfo in mySqlExceptionType.DeclaredConstructors
									   let paramInfos = consInfo.GetParameters()
									   where (paramInfos.Length == 3)
											 && (paramInfos[0].ParameterType == typeof(uint))
											 && (paramInfos[1].ParameterType == typeof(string))
											 && (paramInfos[2].ParameterType == typeof(string))
									   select consInfo).Single();

			var exception =
					internalConstructor.Invoke(
									new object[] { sqlErrorCode, "Nothing done.", "deadlock" }) as
							Exception;

			throw exception;
		}

		private static async Task<string> ThrowMySqlExceptionWithReturnAsync(
				Counter counter
				, uint  sqlErrorCode)
		{
			await Task.CompletedTask;
			counter.Count += 1;
			var mySqlExceptionType = typeof(MySqlException).GetTypeInfo();

			var internalConstructor = (from consInfo in mySqlExceptionType.DeclaredConstructors
									   let paramInfos = consInfo.GetParameters()
									   where (paramInfos.Length == 3)
											 && (paramInfos[0].ParameterType == typeof(uint))
											 && (paramInfos[1].ParameterType == typeof(string))
											 && (paramInfos[2].ParameterType == typeof(string))
									   select consInfo).Single();

			var exception =
					internalConstructor.Invoke(
									new object[] { sqlErrorCode, "Nothing done.", "deadlock" }) as
							Exception;

			throw exception;
		}

		[Fact]
		[Trait("Category", "Database Retries")]
		public void RetryPoliciesShouldNotRepeat()
		{
			// Arrange
			var counter = new Counter { Count = 0 };
			const uint code = 1203;
			var watch = Stopwatch.StartNew();

			// Act
			var ex = Assert.Throws<MySqlException>(
					() => DatabaseCommunicationRetryPolicy.ExecuteRetry(
							() => ThrowMySqlException(counter, code)));

			// Assert
			watch.Stop();
			Assert.Equal(code, ex.Code);

			Assert.Equal(1, counter.Count); // This should have tried a total of _retryCount times.
		}

		[Fact]
		[Trait("Category", "Database Retries")]
		public async Task RetryPoliciesShouldRepeat()
		{
			// Arrange
			var counter = new Counter { Count = 0 };
			const uint code = 1205;

			// See _waitTime method line 126 of DbConnectionBase.cs
			const int minExecutionTime = 2450;
			var watch = Stopwatch.StartNew();

			// Act
			var ex = Assert.Throws<MySqlException>(
					() => DatabaseCommunicationRetryPolicy.ExecuteRetry(
							() => ThrowMySqlException(counter, code)));

			// Assert
			watch.Stop();
			Assert.Equal(code, ex.Code);

			Assert.Equal(
					RetryCount
					, counter.Count); // This should have tried a total of _retryCount times.

			Assert.True(watch.ElapsedMilliseconds > minExecutionTime);

			// With return type
			// Arrange
			counter = new Counter { Count = 0 };
			watch = Stopwatch.StartNew();

			// Act
			ex = Assert.Throws<MySqlException>(
					() => DatabaseCommunicationRetryPolicy.ExecuteRetry(
							() => ThrowMySqlExceptionWithReturn(counter, code)));

			// Assert
			watch.Stop();
			Assert.Equal(code, ex.Code);

			Assert.Equal(
					RetryCount
					, counter.Count); // This should have tried a total of _retryCount times.

			Assert.True(watch.ElapsedMilliseconds > minExecutionTime);

			// With Async
			// Arrange
			counter = new Counter { Count = 0 };
			watch = Stopwatch.StartNew();
			var token = new CancellationToken();

			// Act
			ex = await Assert.ThrowsAsync<MySqlException>(
					() => DatabaseCommunicationRetryPolicy.ExecuteRetry(
							() => ThrowMySqlExceptionAsync(counter, code)
							, token));

			// Assert
			watch.Stop();
			Assert.Equal(code, ex.Code);

			Assert.Equal(
					RetryCount
					, counter.Count); // This should have tried a total of _retryCount times.

			Assert.True(watch.ElapsedMilliseconds > minExecutionTime);

			// Async with return type
			// Arrange
			counter = new Counter { Count = 0 };
			watch = Stopwatch.StartNew();

			// Act
			ex = await Assert.ThrowsAsync<MySqlException>(
					() => DatabaseCommunicationRetryPolicy.ExecuteRetry(
							() => ThrowMySqlExceptionWithReturnAsync(counter, code)
							, token));

			// Assert
			watch.Stop();
			Assert.Equal(code, ex.Code);

			Assert.Equal(
					RetryCount
					, counter.Count); // This should have tried a total of _retryCount times.

			Assert.True(watch.ElapsedMilliseconds > minExecutionTime);
		}

		[Fact]
		[Trait("Category", "Database Retries")]
		public async Task ShortCircuitShouldEngage()
		{
			// Arrange
			var counter = new Counter { Count = 0 };
			const uint code = 1040;
			var policy = new DatabaseCommunicationCircuitBreakPolicy();
			const int repeatCount = 7;
			BrokenCircuitException retryEx = null;

			// Act (even though we run this 7 times, the method itself should only run 5 times total)
			for (var i = 0; i < repeatCount; i++)
			{
				retryEx = Assert.Throws<BrokenCircuitException>(
						() => policy.ExecuteRetryWithCircuitBreaker(
								() => ThrowMySqlException(counter, code)));
			}

			// Assert
			Assert.Equal(code, ((MySqlException) retryEx.InnerException).Code);

			Assert.Equal(
					CircuitBreakCount
					, counter
							.Count); // This should have tried a total of x times, before the breaker engaged.

			// With return type
			// Arrange
			policy = new DatabaseCommunicationCircuitBreakPolicy();
			counter.Count = 0;

			// Act (even though we run this 7 times, the method itself should only run 5 times total)
			for (var i = 0; i < repeatCount; i++)
			{
				retryEx = Assert.Throws<BrokenCircuitException>(
						() => policy.ExecuteRetryWithCircuitBreaker(
								() => ThrowMySqlExceptionWithReturn(counter, code)));
			}

			// Assert
			Assert.Equal(code, ((MySqlException) retryEx.InnerException).Code);

			Assert.Equal(
					CircuitBreakCount
					, counter
							.Count); // This should have tried a total of x times, before the breaker engaged.

			// Async
			// Arrange
			counter.Count = 0;
			policy = new DatabaseCommunicationCircuitBreakPolicy();
			var token = new CancellationToken();

			// Act (even though we run this 7 times, the method itself should only run 5 times total)
			for (var i = 0; i < repeatCount; i++)
			{
				retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(
						() => policy.ExecuteRetryWithCircuitBreaker(
								() => ThrowMySqlExceptionAsync(counter, code)
								, token));
			}

			// Assert
			Assert.Equal(code, ((MySqlException) retryEx.InnerException).Code);

			Assert.Equal(
					CircuitBreakCount
					, counter
							.Count); // This should have tried a total of x times, before the breaker engaged.

			// Async with return
			// Arrange
			policy = new DatabaseCommunicationCircuitBreakPolicy();
			counter.Count = 0;

			// Act (even though we run this 7 times, the method itself should only run 5 times total)
			for (var i = 0; i < repeatCount; i++)
			{
				retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(
						() => policy.ExecuteRetryWithCircuitBreaker(
								() => ThrowMySqlExceptionWithReturnAsync(counter, code)
								, token));
			}

			// Assert
			Assert.Equal(code, ((MySqlException) retryEx.InnerException).Code);

			Assert.Equal(
					CircuitBreakCount
					, counter
							.Count); // This should have tried a total of x times, before the breaker engaged.
		}

		[Fact]
		[Trait("Category", "Database Retries")]
		public void ShortCircuitShouldNotEngage()
		{
			// Arrange
			var counter = new Counter { Count = 0 };
			const uint code = 1044;
			var policy = new DatabaseCommunicationCircuitBreakPolicy();
			const int repeatCount = 7;
			MySqlException ex = null;

			// Act (we run this 7 times and the circuit breaker should not engage)
			for (var i = 0; i < repeatCount; i++)
			{
				ex = Assert.Throws<MySqlException>(
						() => policy.ExecuteRetryWithCircuitBreaker(
								() => ThrowMySqlException(counter, code)));
			}

			// Assert
			Assert.NotNull(ex);
			Assert.Equal(code, ex.Code);
			Assert.Equal(repeatCount, counter.Count);
		}

		private class Counter
		{
			public int Count { get; set; }
		}
	}
}
