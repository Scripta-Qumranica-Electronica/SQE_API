using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class SignChar
    {
        public uint signCharId { get; set; }
        public string signChar { get; set; }
        public readonly List<CharAttribute> attributes = new List<CharAttribute>();
    }
}