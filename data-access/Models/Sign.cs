using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class Sign
    {
        public uint signId { get; set; }
        public uint nextSignId { get; set; }
        public readonly List<SignChar> signChars = new List<SignChar>();
    }
}