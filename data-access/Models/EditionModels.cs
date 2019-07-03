using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SQE.SqeHttpApi.DataAccess.Helpers;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class Edition
    {
        public uint EditionId { get; set; }
        public string Name { get; set; }
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
        public bool CanWrite { get; set; }
        public bool CanLock { get; set; }
        public bool CanAdmin { get; set; }
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
        /// </summary>
        public void addLicence()
        {
            licence = Licence.printLicence(copyrightHolder, collaborators);
        }
    }
}
