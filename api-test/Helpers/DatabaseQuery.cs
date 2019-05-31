using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;

namespace api_test.Helpers
{
    /// <summary>
    /// In general you should avoid using this in the tests.  It is mainly used to access/check the user_email_token
    /// table, since it is impractical to access emails during integration testing.
    /// </summary>
    public class DatabaseQuery
    {
        const string _connection = "server=localhost;port=3307;database=SQE_DEV;username=root;password=none;charset=utf8;";
        public DatabaseQuery(){}
        
        private static string ConnectionString
        {
            get
            {
                var defaultConnection = _connection;
                
                // Read the environment variables for custom database settings defined at runtime.
                var rootPassword = Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD");
                if (rootPassword != null)
                {
                    defaultConnection = Regex.Replace(defaultConnection,@"(.*password=).*?(;.*)$", @"${1}"+ rootPassword +"${2}");
                }
                
                var db = Environment.GetEnvironmentVariable("MYSQL_DATABASE");
                if (db != null)
                {
                    defaultConnection = Regex.Replace(defaultConnection,@"(.*database=).*?(;.*)$", @"${1}"+ db +"${2}");
                }
                
                var port = Environment.GetEnvironmentVariable("MYSQL_PORT");
                if (port != null)
                {
                    defaultConnection = Regex.Replace(defaultConnection,@"(.*port=).*?(;.*)$", @"${1}"+ port +"${2}");
                }
                
                var host = Environment.GetEnvironmentVariable("MYSQL_HOST");
                if (host != null)
                {
                    defaultConnection = Regex.Replace(defaultConnection,@"(.*server=).*?(;.*)$", @"${1}"+ host +"${2}");
                }
                
                return defaultConnection;
            }
        }

        private static IDbConnection OpenConnection()
        {
            return new MySqlConnection(ConnectionString);
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
    }
}