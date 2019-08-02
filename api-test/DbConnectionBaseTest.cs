using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Polly.CircuitBreaker;
using SQE.SqeApi.DataAccess;
using Xunit;

namespace SQE.ApiTest
{
    public class DbConnectionBaseTest
    {
        // TODO: probably we should have tests for ReliableMySqlConnection and ReliableMySqlDbCommand
        // They are in use and all other tests pass, but some things, like ReliableMySqlDbCommand.Prepare() or 
        // ReliableMySqlConnection.DataSource, never get tested (I don't necessarily know what they all should do).
        // Perhaps something could be wrong there and we would not see it for a very long time.
        
        [Fact]
        public async Task RetryPoliciesShouldRepeat()
        {
            // Arrange
            var counter = new Counter() { Count = 0 };
            const uint code = 1205;
            const int minExecutionTime = 2500; // Currently 200 factorial 5 - minimum random offset (100 * 5)
            
            var watch = System.Diagnostics.Stopwatch.StartNew();
            
            // Act
            var ex = Assert.Throws<MySqlException>(() => DatabaseCommunicationRetryPolicy.ExecuteRetry(() => ThrowMySqlException(counter, code)));
            
            // Assert
            watch.Stop();
            Assert.Equal(code, ex.Code);
            Assert.Equal(6, counter.Count); // This should have tried a total of 6 times.
            Assert.True(watch.ElapsedMilliseconds > minExecutionTime);
            
            // With return type
            // Arrange
            counter = new Counter() { Count = 0 };
            watch = System.Diagnostics.Stopwatch.StartNew();
            
            // Act
            ex = Assert.Throws<MySqlException>(() => DatabaseCommunicationRetryPolicy.ExecuteRetry(() => ThrowMySqlExceptionWithReturn(counter, code)));
            
            // Assert
            watch.Stop();
            Assert.Equal(code, ex.Code);
            Assert.Equal(6, counter.Count); // This should have tried a total of 6 times.
            Assert.True(watch.ElapsedMilliseconds > minExecutionTime);
            
            // With Async
            // Arrange
            counter = new Counter() { Count = 0 };
            watch = System.Diagnostics.Stopwatch.StartNew();
            var token = new CancellationToken();
            
            // Act
            ex = await Assert.ThrowsAsync<MySqlException>(() => DatabaseCommunicationRetryPolicy.ExecuteRetry(() => ThrowMySqlExceptionAsync(counter, code), token));
            
            // Assert
            watch.Stop();
            Assert.Equal(code, ex.Code);
            Assert.Equal(6, counter.Count); // This should have tried a total of 6 times.
            Assert.True(watch.ElapsedMilliseconds > minExecutionTime);
            
            // Async with return type
            // Arrange
            counter = new Counter() { Count = 0 };
            watch = System.Diagnostics.Stopwatch.StartNew();
            
            // Act
            ex = await Assert.ThrowsAsync<MySqlException>(() => DatabaseCommunicationRetryPolicy.ExecuteRetry(() => ThrowMySqlExceptionWithReturnAsync(counter, code), token));
            
            // Assert
            watch.Stop();
            Assert.Equal(code, ex.Code);
            Assert.Equal(6, counter.Count); // This should have tried a total of 6 times.
            Assert.True(watch.ElapsedMilliseconds > minExecutionTime);
        }
        
        [Fact]
        public async Task RetryPoliciesShouldNotRepeat()
        {
            // Arrange
            var counter = new Counter() { Count = 0 };
            const uint code = 1203;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            
            // Act
            var ex = Assert.Throws<MySqlException>(() => DatabaseCommunicationRetryPolicy.ExecuteRetry(() => ThrowMySqlException(counter, code)));
            
            // Assert
            watch.Stop();
            Assert.Equal(code, ex.Code);
            Assert.Equal(1, counter.Count); // This should have tried a total of 6 times.
        }
        
        [Fact]
        public async Task ShortCircuitShouldEngage()
        {
            // Arrange
            var counter = new Counter() { Count = 0 };
            const uint code = 1040;
            var policy = new DatabaseCommunicationCircuitBreakPolicy();
            
            // Act (even though we run this 7 times, the method itself should only run 5 times total)
            var retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            
            // Assert
            Assert.Equal(code, ((MySqlException) retryEx.InnerException).Code);
            Assert.Equal(5, counter.Count); // This should have tried a total of 5 times, before the breaker engaged.
            
            // With return type
            // Arrange
            policy = new DatabaseCommunicationCircuitBreakPolicy();
            counter.Count = 0;
            
            // Act (even though we run this 7 times, the method itself should only run 5 times total)
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturn(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturn(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturn(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturn(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturn(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturn(counter, code)));
            retryEx = Assert.Throws<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturn(counter, code)));
            
            // Assert
            Assert.Equal(code, ((MySqlException) retryEx.InnerException).Code);
            Assert.Equal(5, counter.Count); // This should have tried a total of 5 times, before the breaker engaged.
            
            // Async
            // Arrange
            counter.Count = 0;
            policy = new DatabaseCommunicationCircuitBreakPolicy();
            var token = new CancellationToken();
            
            // Act (even though we run this 7 times, the method itself should only run 5 times total)
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionAsync(counter, code), token));
            
