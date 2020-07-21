using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class QwbWord
    {
        public uint? QwbWordId { get; set; }
        public List<WordData> WordIds = new List<WordData>();
    }
}