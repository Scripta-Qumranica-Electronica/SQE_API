using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.Backend.DataAccess.Queries
{
    internal interface IQueryResponse<T>
    {
        T CreateModel();
    }
}
