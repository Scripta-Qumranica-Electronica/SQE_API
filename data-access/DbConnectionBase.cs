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
                var db = _config.GetConnectionString("MysqlDatabase");
                var host = _config.GetConnectionString("MysqlHost");
                var port = _config.GetConnectionString("MysqlPort");
                var user = _config.GetConnectionString("MysqlUsername");
                var pwd = _config.GetConnectionString("MysqlPassword");
                return $"server={host};port={port};database={db};username={user};password={pwd};";
            }
        }

        protected IDbConnection OpenConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
