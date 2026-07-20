using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using SQE.API.Server;
using SQE.ApiTest.Helpers;
using SQE.DatabaseAccess.Helpers;
using Xunit;

namespace SQE.ApiTest;

/// <summary>
///  This collection is deliberately marked non-parallel. The tests here change a *global* server
///  setting (innodb_lock_wait_timeout) and hold row locks, so they must never run at the same time
///  as any other test collection - otherwise they would leak lock-wait-timeouts into unrelated
///  tests. (The whole assembly is also configured non-parallel in xunit.runner.json; this attribute
///  keeps the guarantee even if that is ever re-enabled.)
/// </summary>
[CollectionDefinition("DB lock contention", DisableParallelization = true)]
public class DbLockContentionCollection { }

/// <summary>
///  Verifies the transient-error retry behaviour of <see cref="DatabaseAccessorRetryDecorator" />
///  against a *real* MariaDB lock-wait-timeout (error 1205), rather than a fabricated exception:
///  <list type="bullet">
///    <item>outside a transaction, a statement that hits a transient error is retried until it
///          succeeds (the retry replays the work);</item>
///    <item>inside a transaction, the retry is suppressed so the failure surfaces immediately -
///          the retry belongs at the transaction boundary, not on individual statements.</item>
///  </list>
///  Mechanism: a second connection holds an exclusive lock on a row; the accessor then writes to
///  the same row. With innodb_lock_wait_timeout lowered to a couple of seconds, the accessor's
///  statement errors with 1205 - the transient error the decorator retries on.
/// </summary>
[Collection("DB lock contention")]
public class TransactionRetryTests : IClassFixture<WebApplicationFactory<Startup>>
{
	private const int    LockWaitTimeoutSeconds = 2;
	private readonly DatabaseQuery                  _db = new();
	private readonly WebApplicationFactory<Startup> _factory;

	public TransactionRetryTests(WebApplicationFactory<Startup> factory)
	{
		var projectDir = Directory.GetCurrentDirectory();
		var configPath = Path.Combine(projectDir, "../../../../sqe-api-server/appsettings.json");

		_factory = factory.WithWebHostBuilder(builder =>
											  {
												  builder.UseEnvironment("IntegrationTests");

												  builder.ConfigureAppConfiguration((context, conf)
																						 => conf.AddJsonFile(
																								 configPath));
											  });
	}

	private IDatabaseAccessor GetAccessor(IServiceScope scope)
		=> scope.ServiceProvider.GetRequiredService<IDatabaseAccessor>();

	/// <summary>
	///  Outside a transaction, a transient lock-wait-timeout must be retried until the lock clears,
	///  and the write must ultimately succeed. Because the lock is held longer than a single
	///  timeout, success is only possible if the accessor retried across several timeouts.
	/// </summary>
	[Fact]
	[Trait("Category", "Transaction Retry")]
	public async Task TransientErrorOutsideTransactionIsRetriedUntilItSucceeds()
	{
		const int lockHoldMs = 5000; // held well beyond a single LockWaitTimeoutSeconds window

		var editionId = await _db.RunQuerySingleAsync<uint>(
				"SELECT MIN(edition_id) FROM edition"
				, new DynamicParameters());

		await using var admin = await _db.OpenNewConnectionAsync();
		var originalTimeout = await admin.QuerySingleAsync<int>(
				"SELECT @@GLOBAL.innodb_lock_wait_timeout");

		await admin.ExecuteAsync(
				$"SET GLOBAL innodb_lock_wait_timeout = {LockWaitTimeoutSeconds}");

		try
		{
			// Sanity: confirm the lowered timeout actually reaches the accessor's (pooled)
			// connections - otherwise the statement would merely block instead of erroring, and
			// this test would prove nothing.
			using (var probeScope = _factory.Services.CreateScope())
			{
				var effective = await GetAccessor(probeScope).QueryFirstAsync<int>(
						"SELECT @@SESSION.innodb_lock_wait_timeout");

				Assert.Equal(LockWaitTimeoutSeconds, effective);
			}

			// Hold an exclusive lock on the target row on a separate connection.
			await using var locker = await _db.OpenNewConnectionAsync();
			var lockTx = await locker.BeginTransactionAsync();

			await locker.ExecuteAsync(
					"SELECT * FROM edition WHERE edition_id = @editionId FOR UPDATE"
					, new { editionId }
					, lockTx);

			// Release the lock after lockHoldMs, on a background task.
			var releaseLock = Task.Run(async () =>
									   {
										   await Task.Delay(lockHoldMs);
										   await lockTx.RollbackAsync();
									   });

			using var scope = _factory.Services.CreateScope();
			var dba = GetAccessor(scope);

			var stopwatch = Stopwatch.StartNew();

			// No transaction -> the retry policy is active. Each attempt blocks for
			// LockWaitTimeoutSeconds then errors 1205; the only way this returns instead of
			// throwing is by retrying past those timeouts until the lock is released.
			await dba.ExecuteAsync(
					"UPDATE edition SET manuscript_id = manuscript_id WHERE edition_id = @editionId"
					, new { editionId });

			stopwatch.Stop();
			await releaseLock;

			// It outlived at least one full timeout window => it must have retried.
			Assert.True(
					stopwatch.ElapsedMilliseconds > LockWaitTimeoutSeconds * 1000
					, $"Expected the write to survive at least one {LockWaitTimeoutSeconds}s "
					+ $"lock-wait-timeout by retrying, but it completed in "
					+ $"{stopwatch.ElapsedMilliseconds}ms.");
		}
		finally
		{
			await admin.ExecuteAsync(
					$"SET GLOBAL innodb_lock_wait_timeout = {originalTimeout}");
		}
	}

