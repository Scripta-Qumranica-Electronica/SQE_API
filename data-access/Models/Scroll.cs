using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class Scroll
    {
        public uint scrollId { get; set; }
        public string scroll { get; set; }
        public readonly List<Fragment> fragments = new List<Fragment>();
    }
}