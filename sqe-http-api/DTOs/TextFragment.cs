using System.Collections.Generic;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class TextFragmentDataDTO
    {
        public uint colId { get; set; }
        public string colName { get; set; }
        
        public TextFragmentDataDTO(uint colId, string colName)
        {
            this.colId = colId;
            this.colName = colName;
        }
    }

    public class TextFragmentDataListDTO
    {
        public List<TextFragmentDataDTO> textFragments { get; set; }

        public TextFragmentDataListDTO(List<TextFragmentDataDTO> textFragments)
        {
            this.textFragments = textFragments;
        }
    }
    
    public class TextFragmentDTO
    {
        public uint textFragmentId { get; set; }
        public string textFragmentName { get; set; }
        public List<LineDTO> lines { get; set; }
    }

    public class LineDataDTO
    {
        public uint lineId { get; set; }
        public string lineName { get; set; }

        public LineDataDTO(uint lineId, string lineName)
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
    
    public class LineDTO
    {
        public uint lineId { get; set; }
        public string lineName { get; set; }
        public List<SignDTO> signs { get; set; }
    }

    public class LineTextDTO : LineDTO
    {
        public string licence { get; set; }
    }
}