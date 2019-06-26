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
}