using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SQE.SqeHttpApi.Server.DTOs
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
        public List<ShareDTO> shares { get; set; }
        public bool locked { get; set; }
        public bool isPublic { get; set; }
        public DateTime? lastEdit { set; get; }
        public string copyright { get; set; }
    }

    public class EditionGroupDTO
    {
        public EditionDTO primary { get; set; }
        public IEnumerable<EditionDTO> others { get; set; }
    };

    public class EditionListDTO
    {
        public List<List<EditionDTO>> editions { get; set; }
    };

    public class PermissionDTO
    {
        public bool mayWrite { get; set; }
        public bool isAdmin { get; set; }
    }

    public class EditorRightsDTO
    {
        public string email { get; set; }
        public bool? mayRead { get; set; }
        public bool? isAdmin { get; set; }
        public bool? mayLock { get; set; }
        public bool? mayWrite { get; set; }
    }
    
    public class TextEditionDTO
    {
        public uint manuscriptId { get; set; }
        public string editionName { get; set; }
        public uint editorId { get; set; }
        public string licence { get; set; }
        public Dictionary<uint, EditorDTO> editors { get; set; }
        public List<TextFragmentDTO> textFragments { get; set; }
    }

    public class ShareDTO
    {
        public UserDTO user { get; set; }
        public PermissionDTO permission { get; set; }
    }

    public class DeleteTokenDTO
    {
        public uint editionId { get; set; }
        public string token { get; set; }
    }

    #region Request DTO's
    public class EditionUpdateRequestDTO
    {
        /// <summary>
        /// Metadata to be added to or updated for an edition 
        /// </summary>
        /// <param name="name">Name of the work being edited. If null, the name is left the same as it currently is.</param>
        /// <param name="copyrightHolder">Name of the copyright holder. This is a required field.
        /// If left null, then the name of the first editor of the edition is input.</param>
        /// <param name="collaborators">Name(s) of the collaborator(s) working officially on the edition.
        /// This may be null, in which case the names of all edition editors are collected automatically
        /// and added to the edition license.</param>
        public EditionUpdateRequestDTO(string name, string copyrightHolder, string collaborators)
        {
            this.name = name;
            this.copyrightHolder = copyrightHolder;
            this.collaborators = collaborators;
        }

        public string name { get; set; }
        public string copyrightHolder { get; set; }
        public string collaborators { get; set; }
    }

    public class EditionCopyDTO : EditionUpdateRequestDTO
    {
        public EditionCopyDTO(string name, string copyrightHolder, string collaborators) 
            : base(name, copyrightHolder, collaborators) {}
    }
    #endregion Request DTO's
}
