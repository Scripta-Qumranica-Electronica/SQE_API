using System.Collections.Generic;
// ReSharper disable ArrangeRedundantParentheses
namespace SQE.DatabaseAccess.Models
{
	public class SignInterpretationRoiData
	{
		public uint? SignInterpretationRoiId        { get; set; }
		public uint? SignInterpretationRoiCreatorId { get; set; }
		public uint? SignInterpretationRoiEditorId  { get; set; }
		public uint? ArtefactId                     { get; set; }
		public uint? SignInterpretationId           { get; set; }

		public string Shape { get; set; }

		//TODO I've no idea to what position refers in our database (Ingo)
		public string  Position                   { get; set; }
		public int?    TranslateX                 { get; set; }
		public int?    TranslateY                 { get; set; }
		public ushort? StanceRotation             { get; set; }
		public bool?   ValuesSet                  { get; set; }
		public bool?   Exceptional                { get; set; }
		public uint?   RoiShapeId                 { get; set; }
		public uint?   RoiPositionId              { get; set; }
		public uint    OldSignInterpretationRoiId { get; set; }
	}

	public class SignInterpretationROISearchData : SignInterpretationRoiData
												   , ISearchData
	{
		public string getSearchParameterString()
		{
			var searchParameters = new List<string>();

			if (SignInterpretationId != null)
				searchParameters.Add($"sign_interpretation_id = {SignInterpretationId}");

			if (SignInterpretationRoiId != null)
				searchParameters.Add($"sign_interpretation_roi_id = {SignInterpretationRoiId}");

			if (SignInterpretationRoiEditorId != null)
				searchParameters.Add($"edition_editor_id= {SignInterpretationRoiEditorId}");

			if (ValuesSet != null)
				searchParameters.Add($"values_set = {ValuesSet}");

			if (Exceptional != null)
				searchParameters.Add($"exceptional = {Exceptional}");

			if (ArtefactId != null)
				searchParameters.Add($"artefact_id = {ArtefactId}");

			if (TranslateX != null)
				searchParameters.Add($"translate_x = {TranslateX}");

			if (TranslateY != null)
				searchParameters.Add($"translate_y = {TranslateY}");

			if (StanceRotation != null)
				searchParameters.Add($"stance_rotation = {StanceRotation}");

			return string.Join(" AND ", searchParameters);

			//TODO I skipped Position because I don't understand what it means.
			// If it is not needed, it must be deleted also in getJoinString
		}

		public string getJoinsString()
		{
			var joins = "";

			if (Shape != null)
				joins = "JOIN roi_shape USING (roi_shape_id) ";

			if ((ArtefactId != null)
				|| (Position != null)
				|| (TranslateX != null)
				|| (TranslateY != null)
				|| (StanceRotation != null))
				joins += "JOIN roi_position USING (roi_position_id) ";

			return joins;
		}
	}
}
