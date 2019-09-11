using System.Collections.Generic;

namespace SQE.SqeHttpApi.Server.DTOs
{
	#region output DTOs

	public class TextFragmentDataDTO
	{
		public TextFragmentDataDTO(uint id, string name, uint editorId)
		{
			this.id = id;
			this.name = name;
			this.editorId = editorId;
		}

		public uint id { get; set; }
		public string name { get; set; }

		public uint editorId { get; set; }
	}

	public class TextFragmentDataListDTO
	{
		public TextFragmentDataListDTO(List<TextFragmentDataDTO> textFragments)
		{
			this.textFragments = textFragments;
		}

		public TextFragmentDataListDTO() : this(null)
		{
		}

		public List<TextFragmentDataDTO> textFragments { get; set; }
	}

	public class TextFragmentDTO
	{
		public uint textFragmentId { get; set; }
		public string textFragmentName { get; set; }
		public uint editorId { get; set; }
		public List<LineDTO> lines { get; set; }
	}

	public class LineDataDTO
	{
		public LineDataDTO(uint lineId, string lineName)
		{
			this.lineId = lineId;
			this.lineName = lineName;
		}

		public uint lineId { get; set; }
		public string lineName { get; set; }
	}

	public class LineDataListDTO
	{
		public LineDataListDTO(List<LineDataDTO> lines)
		{
			this.lines = lines;
		}

		public List<LineDataDTO> lines { get; set; }
	}

	public class LineDTO
	{
		public uint lineId { get; set; }
		public string lineName { get; set; }
		public uint editorId { get; set; }
		public List<SignDTO> signs { get; set; }
	}

	public class LineTextDTO : LineDTO
	{
		public string licence { get; set; }
		public Dictionary<uint, EditorDTO> editors { get; set; }
	}

	#endregion output DTOs

	#region Input DTOs

	public class CreateTextFragmentDTO
	{
		public string name { get; set; }
		public uint? previousTextFragmentId { get; set; }
		public uint? nextTextFragmentId { get; set; }
	}

	#endregion
}