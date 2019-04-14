using System;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;




namespace SQE.SqeHttpApi.DataAccess
{
    public class DBConnectionBase
    {
        protected IConfiguration _config;

        public DBConnectionBase(IConfiguration config)
        {
            _config = config;
        }

        protected string ConnectionString
        {
            get
            {
                return _config.GetConnectionString("DefaultConnection");
            }
        }

        protected IDbConnection OpenConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
