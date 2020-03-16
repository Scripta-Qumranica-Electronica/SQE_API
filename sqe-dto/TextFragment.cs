using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
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

        public TextFragmentDataDTO() : this(uint.MinValue, string.Empty, uint.MinValue)
        {
        }

        public uint id { get; set; }
        public string name { get; set; }

        public uint editorId { get; set; }
    }

    public class ArtefactTextFragmentMatchDTO : TextFragmentDataDTO
    {
        /// <summary>
        ///     This DTO contains the data of a text fragment that has been requested via
        ///     artefact id.
        /// </summary>
        /// <param name="id">Id of the text fragment</param>
        /// <param name="name">Name of the text fragment</param>
        /// <param name="editorId">Id of the editor who defined the text fragment</param>
        /// <param name="suggested">
        ///     Whether this text fragment was suggest by the system (true)
        ///     or is a definite match (false)
        /// </param>
        public ArtefactTextFragmentMatchDTO(uint id, string name, uint editorId, bool suggested) : base(
            id,
            name,
            editorId
        )
        {
            this.suggested = suggested;
        }

        public bool suggested { get; set; }
    }
    
    public class ImagedObjectTextFragmentMatchDTO 
    {
        public ImagedObjectTextFragmentMatchDTO(uint editionId, string manuscriptName, uint textFragmentId, 
            string textFragmentName, string side)
        {
            this.editionId = editionId;
            this.manuscriptName = manuscriptName;
            this.textFragmentId = textFragmentId;
            this.textFragmentName = textFragmentName;
            this.side = side;
        }

        public uint editionId { get; set; }
        public string manuscriptName { get; set; }
        public uint textFragmentId { get; set; }
        public string textFragmentName { get; set; }
        public string side { get; set; }
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

    public class ArtefactTextFragmentMatchListDTO
    {
        public ArtefactTextFragmentMatchListDTO(List<ArtefactTextFragmentMatchDTO> textFragments)
        {
            this.textFragments = textFragments;
        }

        public List<ArtefactTextFragmentMatchDTO> textFragments { get; set; }
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

        public LineDataDTO() : this(uint.MinValue, string.Empty)
        {
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

        public LineDataListDTO() : this(null)
        {
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
        public Dictionary<string, EditorDTO> editors { get; set; }
    }

    #endregion output DTOs

    #region Input DTOs

    public class UpdateTextFragmentDTO
    {
        public virtual string name { get; set; }
        public uint? previousTextFragmentId { get; set; }
        public uint? nextTextFragmentId { get; set; }
    }

    public class CreateTextFragmentDTO : UpdateTextFragmentDTO
    {
        [Required]
        [StringLength(
            255,
            MinimumLength = 1,
            ErrorMessage = "Text fragment names must be between 1 and 255 characters"
        )]
        public override string name { get; set; }
    }

    #endregion
}