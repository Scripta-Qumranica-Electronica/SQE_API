using System;
using System.Collections.Generic;
using System.Text;
using SQE.Backend.DataAccess.Models;
using System.Linq;

namespace SQE.Backend.DataAccess.RawModels
{ 
     internal class ArtefactResponse : IQueryResponse<Artefact>
    {
        public string id { get; set; }
        public string scroll_version_id { get; set; }
        public string imageFragmentId { get; set; }
        public string name { get; set; }
        public Polygon mask { get; set; }
        public string transform_matrix { get; set; }
        public int? zOrder { get; set; }
        public string side { get; set; }

        public Artefact CreateModel()
        {
            var artefact = new Artefact
            { 
                Id = id,
                Name = name,
                //scrollVersionId = scroll_version_id,
                //imagedFragmentId = imageFragmentId
            };
        return artefact;
        }

    }
    public class Polygon
    {
        public string represent { get; set; }
    }

    
}
