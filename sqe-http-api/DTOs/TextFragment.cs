using System.Collections.Generic;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class TextFragmentDTO
    {
        public string colId { get; set; }
        public string colName { get; set; }
        
        public TextFragmentDTO(string colId, string colName)
        {
            this.colId = colId;
            this.colName = colName;
        }
    }

    public class TextFragmentListDTO
    {
        public List<TextFragmentDTO> textFragments { get; set; }

        public TextFragmentListDTO(List<TextFragmentDTO> textFragments)
        {
            this.textFragments = textFragments;
        }
    }

    public class LineDataDTO
    {
        public string lineId { get; set; }
        public string lineName { get; set; }

        public LineDataDTO(string lineId, string lineName)
        {
            this.lineId = lineId;
            this.lineName = lineName;
        }
    }

    public class LineDataListDTO
    {
        public List<LineDataDTO> lines { get; set; }

        public LineDataListDTO(List<LineDataDTO> lines)
        {
            this.lines = lines;
        }
    }
}