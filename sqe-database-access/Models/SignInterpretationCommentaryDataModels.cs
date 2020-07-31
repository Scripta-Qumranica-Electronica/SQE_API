using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class SignInterpretationCommentaryData
    {
        public uint? SignInterpretationCommentaryId { get; set; }
        public uint? SignInterpretationId { get; set; }
        public uint? AttributeId { get; set; }
        public string Commentary { get; set; }
    }

    public class SignInterpretationCommentaryDataSearchData : SignInterpretationCommentaryData, ISearchData
    {
        public string CommentaryRegex { get; set; }

        public string getSearchParameterString()
        {
            var searchParameters = new List<string>();
            if (SignInterpretationId.HasValue)
                searchParameters.Add($"sign_interpretation_id = {SignInterpretationId.Value}");
            if (SignInterpretationCommentaryId.HasValue)
                searchParameters.Add($"sign_interpretation_commentary_id = {SignInterpretationCommentaryId.Value}");
            if (AttributeId.HasValue) searchParameters.Add($"attribute_id = {AttributeId.Value}");
            if (!string.IsNullOrEmpty(Commentary)) searchParameters.Add($"commentary like '{Commentary}'");
            if (!string.IsNullOrEmpty(CommentaryRegex)) searchParameters.Add($"commentary regexp '{CommentaryRegex}'");

            return string.Join(" AND ", searchParameters);
        }

        public string getJoinsString()
        {
            return "";
        }
    }
}