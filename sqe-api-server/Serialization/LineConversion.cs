using SQE.API.DTO;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Serialization
{
	public static partial class ExtensionsDTO
	{
		public static LineDataDTO ToDTO(this LineData line) => new LineDataDTO
		{
				lineId = line.LineId.Value
				, lineName = line.LineName
				, editorId = line.LineAuthor ?? 0
				,
		};
	}
}
