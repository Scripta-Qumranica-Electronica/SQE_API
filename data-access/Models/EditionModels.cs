using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SQE.SqeHttpApi.DataAccess.Helpers;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class Edition
    {
        public uint EditionId { get; set; }
        public string Name { get; set; }
        public uint EditionDataEditorId { get; set; }
        public string ScrollId { get; set; }
        public Permission Permission { get; set; }
        public string Thumbnail { get; set; }
        public bool Locked { get; set; }
        public bool IsPublic {get; set;}
        public DateTime? LastEdit { get; set; }
        public User Owner { get; set; }
        public string Copyright { get; set; }
        public string CopyrightHolder { get; set; }
        public string Collaborators { get; set; }
    }

    public class Permission
    {
        public bool MayRead { get; set; }
        public bool MayWrite { get; set; }
        public bool MayLock { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class DetailedPermissions : Permission
    {
        public string Email { get; set; }
    }

    public class Share
    {
        public UserToken UserToken { get; set; }
        public Permission Permission { get; set; }
    }
    
    public class TextEdition
    {
        public uint manuscriptId { get; set; }
        public string editionName { get; set; }
        public string copyrightHolder { get; set; }
        public string collaborators { get; set; }
        public uint manuscriptAuthor { get; set; }
        public readonly List<TextFragment> fragments = new List<TextFragment>();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string licence { get; set; }


        /// <summary>
        /// Call this, if you want the the licence to be added on the output.
        /// If the collaborators field is empty, it will be populated from the list of editors.
        /// </summary>
        public void AddLicence(List<EditorInfo> editors = null)
        {
            var collab = collaborators;
            if (string.IsNullOrEmpty(collab))
                collab = editors == null ? 
                    copyrightHolder
                    : string.Join(", ", editors.Select(x => 
                        x.Forename + (!string.IsNullOrEmpty(x.Forename) &&  !string.IsNullOrEmpty(x.Surname) ? " " : "") 
                                   + x.Surname + (string.IsNullOrEmpty(x.Organization) ? "" : " (" + x.Organization + ")") ));
            licence = Licence.printLicence(copyrightHolder, collab);
        }
    }
}
