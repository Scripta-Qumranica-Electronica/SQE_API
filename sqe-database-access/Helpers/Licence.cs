using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using MySqlX.XDevAPI;
using SQE.DatabaseAccess.Models;

namespace SQE.DatabaseAccess.Helpers
{
    // TODO We still have to find a final formulation.
    /// <summary>
    ///     Provides the licence and copyright texts for a text
    /// </summary>
    public static class Licence
    {
        public const string licenceText =
            @"This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
To view a copy of this license, visit https://creativecommons.org/licenses/by-sa/4.0/legalcode 
or send a letter to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.";

        /// <summary>
        /// Formats a license statement from the submitted information. All licenses must be at least
        /// as permissive as CC-BY-SA within the Qumranica database due to its internal operational
        /// characteristics. This code ensures that all data sent from this API clearly conveys the
        /// necessary license.
        /// </summary>
        /// <param name="copyrightHolder">Name of the individual or organization holding the copyright</param>
        /// <param name="contributors">A list of all contributors</param>
        /// <param name="editors">An optional list of editors that can be used to automatically
        /// generate a list of contributors if the contributors variable is null.</param>
        /// <returns>A formatted license statement</returns>
        public static string printLicence(string copyrightHolder, string contributors, IEnumerable<EditorInfo> editors = null)
        {
            var formattedEditors = editors == null
                ? ""
                : string.Join(", ", editors.Select(FormatEditorName));
            contributors ??= formattedEditors;
            return $@"© {copyrightHolder} 

Provided by {AddAndToLastComma(contributors)} on the basis of a text provided by the Qumran-Wörterbuch of the Göttingen Academy of Sciences, 
which is based upon a preliminary text provided by Martin Abegg.

{licenceText}";
        }

        /// <summary>
        /// Examines the editor info and provides a human readable version
        /// of the editor's name.
        /// </summary>
        /// <param name="editor">Details of the editor</param>
        /// <returns>A string with the editors formatted name</returns>
        private static string FormatEditorName(EditorInfo editor)
        {
            return $@"{editor.Forename} {editor.Surname}".Trim();
        }

        /// <summary>
        /// Examine a string and add " and" after the last comma.
        /// This makes lists more human readable.
        /// </summary>
        /// <param name="text">The text to process</param>
        /// <returns>A string with " and" added after the last comma</returns>
        private static string AddAndToLastComma(string text)
        {
            var idxOfLastComma = text.LastIndexOf(',');
            return idxOfLastComma != -1
                ? text.Remove(idxOfLastComma, 1).Insert(idxOfLastComma, ", and")
                : text;
        }
    }


}