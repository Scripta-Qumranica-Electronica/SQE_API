using System.Collections.Generic;

namespace SQE.SqeApi.Server.DTOs
{
    
    #region output DTOs
    public class TextFragmentDataDTO
    {
        public uint id { get; set; }
        public string name { get; set; }
            
        public TextFragmentDataDTO(uint id, string name)
        {
            this.id = id;
            this.name = name;
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
        public uint editorId { get; set; }
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