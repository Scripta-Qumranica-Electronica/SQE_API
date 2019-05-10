using System.Collections.Generic;
using System.Linq;

namespace SQE.SqeHttpApi.Server.DTOs
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
