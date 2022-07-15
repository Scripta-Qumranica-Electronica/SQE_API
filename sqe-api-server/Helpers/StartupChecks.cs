using System;
using System.Linq;
using Dapper;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;
using Polly;
using Serilog;
using SQE.DatabaseAccess;

namespace SQE.API.Server.Helpers
{
	public static class StartupChecks
	{
		public static void RunAllChecks(IConfiguration configuration, IWebHostEnvironment env)
		{
			// Always test the database connection
			DatabaseConnector(configuration);

			// Only test the emailer in production
			if (env.IsProduction())
				Emailer(configuration);

			// TO-DO add check for github credentials
		}

		private static void Emailer(IConfiguration configuration)
		{
			// Check for each individual email connection string and provide a helpful diagnostic
			// response when it is absent.
			const string err =
					"You must enter a setting for $EmailSetting in appsettings.json when running in production mode.";

			CheckConfig(
					configuration
					, "MailerEmailAddress"
					, err
					, "$EmailSetting");

			CheckConfig(
					configuration
					, "MailerEmailUsername"
					, err
					, "$EmailSetting");

			CheckConfig(
					configuration
					, "MailerEmailPassword"
					, err
					, "$EmailSetting");

			CheckConfig(
					configuration
					, "MailerEmailSmtpUrl"
					, err
					, "$EmailSetting");

			CheckConfig(
					configuration
					, "MailerEmailSmtpPort"
					, err
					, "$EmailSetting");

			CheckConfig(
					configuration
					, "MailerEmailSmtpSecurity"
					, err
					, "$EmailSetting");

			CheckConfig(
					configuration
					, "WebsiteHost"
					, err
					, "$EmailSetting");

			// Test the email smtp connection
			var user = configuration.GetConnectionString("MailerEmailUsername");
			var pwd = configuration.GetConnectionString("MailerEmailPassword");
			var smtp = configuration.GetConnectionString("MailerEmailSmtpUrl");
			var port = configuration.GetConnectionString("MailerEmailSmtpPort");

			var security = configuration.GetConnectionString("MailerEmailSmtpSecurity");

			var securityEnum = (SecureSocketOptions) Enum.Parse(
					typeof(SecureSocketOptions)
					, security);

			using (var client = new SmtpClient())
			{
				client.Connect(
						smtp
						, int.TryParse(port, out var intValue)
								? intValue
								: 0
						, securityEnum);

				client.Authenticate(user, pwd);
				var dispose = client.DisconnectAsync(true);
				dispose.Wait();
			}
		}

		private static void DatabaseConnector(IConfiguration configuration)
		{
			// Check for each individual database connection string and provide a helpful diagnostic
			// response when it is absent.
			const string err = "You must enter a setting for $DatabaseSetting in appsettings.json.";

			CheckConfig(
					configuration
					, "MysqlHost"
					, err
					, "$DatabaseSetting");

			CheckConfig(
					configuration
					, "MysqlPort"
					, err
					, "$DatabaseSetting");

			CheckConfig(
					configuration
					, "MysqlDatabase"
					, err
					, "$DatabaseSetting");

			CheckConfig(
					configuration
					, "MysqlUsername"
					, err
					, "$DatabaseSetting");

			CheckConfig(
					configuration
					, "MysqlPassword"
					, err
					, "$DatabaseSetting");

			// Connect to the database and run a quick test query
			// Retry 5 times if the database is not yet up (3 second pause between retries)
			var policy = Policy.Handle<MySqlException>() // Only retry on Mysql Exceptions
							   .WaitAndRetry(
									   10
									   , attempt => TimeSpan.FromSeconds(3)
									   , // wait a total of 10 * 3 seconds
									   (
											   exception
											   , delay
											   , retryCount
											   , _) =>
									   {
										   Log.ForContext<Startup>()
											  .Warning(
													  "Exception encountered, retry {retryCount} in {delay} seconds. {@exception}"
													  , retryCount
													  , delay
													  , exception);
									   });

			policy.Execute(() => new DatabaseVerificationInstance(configuration).Verify());
		}

		private static void CheckConfig(
				IConfiguration configuration
				, string       stringName
				, string       err
				, string       errReplaceToken = null)
		{
			const string dockerMsg =
					" If you are running the API from a docker container, this value should be set by an environment variable.";

			err += dockerMsg;

			// Check if the string is set in the configuration file
			if (!string.IsNullOrEmpty(configuration.GetConnectionString(stringName)))
				return;

			// If it isn't set, and we have no errReplaceToken for a more detailed error, throw a simple error message
			if (string.IsNullOrEmpty(errReplaceToken))
				throw new SystemException(err);

			// Otherwise throw a more detailed error message
			throw new SystemException(err.Replace(errReplaceToken, stringName));
		}

		private class DatabaseVerificationInstance : DbConnectionBase
		{
			private readonly IConfiguration _config;

			internal DatabaseVerificationInstance(IConfiguration configuration) : base(
					configuration) => _config = configuration;

			public void Verify()
			{
				var dbName = _config.GetConnectionString("MysqlDatabase");

				const string sql =
						"SELECT table_name FROM information_schema.tables where table_schema=@DbName;";

				using (var connection = OpenConnection())
				{
					var tableNames = connection.Query<string>(sql, new { DbName = dbName });

					if (!tableNames.Any())
					{
						throw new SystemException(
								$"A database named {dbName} exists, but it is empty.");
					}
				}
			}
		}
	}
}
