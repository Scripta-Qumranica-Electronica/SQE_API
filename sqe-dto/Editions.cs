using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SQE.API.DTO
{
    // TODO: get rid of owner and instead provide a list of editors (don't give the email addresses)
    public class EditionDTO
    {
        public uint id { get; set; }
        public string name { get; set; }
        public uint editionDataEditorId { get; set; }
        public PermissionDTO permission { get; set; }
        public UserDTO owner { get; set; }
        public string thumbnailUrl { get; set; }
        public List<DetailedEditorRightsDTO> shares { get; set; }
        public bool locked { get; set; }
        public bool isPublic { get; set; }
        public DateTime? lastEdit { set; get; }
        public string copyright { get; set; }
    }

    public class EditionGroupDTO
    {
        public EditionDTO primary { get; set; }
        public IEnumerable<EditionDTO> others { get; set; }
    }

    public class EditionListDTO
    {
        public List<List<EditionDTO>> editions { get; set; }
    }

    public class PermissionDTO
    {
        public bool mayRead { get; set; }
        public bool mayWrite { get; set; }
        public bool isAdmin { get; set; }
    }

    public class UpdateEditorRightsDTO : PermissionDTO
    {
        public bool mayLock { get; set; }
    }

    public class InviteEditorDTO : UpdateEditorRightsDTO
    {
        [Required]
        [RegularExpression(@"^.*@.*\..*$", ErrorMessage = "The email address appears to be improperly formatted")]
        public string email { get; set; }
    }

    public class DetailedEditorRightsDTO : UpdateEditorRightsDTO
    {
        [Required]
        [RegularExpression(@"^.*@.*\..*$", ErrorMessage = "The email address appears to be improperly formatted")]
        public string email { get; set; }
        public uint editionId { get; set; }
    }

    public class DetailedUpdateEditorRightsDTO : UpdateEditorRightsDTO
    {
        public uint editionId { get; set; }
        public string editionName { get; set; }
        public DateTime date { get; set; }
    }

    public class AdminEditorRequestDTO : DetailedUpdateEditorRightsDTO
    {
        public string editorName { get; set; }
        public string editorEmail { get; set; }
    }

    public class EditorInvitationDTO : DetailedUpdateEditorRightsDTO
    {
        public Guid token { get; set; }
        public string requestingAdminName { get; set; }
        public string requestingAdminEmail { get; set; }
    }

    public class EditorInvitationListDTO
    {
        public List<EditorInvitationDTO> editorInvitations { get; set; }
    }

    public class AdminEditorRequestListDTO
    {
        public List<AdminEditorRequestDTO> editorRequests { get; set; }
    }

    public class TextEditionDTO
    {
        public uint manuscriptId { get; set; }
        public string editionName { get; set; }
        public uint editorId { get; set; }
        public string licence { get; set; }
        public Dictionary<string, EditorDTO> editors { get; set; }
        public List<TextFragmentDTO> textFragments { get; set; }
    }

    public class DeleteTokenDTO
    {
        public uint editionId { get; set; }
        public string token { get; set; }
    }

    public class DeleteEditionEntityDTO
    {
        public uint entityId { get; set; }
        public uint editorId { get; set; }
    }



    /// <summary>
    /// This is a list of all entities in an edition, including the edition itself.
    /// This is initially intended to be used with the DeleteDTO object
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EditionEntities
    {
        edition,
        artefact,
        textFragment,
        line,
        signInterpretation,
        roi
    }

    public class DeleteDTO
    {
        public DeleteDTO(EditionEntities entity, List<uint> ids)
        {
            this.entity = entity;
            this.ids = ids;
        }

        public EditionEntities entity { get; set; }
        public List<uint> ids { get; set; }
    }

    #region Request DTO's

    public class EditionUpdateRequestDTO
    {
        /// <summary>
        ///     Metadata to be added to or updated for an edition
        /// </summary>
        /// <param name="name">Name of the work being edited. If null, the name is left the same as it currently is.</param>
        /// <param name="copyrightHolder">
        ///     Name of the copyright holder. This is a required field.
        ///     If left null, then the name of the first editor of the edition is input.
        /// </param>
        /// <param name="collaborators">
        ///     Name(s) of the collaborator(s) working officially on the edition.
        ///     This may be null, in which case the names of all edition editors are collected automatically
        ///     and added to the edition license.
        /// </param>
        public EditionUpdateRequestDTO(string name, string copyrightHolder, string collaborators)
        {
            this.name = name;
            this.copyrightHolder = copyrightHolder;
            this.collaborators = collaborators;
        }

        public EditionUpdateRequestDTO() : this(string.Empty, string.Empty, string.Empty)
        {
        }


        [StringLength(
            255,
            MinimumLength = 1,
            ErrorMessage = "The name of the edition must be between 1 and 255 characters long"
        )]
        public string name { get; set; }

        public string copyrightHolder { get; set; }
        public string collaborators { get; set; }
    }

    public class EditionCopyDTO : EditionUpdateRequestDTO
    {
        public EditionCopyDTO(string name, string copyrightHolder, string collaborators)
            : base(name, copyrightHolder, collaborators)
        {
        }

        public EditionCopyDTO() : this(string.Empty, string.Empty, string.Empty)
        {
        }
    }

    #endregion Request DTO's
}