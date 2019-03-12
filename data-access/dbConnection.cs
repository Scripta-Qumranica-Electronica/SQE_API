using System;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;




namespace data_access
{
    public class dbConnection
    {
        protected IConfiguration _config;

        public dbConnection(IConfiguration config)
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
