﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ArtefactDTO
    {
        public int id { get; set; }
        public int scrollVersionId { get; set; }
        public string imageFragmentId {get; set;}
        public string name { get; set; }
        public PolygonDTO mask { get; set; }
        public string transformMatrix { get; set; }
        public string zOrder { get; set; }
        public artSide side { get; set; }

        public enum artSide { recto, verso}
    }
    public class ArtefactListDTO
    {
        public List<ArtefactDTO> ArtefactList { get; set; }
    }
}
