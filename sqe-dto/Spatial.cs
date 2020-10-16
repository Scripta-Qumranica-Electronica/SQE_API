using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
	public class EditionScriptCollectionDTO
	{
		[Required]
		public List<CharacterShapeDTO> letters { get; set; }
	}

	public class EditionScriptLinesDTO
	{
		[Required]
		public List<ScriptTextFragmentDTO> textFragments { get; set; }
	}

	public class CharacterShapeDTO
	{
		[Required]
		public uint id { get; set; }

		[Required]
		public char character { get; set; }

		[Required]
		public string polygon { get; set; }

		[Required]
		public string imageURL { get; set; }

		public float rotation { get; set; }

		// In the next iteration use something more reliable for attributes, either the uint
		// attribute_id and the attribute_value_id or the attribute and value string.
		// Do not collect these using Dapper, because we don't want to transmit more than one of each WKB path 
		// from the database to the API.
		[Required]
		public List<string> attributes { get; set; }
	}

	public class ScriptTextFragmentDTO
	{
		[Required]
		public string textFragmentName { get; set; }

		[Required]
		public uint textFragmentId { get; set; }

		[Required]
		public List<ScriptLineDTO> lines { get; set; }
	}

	public class ScriptLineDTO
	{
		[Required]
		public string lineName { get; set; }

		[Required]
		public uint lineId { get; set; }

		[Required]
		public List<ScriptArtefactCharactersDTO> artefacts { get; set; }
	}

	public class ScriptArtefactCharactersDTO
	{
		[Required]
		public string artefactName { get; set; }

		[Required]
		public uint artefactId { get; set; }

		[Required]
		public PlacementDTO placement { get; set; }

		[Required]
		public List<SignInterpretationDTO> characters { get; set; }
	}
}
