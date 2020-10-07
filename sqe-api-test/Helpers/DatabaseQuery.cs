using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;

namespace SQE.ApiTest.Helpers
{
    /// <summary>
    ///     In general you should avoid using this in the tests.  It is mainly used to access/check the user_email_token
    ///     table, since it is impractical to access emails during integration testing.
    /// </summary>
    public class DatabaseQuery
    {
        private readonly string _connection;

        public DatabaseQuery()
        {
            // TODO: Find a better way to get these settings.
            var projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
            using (var r = new StreamReader(projectDirectory + "/../../sqe-api-server/appsettings.json"))
            {
                var json = r.ReadToEnd();
                //dynamic settings = JsonSerializer.Deserialize<object>(json);
                var connectionStrings = JsonDocument.Parse(json).RootElement.GetProperty("ConnectionStrings");
                var db = connectionStrings.GetProperty("MysqlDatabase").GetString();
                var host = connectionStrings.GetProperty("MysqlHost").GetString();
                var port = connectionStrings.GetProperty("MysqlPort").GetString();
                var user = connectionStrings.GetProperty("MysqlUsername").GetString();
                var pwd = connectionStrings.GetProperty("MysqlPassword").GetString();
                _connection = $"server={host};port={port};database={db};username={user};password={pwd};charset=utf8;";
            }
        }

        private IDbConnection OpenConnection()
        {
            return new MySqlConnection(_connection);
        }

        public async Task<IEnumerable<T>> RunQueryAsync<T>(string sql, DynamicParameters parameters)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<T>(sql, parameters);
            }
        }

        public async Task<T> RunQuerySingleAsync<T>(string sql, DynamicParameters parameters)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QuerySingleAsync<T>(sql, parameters);
            }
        }

        public async Task<int> RunExecuteAsync(string sql, DynamicParameters parameters)
        {
            using (var connection = OpenConnection())
            {
                return await connection.ExecuteAsync(sql, parameters);
            }
        }

        private class DatabaseSettings
        {
        }
    }
}