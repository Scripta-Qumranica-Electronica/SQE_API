using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;


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
                return $"server={host};port={port};database={db};username={user};password={pwd};charset=utf8;";
            }
        }

        protected IDbConnection OpenConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
