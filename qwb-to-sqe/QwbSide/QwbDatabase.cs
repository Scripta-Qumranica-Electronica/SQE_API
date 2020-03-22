using System;
using System.Collections.Generic;
using System.Net;
using MySql.Data.MySqlClient;
using Dapper;

using SshNet;

namespace qwb_to_sqe
{
    public class QwbDatabase
    {
        private MySqlConnection _dbSqlConnection;

        private String _getBookIdQuery = @"SELECT Id 
                                            FROM buch
                                            WHERE Art LIKE 'qb' 
                                               OR (Art LIKE 'q' AND Reihe < 1910 )
                                             ORDER BY Reihe";

        public QwbDatabase()
        {
            try
            {
                _dbSqlConnection = new MySqlConnection(QwbAccesData.dbConnection);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Close()
        {
            _dbSqlConnection.Close();
        }

        public IEnumerable<int> GetBookIds()
        {
            return _dbSqlConnection.Query<int>(_getBookIdQuery);
        }
        
        public 
    }
}
    
    
