using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class SignInterpretationAttributeValueInput
	{
		public string AttributeStringValue            { get; set; }
		public string AttributeStringValueDescription { get; set; }
		public string Css                             { get; set; }
	}

	public class SignInterpretationAttributeValue : SignInterpretationAttributeValueInput
	{
		public uint AttributeValueId { get; set; }
	}

	public class SignInterpretationAttributeEntry : SignInterpretationAttributeValue
	{
		public uint   AttributeId           { get; set; }
		public string AttributeName         { get; set; }
		public string AttributeDescription  { get; set; }
		public uint   AttributeCreator      { get; set; }
		public uint   AttributeEditor       { get; set; }
		public uint   AttributeValueCreator { get; set; }
		public uint   AttributeValueEditor  { get; set; }
		public bool   Editable              { get; set; }
		public bool   Removable             { get; set; }
		public bool   Repeatable            { get; set; }
		public bool   BatchEditable         { get; set; }
	}

	public class SignInterpretationAttributeData
	{
		public uint?  SignInterpretationAttributeId        { get; set; }
		public uint?  SignInterpretationId                 { get; set; }
		public byte?  Sequence                             { get; set; }
		public uint?  AttributeId                          { get; set; }
		public string AttributeString                      { get; set; }
		public uint?  AttributeValueId                     { get; set; }
		public string AttributeValueString                 { get; set; }
		public string AttributeCommentary                  { get; set; }
		public uint?  AttributeCommentaryCreatorId         { get; set; }
		public uint?  AttributeCommentaryEditorId          { get; set; }
		public uint?  SignInterpretationAttributeCreatorId { get; set; }
		public uint?  SignInterpretationAttributeEditorId  { get; set; }
		public bool   Editable                             { get; set; }
		public bool   Removable                            { get; set; }
		public bool   Repeatable                           { get; set; }
		public bool   BatchEditable                        { get; set; }
	}

	public class SignInterpretationAttributeDataSearchData : SignInterpretationAttributeData
															 , ISearchData
	{
		public string getSearchParameterString()
		{
			var searchParameters = new List<string>();

			if (SignInterpretationId != null)
				searchParameters.Add($"sign_interpretation_id = {SignInterpretationId}");

			if (SignInterpretationAttributeId != null)
			{
				searchParameters.Add(
						$"sign_interpretation_attribute_id = {SignInterpretationAttributeId}");
			}

			if (Sequence != null)
				searchParameters.Add($"sequence = {Sequence}");

			if (AttributeValueId != null)
				searchParameters.Add($"attribute_value_id = {AttributeValueId}");

			if (SignInterpretationAttributeEditorId != null)
				searchParameters.Add($"edition_editor_id = {SignInterpretationAttributeEditorId}");

			return string.Join(" AND ", searchParameters);
		}

		public string getJoinsString() => "";
	}

	public class AttributeDefinition
	{
		public uint   attributeId          { get; set; }
		public uint   attributeValueId     { get; set; }
		public string attributeString      { get; set; }
		public string attributeValueString { get; set; }
	}
}
