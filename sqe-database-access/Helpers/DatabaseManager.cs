using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace SQE.DatabaseAccess.Helpers;

public interface IDatabaseManager
{
	DbConnection       GetConnection();
	Task<DbConnection> GetConnectionAsync();
}

public class DatabaseManager : IDatabaseManager
{
	private readonly string _connectionString;

	public DatabaseManager(IConfiguration config)
	{
		var db = config.GetConnectionString("MysqlDatabase");
		var host = config.GetConnectionString("MysqlHost");
		var port = config.GetConnectionString("MysqlPort");
		var user = config.GetConnectionString("MysqlUsername");
		var pwd = config.GetConnectionString("MysqlPassword");
		var minConn = config.GetConnectionString("MysqlMinConnectionPoolSize") ?? "8";
		var maxConn = config.GetConnectionString("MysqlMaxConnectionPoolSize") ?? "16";

		_connectionString = $"server={
			host
		};port={
			port
		};database={
			db
		};username={
			user
		};password={
			pwd
		};charset=utf8mb4;AllowUserVariables=True;Pooling=true;MinPoolSize={
			minConn
		};MaxPoolSize={
			maxConn
		};DefaultCommandTimeout=120;ConnectionReset=true;";
	}

	public DbConnection GetConnection() => new MySqlConnection(_connectionString);

	public async Task<DbConnection> GetConnectionAsync()
		=> await Task.Run(() => new MySqlConnection(_connectionString));
}