            // Assert
            Assert.Equal(code, ((MySqlException) retryEx.InnerException).Code);
            Assert.Equal(5, counter.Count); // This should have tried a total of 5 times, before the breaker engaged.
            
            // Async with return
            // Arrange
            policy = new DatabaseCommunicationCircuitBreakPolicy();
            counter.Count = 0;
            
            // Act (even though we run this 7 times, the method itself should only run 5 times total)
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturnAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturnAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturnAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturnAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturnAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturnAsync(counter, code), token));
            retryEx = await Assert.ThrowsAsync<BrokenCircuitException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlExceptionWithReturnAsync(counter, code), token));
            
            // Assert
            Assert.Equal(code, ((MySqlException) retryEx.InnerException).Code);
            Assert.Equal(5, counter.Count); // This should have tried a total of 5 times, before the breaker engaged.
        }
        
        [Fact]
        public async Task ShortCircuitShouldNotEngage()
        {
            // Arrange
            var counter = new Counter() { Count = 0 };
            const uint code = 1044;
            var policy = new DatabaseCommunicationCircuitBreakPolicy();
            
            // Act (we run this 7 times and the circuit breaker should not engage)
            var ex = Assert.Throws<MySqlException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            ex = Assert.Throws<MySqlException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            ex = Assert.Throws<MySqlException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            ex = Assert.Throws<MySqlException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            ex = Assert.Throws<MySqlException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            ex = Assert.Throws<MySqlException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            ex = Assert.Throws<MySqlException>(() => policy.ExecuteRetryWithCircuitBreaker(() => ThrowMySqlException(counter, code)));
            
            // Assert
            Assert.Equal(code, ex.Code);
            Assert.Equal(7, counter.Count); // This should have tried a total of 5 times, before the breaker engaged.
        }

        private static void ThrowMySqlException(Counter counter, uint sqlErrorCode)
        {
            counter.Count += 1;
            var mySqlExceptionType = typeof(MySqlException).GetTypeInfo();
            var internalConstructor = (from consInfo in mySqlExceptionType.DeclaredConstructors
                let paramInfos = consInfo.GetParameters()
                where paramInfos.Length == 3 && 
                      paramInfos[0].ParameterType == typeof(uint) && 
                      paramInfos[1].ParameterType == typeof(string) && 
                      paramInfos[2].ParameterType == typeof(string)
                select consInfo).Single();
 
            var exception = internalConstructor.Invoke(new object[] { sqlErrorCode, "Nothing done.", "deadlock" }) as Exception;
            throw exception;
        }
        
        private static string ThrowMySqlExceptionWithReturn(Counter counter, uint sqlErrorCode)
        {
            counter.Count += 1;
            var mySqlExceptionType = typeof(MySqlException).GetTypeInfo();
            var internalConstructor = (from consInfo in mySqlExceptionType.DeclaredConstructors
                let paramInfos = consInfo.GetParameters()
                where paramInfos.Length == 3 && 
                      paramInfos[0].ParameterType == typeof(uint) && 
                      paramInfos[1].ParameterType == typeof(string) && 
                      paramInfos[2].ParameterType == typeof(string)
                select consInfo).Single();
 
            var exception = internalConstructor.Invoke(new object[] { sqlErrorCode, "Nothing done.", "deadlock" }) as Exception;
            throw exception;
        }
        
        private static async Task ThrowMySqlExceptionAsync(Counter counter, uint sqlErrorCode)
        {
            counter.Count += 1;
            var mySqlExceptionType = typeof(MySqlException).GetTypeInfo();
            var internalConstructor = (from consInfo in mySqlExceptionType.DeclaredConstructors
                let paramInfos = consInfo.GetParameters()
                where paramInfos.Length == 3 && 
                      paramInfos[0].ParameterType == typeof(uint) && 
                      paramInfos[1].ParameterType == typeof(string) && 
                      paramInfos[2].ParameterType == typeof(string)
                select consInfo).Single();
 
            var exception = internalConstructor.Invoke(new object[] { sqlErrorCode, "Nothing done.", "deadlock" }) as Exception;
            throw exception;
        }
        
        private static async Task<string> ThrowMySqlExceptionWithReturnAsync(Counter counter, uint sqlErrorCode)
        {
            counter.Count += 1;
            var mySqlExceptionType = typeof(MySqlException).GetTypeInfo();
            var internalConstructor = (from consInfo in mySqlExceptionType.DeclaredConstructors
                let paramInfos = consInfo.GetParameters()
                where paramInfos.Length == 3 && 
                      paramInfos[0].ParameterType == typeof(uint) && 
                      paramInfos[1].ParameterType == typeof(string) && 
                      paramInfos[2].ParameterType == typeof(string)
                select consInfo).Single();
 
            var exception = internalConstructor.Invoke(new object[] { sqlErrorCode, "Nothing done.", "deadlock" }) as Exception;
            throw exception;
        }

        private class Counter
        {
            public int Count { get; set; }
        }
    }
}