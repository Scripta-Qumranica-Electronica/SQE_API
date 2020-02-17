using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class Sign
    {
        public readonly List<SignInterpretation> signInterpretations = new List<SignInterpretation>();
        public uint signId { get; set; }
    }

    public class LetterShape
    {
        public uint Id { get; set; }
        public char Letter { get; set; }
        public byte[] Polygon { get; set; }
        public uint TranslateX { get; set; }
        public uint TranslateY { get; set; }
        public ushort LetterRotation { get; set; }
        public string ImageURL { get; set; }
        public string ImageSuffix { get; set; }
        public float ImageRotation { get; set; }
    }
}