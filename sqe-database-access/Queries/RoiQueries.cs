namespace SQE.DatabaseAccess.Queries
{
	//TODO Probably most of the queries can be replace by the new queries GetSignInterpretationRoiDetailsByDataQuery
	// and GetRoiIdByData using SignInterpretationROISearchData
	internal static class CreateRoiShapeQuery
	{
		// Added here an ad-hoc uniqueness constraint, we may need an index on `path` for better performance
		public const string GetQuery = @"
INSERT INTO roi_shape (path)
SELECT ST_GeomFromText(@Path)
FROM dual
WHERE NOT EXISTS (
    SELECT path
    FROM roi_shape
    WHERE path = ST_GeomFromText(@Path)
)
";
	}

	internal static class GetRoiShapeIdQuery
	{
		// Added here an ad-hoc uniqueness constraint, we may need an index on `path` for better performance
		public const string GetQuery = @"
SELECT roi_shape_id
FROM roi_shape
WHERE path = ST_GeomFromText(@Path)
";
	}

	internal static class CreateRoiPositionQuery
	{
		public const string GetQuery = @"
INSERT INTO roi_position (artefact_id, translate_x, translate_y, stance_rotation)
SELECT @ArtefactId, @TranslateX, @TranslateY, @StanceRotation
FROM dual
WHERE NOT EXISTS (
    SELECT artefact_id, translate_x, translate_y, stance_rotation
    FROM roi_position
    WHERE (artefact_id, translate_x, translate_y, stance_rotation) = 
          (@ArtefactId, @TranslateX, @TranslateY, @StanceRotation)
)
";
	}

	internal static class GetRoiPositionIdQuery
	{
		public const string GetQuery = @"
SELECT roi_position_id
FROM roi_position
WHERE (artefact_id, translate_x, translate_y, stance_rotation) = 
      (@ArtefactId, @TranslateX, @TranslateY, @StanceRotation)
";
	}

	internal static class GetSignInterpretationRoiDetailsQuery
	{
		public const string GetQuery = @"
SELECT roi_position.artefact_id AS ArtefactId,
       sign_interpretation_roi.sign_interpretation_id AS SignInterpretationId,
       ST_ASTEXT(roi_shape.path) AS Shape,
       roi_position.translate_x AS TranslateX,
       roi_position.translate_y AS TranslateY,
       roi_position.stance_rotation AS StanceRotation,
       sign_interpretation_roi.values_set AS ValuesSet,
       sign_interpretation_roi.exceptional AS Exceptional,
       sign_interpretation_roi.sign_interpretation_roi_id AS SignInterpretationRoiId,
       sign_interpretation_roi.creator_id AS SignInterpretationRoiCreatorId,
       sign_interpretation_roi_owner.edition_editor_id AS SignInterpretationRoiEditorId,
       sign_interpretation_roi.roi_shape_id AS RoiShapeId,
       sign_interpretation_roi.roi_position_id AS RoiPositionId
FROM sign_interpretation_roi
JOIN roi_position USING(roi_position_id)
JOIN roi_shape USING(roi_shape_id)
JOIN sign_interpretation_roi_owner 
    ON sign_interpretation_roi_owner.sign_interpretation_roi_id = sign_interpretation_roi.sign_interpretation_roi_id
	AND sign_interpretation_roi_owner.edition_id = @EditionId
WHERE sign_interpretation_roi.sign_interpretation_roi_id = @SignInterpretationRoiId
";
	}

	/// <summary>
	///  Template for getting all RoiData searched by using SignInterpretationROISearchData.getSearchParameterString.
	///  getJoinsString is not needed since all joins must be set for the select.
	/// </summary>
	internal static class GetSignInterpretationRoiDetailsByDataQuery
	{
		public const string GetQuery = @"
SELECT roi_position.artefact_id AS ArtefactId,
       sign_interpretation_roi.sign_interpretation_id AS SignInterpretationId,
       ST_ASTEXT(roi_shape.path) AS Shape,
       roi_position.translate_x AS TranslateX,
       roi_position.translate_y AS TranslateY,
       roi_position.stance_rotation AS StanceRotation,
       sign_interpretation_roi.values_set AS ValuesSet,
       sign_interpretation_roi.exceptional AS Exceptional,
       sign_interpretation_roi.sign_interpretation_roi_id AS SignInterpretationRoiId,
       sign_interpretation_roi.creator_id AS SignInterpretationRoiCreatorId,
       sign_interpretation_roi_owner.edition_editor_id AS SignInterpretationRoiEditorId,
       sign_interpretation_roi.roi_shape_id AS RoiShapeId,
       sign_interpretation_roi.roi_position_id AS RoiPositionId
FROM sign_interpretation_roi
JOIN roi_position USING(roi_position_id)
JOIN roi_shape USING(roi_shape_id)
JOIN sign_interpretation_roi_owner 
    ON sign_interpretation_roi_owner.sign_interpretation_roi_id = sign_interpretation_roi.sign_interpretation_roi_id
	AND sign_interpretation_roi_owner.edition_id = @EditionId
WHERE @WhereData
";
	}

	internal static class GetSignInterpretationRoiDetailsByArtefactIdQuery
	{
		public const string GetQuery = @"
SELECT roi_position.artefact_id AS ArtefactId,
       sign_interpretation_roi.sign_interpretation_id AS SignInterpretationId,
       ST_ASTEXT(roi_shape.path) AS Shape,
       roi_position.translate_x AS TranslateX,
       roi_position.translate_y AS TranslateY,
       roi_position.stance_rotation AS StanceRotation,
       sign_interpretation_roi.values_set AS ValuesSet,
       sign_interpretation_roi.exceptional AS Exceptional,
       sign_interpretation_roi.sign_interpretation_roi_id AS SignInterpretationRoiId,
       sign_interpretation_roi.creator_id AS SignInterpretationRoiCreatorId,
       sign_interpretation_roi_owner.edition_editor_id AS SignInterpretationRoiEditorId,
       sign_interpretation_roi.roi_shape_id AS RoiShapeId,
       sign_interpretation_roi.roi_position_id AS RoiPositionId
FROM sign_interpretation_roi
JOIN roi_position USING(roi_position_id)
JOIN roi_shape USING(roi_shape_id)
JOIN sign_interpretation_roi_owner 
    ON sign_interpretation_roi_owner.sign_interpretation_roi_id = sign_interpretation_roi.sign_interpretation_roi_id
	AND sign_interpretation_roi_owner.edition_id = @EditionId
WHERE roi_position.artefact_id = @ArtefactId
";
	}

	/// <summary>
	///  Template for getting all ids of sign_interpretation_roi by using
	///  SignInterpretationROISearchData.getSearchParameterString
	///  and getJoinsString
	/// </summary>
	internal static class GetRoiIdByData
	{
		public const string GetQuery = @"
SELECT sign_interpretation_roi.sign_interpretation_roi_id AS SignInterpretationRoiId
FROM sign_interpretation_roi
@JoinString
JOIN sign_interpretation_roi_owner 
    ON sign_interpretation_roi_owner.sign_interpretation_roi_id = sign_interpretation_roi.sign_interpretation_roi_id
	AND sign_interpretation_roi_owner.edition_id = @EditionId
WHERE @WhereData
";
	}
}
