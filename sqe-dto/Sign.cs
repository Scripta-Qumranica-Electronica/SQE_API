using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
	public class SignDTO
	{
		[Required]
		public List<SignInterpretationDTO> signInterpretations { get; set; }
	}

	public class NextSignInterpretationDTO
	{
		[Required]
		public uint nextSignInterpretationId { get; set; }

		[Required]
		public uint creatorId { get; set; }

		[Required]
		public uint editorId { get; set; }
	}

	public class SignInterpretationBaseDTO
	{
		public string character { get; set; }

		[Required]
		public bool isVariant { get; set; }
	}

	public class SignInterpretationCreateDTO : SignInterpretationBaseDTO
	{
		public uint?  lineId                        { get; set; }
		public uint[] previousSignInterpretationIds { get; set; }
		public uint[] nextSignInterpretationIds     { get; set; }

		[Required]
		public InterpretationAttributeCreateDTO[] attributes { get; set; }

		[Required]
		public SetInterpretationRoiDTO[] rois { get; set; }

		public CommentaryCreateDTO commentary { get; set; }

		public bool breakPreviousAndNextSignInterpretations { get; set; } = false;
	}

	public class SignInterpretationVariantDTO : InterpretationAttributeBaseDTO
	{
		[Required]
		[StringLength(
				1
				, ErrorMessage =
						"The character string must not exceed a single character in length.")]
		public string character { get; set; }
	}

	public class SignInterpretationCharacterUpdateDTO
	{
		[StringLength(
				1
				, ErrorMessage =
						"The character string must not exceed a single character in length.")]
		public string character { get; set; }

		[Range(1, 9, ErrorMessage = "The attribute value id must be null or a number from 1 to 9")]
		public uint? attributeValueId { get; set; }

		// Not yet implemented
		public byte priority { get; set; } = 0;
	}

	public class SignInterpretationDTO : SignInterpretationBaseDTO
	{
		public uint signId { get; set; }

		[Required]
		public uint signInterpretationId { get; set; }

		[Required]
		public NextSignInterpretationDTO[] nextSignInterpretations { get; set; }

		[Required]
		public InterpretationAttributeDTO[] attributes { get; set; }

		[Required]
		public InterpretationRoiDTO[] rois { get; set; }

		public CommentaryDTO commentary           { get; set; }
		public uint[]        signStreamSectionIds { get; set; }
		public uint[]        qwbWordIds           { get; set; }
	}

	public class SignInterpretationListDTO
	{
		public SignInterpretationDTO[] signInterpretations { get; set; }
	}

	public class SignInterpretationCreatedDTO
	{
		public SignInterpretationDTO[] created { get; set; }
		public SignInterpretationDTO[] updated { get; set; }
	}

	public class SignInterpretationDeleteDTO
	{
		public SignInterpretationListDTO updates { get; set; }
		public uint[]                    deletes { get; set; }
	}

	public class InterpretationAttributeBaseDTO
	{
		public byte? sequence { get; set; }

		[Required]
		public uint attributeId { get; set; }

		[Required]
		public uint attributeValueId { get; set; }
	}

	public class InterpretationAttributeCreateDTO : InterpretationAttributeBaseDTO
	{
		public string commentary { get; set; }
	}

	public class InterpretationAttributeDTO : InterpretationAttributeBaseDTO
	{
		[Required]
		public uint interpretationAttributeId { get; set; }

		[Required]
		public string attributeString { get; set; }

		[Required]
		public string attributeValueString { get; set; }

		[Required]
		public uint creatorId { get; set; }

		[Required]
		public uint editorId { get; set; }

		public CommentaryDTO commentary { get; set; }
	}

	public class CreateAttributeValueDTO
	{
		[Required]
		public string value { get; set; }

		public string description   { get; set; }
		public string cssDirectives { get; set; }
	}

	public class UpdateAttributeValueDTO : CreateAttributeValueDTO
	{
		[Required]
		public uint id { get; set; }
	}

	public class AttributeValueDTO : UpdateAttributeValueDTO
	{
		[Required]
		public uint creatorId { get; set; }

		[Required]
		public uint editorId { get; set; }
	}

	public class AttributeBaseDTO
	{
		public string description   { get; set; }
		public bool   editable      { get; set; }
		public bool   removable     { get; set; }
		public bool   repeatable    { get; set; }
		public bool   batchEditable { get; set; }
	}

	public class CreateAttributeDTO : AttributeBaseDTO
	{
		[Required]
		public string attributeName { get; set; }

		[Required]
		public CreateAttributeValueDTO[] values { get; set; }
	}

	public class UpdateAttributeDTO
	{
		[Required]
		public CreateAttributeValueDTO[] createValues { get; set; }

		[Required]
		public UpdateAttributeValueDTO[] updateValues { get; set; }

		[Required]
		public uint[] deleteValues { get; set; }

		public bool editable      { get; set; }
		public bool removable     { get; set; }
		public bool repeatable    { get; set; }
		public bool batchEditable { get; set; }
	}

	public class AttributeDTO : AttributeBaseDTO
	{
		[Required]
		public uint attributeId { get; set; }

		[Required]
		public string attributeName { get; set; }

		[Required]
		public AttributeValueDTO[] values { get; set; }

		[Required]
		public uint creatorId { get; set; }

		[Required]
		public uint editorId { get; set; }
	}

	public class AttributeListDTO
	{
		[Required]
		public AttributeDTO[] attributes { get; set; }
	}

	public class BasicDiffReplaceRequestDTO
	{
		[Required]
		public string newText { get; set; }
	}

	public class DiffReplaceRequestDTO : BasicDiffReplaceRequestDTO
	{
		[Required]
		public uint priorSignInterpretationId { get; set; }

		[Required]
		public uint followingSignInterpretationId { get; set; }
	}

	public class DiffReplaceReconstructionRequestDTO : BasicDiffReplaceRequestDTO
	{
		/// <summary>
		///  The key of this dictionary should be the index of the letter in the
		///  string to which the SetReconstructedInterpretationRoiDTO corresponds.
		///  It is not necessary for every character in the string to have a
		///  corresponding entry in textRois (for instance, a space character will
		///  probably have no associated ROI).
		/// </summary>
		[Required]
		public Dictionary<uint, SetReconstructedInterpretationRoiDTO> textRois { get; set; }

		[Required]
		public string virtualArtefactShape { get; set; }

		[Required]
		public PlacementDTO virtualArtefactPlacement { get; set; }
	}

	public class DiffReplaceResponseDTO
	{
		public SignInterpretationListDTO created { get; set; }
		public SignInterpretationListDTO updated { get; set; }
		public DeleteIntIdDTO            deleted { get; set; }
	}

	public class DiffReconstructedResponseDTO : DiffReplaceResponseDTO
	{
		public UpdateArtefactDTO virtualArtefact { get; set; }
	}
}
