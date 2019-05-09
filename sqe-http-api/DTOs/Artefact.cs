using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class ArtefactDTO
    {
        public uint id { get; set; }
        public uint editionId { get; set; }
        public string imagedObjectId {get; set;}
        public string name { get; set; }
        public PolygonDTO mask { get; set; }
        public uint zOrder { get; set; }
        public ArtefactSide side { get; set; }

        public enum ArtefactSide { recto, verso}
    }

    public static class ArtefactDTOTransform
    {
        public static ArtefactDTO QueryArtefactToArtefactDTO(ArtefactsOfEditionQuery.Result artefact, uint editionId)
        {
            return new ArtefactDTO()
            {
                id = artefact.artefact_id,
                editionId = editionId,
                mask = new PolygonDTO()
                {
                    mask = artefact.mask,
                    transformMatrix = ""
                },

                imagedObjectId = ImagedObjectIdFormat.Serialize(artefact.institution, artefact.catalog_number_1, artefact.catalog_number_2),

                name = artefact.name,
                side = artefact.catalog_side == 0 ? ArtefactDTO.ArtefactSide.recto : ArtefactDTO.ArtefactSide.verso, 
                zOrder = 0,
            };
        }
        
        public static ArtefactListDTO QueryArtefactListToArtefactListDTO(List<ArtefactsOfEditionQuery.Result> artefacts, uint editionId)
        {
            return new ArtefactListDTO()
            {
                artefacts = artefacts.Select(x => QueryArtefactToArtefactDTO(x, editionId)).ToList()
            };
        }
    }

    

    public class ArtefactDesignationDTO
    {
        public uint artefactId { get; set; }
        public uint imageCatalogId { get; set; }
        public string name { get; set; }
        public string side { get; set; }
    }

    public class ArtefactListDTO
    {
        public List<ArtefactDTO> artefacts { get; set; }
    }
    
    public class ArtefactDesignationListDTO
    {
        public List<ArtefactDesignationDTO> artefactDesignations { get; set; }
    }
}
