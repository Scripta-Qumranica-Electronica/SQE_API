using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace SQE.ApiTest.Helpers
{
    /// <summary>
    /// In general you should avoid using this in the tests.  It is mainly used to access/check the user_email_token
    /// table, since it is impractical to access emails during integration testing.
    /// </summary>
    public class DatabaseQuery
    {
        private readonly string _connection;
        public DatabaseQuery()
        {
            // TODO: Find a better way to get these settings.
            string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
            using (StreamReader r = new StreamReader(projectDirectory + "/../../sqe-http-api/appsettings.json"))
            {
                var json = r.ReadToEnd();
                dynamic settings = JsonConvert.DeserializeObject(json);
                var db = settings.ConnectionStrings.MysqlDatabase;
                var host = settings.ConnectionStrings.MysqlHost;
                var port = settings.ConnectionStrings.MysqlPort;
                var user = settings.ConnectionStrings.MysqlUsername;
                var pwd = settings.ConnectionStrings.MysqlPassword;
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
    }
}