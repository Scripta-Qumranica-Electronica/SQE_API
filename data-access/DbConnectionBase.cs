using System;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.RegularExpressions;


namespace SQE.SqeHttpApi.DataAccess
{
    public class DbConnectionBase
    {
        private readonly IConfiguration _config;

        protected DbConnectionBase(IConfiguration config)
        {
            _config = config;
        }

        private string ConnectionString
        {
            get
            {
                var defaultConnection = _config.GetConnectionString("DefaultConnection");
                
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

        protected IDbConnection OpenConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
