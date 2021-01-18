using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SQE.API.DTO
{
	public class ArtefactDataDTO
	{
		[Required]
		public uint id { get; set; }

		[Required]
		public string name { get; set; }
	}

	public class ArtefactDTO : ArtefactDataDTO
	{
		[Required]
		public uint editionId { get; set; }

		[Required]
		public string imagedObjectId { get; set; }

		[Required]
		public uint imageId { get; set; }

		[Required]
		public uint artefactDataEditorId { get; set; }

		[Required]
		public string mask { get; set; }

		[Required]
		public uint artefactMaskEditorId { get; set; }

		[Required]
		public bool isPlaced { get; set; }

		[Required]
		public PlacementDTO placement { get; set; }

		public uint? artefactPlacementEditorId { get; set; }

		[JsonConverter(typeof(JsonStringEnumConverter))]
		[Required]
		public SideDesignation side { get; set; }

		public string statusMessage { get; set; }
	}

	public class ExtendedArtefactDTO : ArtefactDTO
	{
		public string url { get; set; }
		public uint   ppi { get; set; }
	}

	public class ArtefactListDTO
	{
		[Required]
		public List<ArtefactDTO> artefacts { get; set; }
	}

	public class ExtendedArtefactListDTO
	{
		[Required]
		public List<ExtendedArtefactDTO> artefacts { get; set; }
	}

	public class ArtefactDataListDTO
	{
		[Required]
		public List<ArtefactDataDTO> artefacts { get; set; }
	}

	public class ArtefactGroupDTO : CreateArtefactGroupDTO
	{
		[Required]
		public uint id { get; set; }
	}

	public class ArtefactGroupListDTO
	{
		[Required]
		public List<ArtefactGroupDTO> artefactGroups { get; set; }
	}

	public class UpdateArtefactDTO
	{
		public string       mask      { get; set; }
		public PlacementDTO placement { get; set; }

		[StringLength(
				255
				, MinimumLength = 1
				, ErrorMessage = "Artefact names must be between 1 and 255 characters long")]
		public string name { get; set; }

		public string statusMessage { get; set; }
	}

	/// <summary>
	///  A DTO for updating an artefact's placement. The placement may be changed or
	///  removed completely. The PlacementDTO is not required because this update request
	///  may be setting isPlaced to false.
	/// </summary>
	public class UpdateArtefactPlacementDTO
	{
		[Required]
		public uint artefactId { get; set; }

		[Required]
		public bool isPlaced { get; set; }

		public PlacementDTO placement { get; set; }
	}

	public class BatchUpdateArtefactPlacementDTO
	{
		[Required]
		public List<UpdateArtefactPlacementDTO> artefactPlacements { get; set; }
	}

	public class UpdatedArtefactPlacementDTO : UpdateArtefactPlacementDTO
	{
		[Required]
		public uint placementEditorId { get; set; }
	}

	public class BatchUpdatedArtefactTransformDTO
	{
		[Required]
		public List<UpdatedArtefactPlacementDTO> artefactPlacements { get; set; }
	}

	public class CreateArtefactDTO : UpdateArtefactDTO
	{
		[Required]
		public uint masterImageId { get; set; }

		// Run a quick regex to make sure we have a the valid text for a WKT polygon (does not check polygon validity)
		// [RegularExpression(
		// 		@"^\s*[Pp][Oo][Ll][Yy][Gg][Oo][Nn]\s*\(\s*\(.*\)\s*\)\s*$"
		// 		, ErrorMessage = "The mask must be a valid WKT POLYGON description.")]
		// [Required]
		// public override string mask { get; set; }
	}

	public class UpdateArtefactGroupDTO
	{
		[MaxLength(255)]
		public string name { get; set; }

		[Required]
		[MinLength(1)]
		public List<uint> artefacts { get; set; }
	}

	public class CreateArtefactGroupDTO : UpdateArtefactGroupDTO
	{
		[Required]
		[MaxLength(255)]
		public new string name { get; set; }
	}
}
