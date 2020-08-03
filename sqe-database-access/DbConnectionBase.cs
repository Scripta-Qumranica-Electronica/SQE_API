using System.Data;

namespace SQE.DatabaseAccess
{
    public class DbConnectionBase
    {
        private readonly IDbConnection _conn;

        protected DbConnectionBase(IDbConnection conn)
        {
            _conn = conn;
        }

        protected IDbConnection OpenConnection() => _conn;
    }

}