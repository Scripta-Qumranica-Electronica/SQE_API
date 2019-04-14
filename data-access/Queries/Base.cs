using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal interface IQueryResponse<T>
    {
        T CreateModel();
    }
}
