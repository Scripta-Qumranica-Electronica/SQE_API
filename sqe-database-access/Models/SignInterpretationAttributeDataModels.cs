using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class SignEnterpretationAttributeEntry
    {
        public uint AttributeId { get; set; }
        public string AttributeName { get; set; }
        public string AttributeDescription { get; set; }
        public uint AttributeCreator { get; set; }
        public uint AttributeEditor { get; set; }
        public uint AttributeValueId { get; set; }
        public string AttributeStringValue { get; set; }
        public string AttributeStringValueDescription { get; set; }
        public uint AttributeValueCreator { get; set; }
        public uint AttributeValueEditor { get; set; }
        public string Css { get; set; }
    }

    public class SignInterpretationAttributeData
    {
        public uint? SignInterpretationAttributeId { get; set; }
        public uint? SignInterpretationId { get; set; }
        public byte? Sequence { get; set; }
        public uint? AttributeValueId { get; set; }
        public string AttributeString { get; set; }
        public uint? SignInterpretationAttributeAuthor { get; set; }
        public float? NumericValue { get; set; }
    }

    public class SignInterpretationAttributeDataSearchData : SignInterpretationAttributeData, ISearchData
    {
        public float? NumericValueMoreThan { get; set; }
        public float? NumericValueLessThan { get; set; }

        public string getSearchParameterString()
        {
            var searchParameters = new List<string>();
            if (SignInterpretationId != null) searchParameters.Add($"sign_interpretation_id = {SignInterpretationId}");
            if (SignInterpretationAttributeId != null)
                searchParameters.Add($"sign_interpretation_attribute_id = {SignInterpretationAttributeId}");
            if (Sequence != null) searchParameters.Add($"sequence = {Sequence}");
            if (AttributeValueId != null) searchParameters.Add($"attribute_value_id = {AttributeValueId}");
            if (SignInterpretationAttributeAuthor != null)
                searchParameters.Add($"edition_editor_id = {SignInterpretationAttributeAuthor}");
            if (NumericValue != null) searchParameters.Add($"numeric_value = {NumericValue}");
            if (NumericValueMoreThan != null) searchParameters.Add($"numeric_value > {NumericValueMoreThan}");
            if (NumericValueLessThan != null) searchParameters.Add($"numeric_value < {NumericValueLessThan}");

            return string.Join(" AND ", searchParameters);
        }

        public string getJoinsString()
        {
            return "";
        }
    }

    public class AttributeDefinition
    {
        public uint attributeValueId { get; set; }
        public string attributeString { get; set; }
    }
}