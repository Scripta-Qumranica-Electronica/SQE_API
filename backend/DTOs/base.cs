using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.Backend.DataAccess.Models;

namespace SQE.Backend.Server.DTOs
{
    public class ListResult<T>
    {

        public int Count { get; set; }
        public List<T> Results { get; set; }
        public ListResult(IEnumerable<T> result)
        {
            Results = result.ToList();
            Count = Results.Count;
        }

        
    }

}
