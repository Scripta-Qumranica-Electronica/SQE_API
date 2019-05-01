using SQE.SqeHttpApi.DataAccess.Models;

namespace SQE.SqeHttpApi.DataAccess.Helpers
{
    // TODO We still have to find a final formulation.
    /// <summary>
    /// Provides the licence and copyright texts for a text
    /// </summary>
    public static class Licence
    {
        public const string licenceText =
            @"This work is licensed under the Creative Commons Attribution 4.0 International License. 
To view a copy of this license, visit http://creativecommons.org/licenses/by/4.0/ 
or send a letter to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.";

        public const string copyright = "© SQE-Project (https://www.qumranica.org/)";


        public static string printLicence(Scroll scroll)
        {
            return $@"{copyright} 

Provided by {scroll.getAuthors()} on the base of a text provided by the Qumran-Wörterbuch of the Göttingen Academy of Scieneces 
based on a preliminary text provided Martin Abegg.

{licenceText}";
        }
    }
}