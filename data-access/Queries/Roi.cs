namespace SQE.SqeHttpApi.DataAccess.Queries
{
	internal static class CreateRoiShapeQuery
	{
		public const string GetQuery = @"
INSERT INTO SQE.roi_shape (path)
VALUES (ST_GeomFromText(@Path))
";
	}

	internal static class CreateRoiPositionQuery
	{
		public const string GetQuery = @"
INSERT INTO SQE.roi_position (artefact_id, transform_matrix)
VALUES (@ArtefactId, @TransformMatrix)
";
	}

	internal static class GetSignInterpretationRoiDetailsQuery
	{
		public const string GetQuery = @"
SELECT roi_position.artefact_id AS ArtefactId,
       sign_interpretation_roi.sign_interpretation_id AS SignInterpretationId,
       ST_ASTEXT(roi_shape.path) AS Shape,
       roi_position.transform_matrix AS Position,
       sign_interpretation_roi.values_set AS ValuesSet,
       sign_interpretation_roi.exceptional AS Exceptional,
       sign_interpretation_roi.sign_interpretation_roi_id AS SignInterpretationRoiId,
       sign_interpretation_roi_owner.edition_editor_id AS SignInterpretationRoiAuthor,
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

	internal static class GetSignInterpretationRoiDetailsByArtefactIdQuery
	{
		public const string GetQuery = @"
SELECT roi_position.artefact_id AS ArtefactId,
       sign_interpretation_roi.sign_interpretation_id AS SignInterpretationId,
       ST_ASTEXT(roi_shape.path) AS Shape,
       roi_position.transform_matrix AS Position,
       sign_interpretation_roi.values_set AS ValuesSet,
       sign_interpretation_roi.exceptional AS Exceptional,
       sign_interpretation_roi.sign_interpretation_roi_id AS SignInterpretationRoiId,
       sign_interpretation_roi_owner.edition_editor_id AS SignInterpretationRoiAuthor,
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
}