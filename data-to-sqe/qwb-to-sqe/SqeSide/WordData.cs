using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class WordData : TextChunkData
    {
        public uint? WordId { get; set; }
        public TextChunkData NonWordTextBefore  { get; set; }
        public TextChunkData NonWordTextAfter  { get; set; }
        public uint NextWordId { get; set; }
        public uint PreviousWordId { get; set; }
    }
}