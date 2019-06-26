using System.Collections.Generic;
using Newtonsoft.Json;
using SQE.SqeHttpApi.DataAccess.Helpers;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class TextEdition
    {
        public uint scrollId { get; set; }
        public string editionName { get; set; }
        public string copyrightHolder { get; set; }
        public string collaborators { get; set; }
        public readonly List<Fragment> fragments = new List<Fragment>();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string licence = "Licence";


        // TODO We have to decide how to retrieve them: either on the fly for the real text chunk or should we use an "Author" field in the edition table?
        // TODO If the collaborators field in the edition table is not null, then use that.  If it is null, then collect the
        // names of all the edition editors.
        /// <summary>
        /// Gets all Contributors to the text chunk contained in the current instance
        /// </summary>
        /// <returns>String with all contributors</returns>
        public string getAuthors()
        {
            return collaborators;
        }


        /// <summary>
        /// Call this, if you want the the licence to be added on the output.
        /// </summary>
        public void addLicence()
        {
            licence = Licence.printLicence(copyrightHolder, collaborators);
        }
    }


}