	/// <summary>
	///  Inside a transaction, the retry must be suppressed: a transient failure has to surface to
	///  the caller (which owns the transaction boundary) rather than being retried statement-by-
	///  statement against an already-failed transaction. So the write throws at the first
	///  lock-wait-timeout instead of waiting out the (longer) lock hold.
	/// </summary>
	[Fact]
	[Trait("Category", "Transaction Retry")]
	public async Task TransientErrorInsideTransactionIsNotRetried()
	{
		const int lockHoldMs = 6000; // longer than the timeout; a retrying impl would outlast it

		var editionId = await _db.RunQuerySingleAsync<uint>(
				"SELECT MIN(edition_id) FROM edition"
				, new DynamicParameters());

		await using var admin = await _db.OpenNewConnectionAsync();
		var originalTimeout = await admin.QuerySingleAsync<int>(
				"SELECT @@GLOBAL.innodb_lock_wait_timeout");

		await admin.ExecuteAsync(
				$"SET GLOBAL innodb_lock_wait_timeout = {LockWaitTimeoutSeconds}");

		try
		{
			await using var locker = await _db.OpenNewConnectionAsync();
			var lockTx = await locker.BeginTransactionAsync();

			await locker.ExecuteAsync(
					"SELECT * FROM edition WHERE edition_id = @editionId FOR UPDATE"
					, new { editionId }
					, lockTx);

			var releaseLock = Task.Run(async () =>
									   {
										   await Task.Delay(lockHoldMs);
										   await lockTx.RollbackAsync();
									   });

			using var scope = _factory.Services.CreateScope();
			var dba = GetAccessor(scope);

			await dba.BeginTransactionAsync();
			var stopwatch = Stopwatch.StartNew();

			// In a transaction the retry policy is bypassed, so the first 1205 propagates.
			var exception = await Assert.ThrowsAnyAsync<MySqlException>(() => dba.ExecuteAsync(
					"UPDATE edition SET manuscript_id = manuscript_id WHERE edition_id = @editionId"
					, new { editionId }));

			stopwatch.Stop();
			dba.RollbackTransaction();
			await releaseLock;

			// It was really the transient error we expect...
			Assert.Equal(MySqlErrorCode.LockWaitTimeout, exception.ErrorCode);

			// ...and it failed fast (before the lock released), i.e. it did not retry.
			Assert.True(
					stopwatch.ElapsedMilliseconds < lockHoldMs
					, $"Expected an immediate failure without retry (~{LockWaitTimeoutSeconds}s), "
					+ $"but it took {stopwatch.ElapsedMilliseconds}ms.");
		}
		finally
		{
			await admin.ExecuteAsync(
					$"SET GLOBAL innodb_lock_wait_timeout = {originalTimeout}");
		}
	}
}
