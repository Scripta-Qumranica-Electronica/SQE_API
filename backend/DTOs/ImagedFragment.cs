using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ImagedFragment
    {
        public string id { get; set; }
        public ImageStack recto { get; set; } // TODO: Change to ImageStack
        public ImageStack verso { get; set; } // TODO: Change to ImageStack
        public List<Artefact> Artefacts { get; set; }

    }
    public class ImagedFragmentList
    {
        public List<ImagedFragment> ImagedFragments { get; set; }
    };
}
