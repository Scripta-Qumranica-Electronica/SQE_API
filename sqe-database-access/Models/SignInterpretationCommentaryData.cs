using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using SQE.DatabaseAccess.Helpers;

namespace SQE.DatabaseAccess.Models
{
    public class SignInterpretationCommentaryData
    {
        public uint SignInterpretationCommentaryId { get; set; }
        public uint? SignInterpretationId { get; set; }
        public uint AttributeId { get; set; }
        public string Commentary { get; set; }
    }

    public class SignInterpretationCommentaryDataSearchData : SignInterpretationCommentaryData, ISearchData
    {
        

        public string CommentaryRegex { get; set; }

        public string getSearchParameterString()
        {
            var searchParameters = new List<string>();
            if (SignInterpretationId != null) searchParameters.Add($"sign_interpretation_id = {SignInterpretationId}");
            if (SignInterpretationCommentaryId != null)
                searchParameters.Add($"sign_interpretation_commentary_id = {SignInterpretationCommentaryId}");
            if (AttributeId != null) searchParameters.Add($"attribute_id = {AttributeId}");
            if (Commentary != null) searchParameters.Add($"commentary like '{Commentary}'");
            if (CommentaryRegex != null) searchParameters.Add($"commentary regexp '{CommentaryRegex}'");

            return String.Join(" AND ", searchParameters);
        }

        public string getJoinsString()
        {
            return "";
        }
    }

}