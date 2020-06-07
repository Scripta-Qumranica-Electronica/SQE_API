using System.Collections.Generic;

namespace SQE.API.DTO
{
    public class EditionScriptCollectionDTO
    {
        public List<CharacterShapeDTO> letters { get; set; }
    }

    public class EditionScriptLinesDTO
    {
        public List<ScriptTextFragmentDTO> textFragments { get; set; }
    }

    public class CharacterShapeDTO
    {
        public uint id { get; set; }
        public char character { get; set; }
        public string polygon { get; set; }
        public string imageURL { get; set; }
        public float rotation { get; set; }

        // In the next iteration use something more reliable for attributes, either the uint
        // attribute_id and the attribute_value_id or the attribute and value string.
        // Do not collect these using Dapper, because we don't want to transmit more than one of each WKB path 
        // from the database to the API.
        public List<string> attributes { get; set; }
    }

    public class ScriptTextFragmentDTO
    {
        public string textFragmentName { get; set; }
        public uint textFragmentId { get; set; }
        public List<ScriptLineDTO> lines { get; set; }
    }

    public class ScriptLineDTO
    {
        public string lineName { get; set; }
        public uint lineId { get; set; }
        public List<ScriptArtefactCharactersDTO> artefacts { get; set; }
    }

    public class ScriptArtefactCharactersDTO
    {
        public string artefactName { get; set; }
        public uint artefactId { get; set; }
        public PolygonDTO mask { get; set; }
        public List<SignInterpretationDTO> characters { get; set; }
    }
}