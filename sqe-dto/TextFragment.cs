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
        /// <param name="editorId">Id of the editor who sefined the text fragment</param>
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

    public class TextFragmentDataListDTO
    {
        public TextFragmentDataListDTO(List<TextFragmentDataDTO> textFragments)
        {
            this.textFragments = textFragments;
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
        [Required]
        [StringLength(
            255,
            MinimumLength = 1,
            ErrorMessage = "Text fragment names must be between 1 and 255 characters"
        )]
        public string name { get; set; }

        public uint? previousTextFragmentId { get; set; }
        public uint? nextTextFragmentId { get; set; }
    }

    #endregion
}