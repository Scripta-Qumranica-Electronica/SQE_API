using System;
using System.Collections.Generic;
using System.Text;
using data_access.Models;
using System.Linq;

namespace data_access.RawModels
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
