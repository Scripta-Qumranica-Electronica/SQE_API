using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
	public class CreateScriptDataDTO
	{
		[Required]
		public ushort wordSpace { get; set; }

		[Required]
		public ushort lineSpace { get; set; }
	}

	public class ScriptDataDTO : CreateScriptDataDTO
	{
		public List<KernPairDTO>  kerningPairs  { get; set; }
		public List<GlyphDataDTO> glyphs        { get; set; }
		public uint               creatorId     { get; set; }
		public uint               editorId      { get; set; }
		public uint               scribalFontId { get; set; }
	}

	public class ScriptDataListDTO
	{
		public List<ScriptDataDTO> scripts { get; set; }
	}

	public class CreateKernPairDTO
	{
		[Required]
		[StringLength(1, ErrorMessage = "The {0} value cannot exceed {1} characters. ")]
		public string firstCharacter { get; set; }

		[Required]
		[StringLength(1, ErrorMessage = "The {0} value cannot exceed {1} characters. ")]
		public string secondCharacter { get; set; }

		[Required]
		public short xKern { get; set; }

		[Required]
		public short yKern { get; set; }
	}

	public class KernPairDTO : CreateKernPairDTO
	{
		public uint creatorId     { get; set; }
		public uint editorId      { get; set; }
		public uint scribalFontId { get; set; }
	}

	public class DeleteKernPairDTO
	{
		[Required]
		[StringLength(1, ErrorMessage = "The {0} value cannot exceed {1} characters. ")]
		public string firstCharacter { get; set; }

		[Required]
		[StringLength(1, ErrorMessage = "The {0} value cannot exceed {1} characters. ")]
		public string secondCharacter { get; set; }

		[Required]
		public uint editorId { get; set; }

		[Required]
		public uint scribalFontId { get; set; }
	}

	public class CreateGlyphDataDTO
	{
		[Required]
		[StringLength(1, ErrorMessage = "The {0} value cannot exceed {1} characters. ")]
		public string character { get; set; }

		[Required]
		public string shape { get; set; }

		public short yOffset { get; set; } = 0;
	}

	public class GlyphDataDTO : CreateGlyphDataDTO
	{
		public uint creatorId     { get; set; }
		public uint editorId      { get; set; }
		public uint scribalFontId { get; set; }
	}

	public class DeleteGlyphDataDTO
	{
		[Required]
		[StringLength(1, ErrorMessage = "The {0} value cannot exceed {1} characters. ")]
		public string character { get; set; }

		[Required]
		public uint editorId { get; set; }

		[Required]
		public uint scribalFontId { get; set; }
	}

	public class DeleteScribalFontDTO
	{
		[Required]
		public uint scribalFontId { get; set; }

		[Required]
		public uint editionEditorId { get; set; }
	}
}
