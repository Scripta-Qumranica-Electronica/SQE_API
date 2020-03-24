using System;
using System.Collections.Generic;
using System.Linq;

namespace SQE.DatabaseAccess.Queries
{
    internal class EditionGroupQuery
    {
        private const string _baseQuery = @"
SELECT DISTINCT ed2.edition_id AS EditionId,
    ed2.copyright_holder AS CopyrightHolder,
    COALESCE(
        ed2.collaborators, 
        GROUP_CONCAT(DISTINCT CONCAT(user.forename, ' ', user.surname)
        SEPARATOR ', ')) AS Collaborators,
    current_editor.is_admin AS Admin,
    manuscript_data.name AS Name, 
    manuscript_data_owner.edition_editor_id AS EditionDataEditorId,
    im.thumbnail_url AS Thumbnail, 
    ed2.locked AS Locked,
    ed2.public AS IsPublic,
    ed2.manuscript_id AS ScrollId,
    current_editor.may_lock AS MayLock,
    current_editor.may_write AS MayWrite, 
    current_editor.may_read AS MayRead,
    last.last_edit AS LastEdit, 
    current_editor.user_id AS UserId, 
    current_editor_details.email AS Email 
FROM edition AS ed1
JOIN (SELECT edition.edition_id, 
             edition.copyright_holder, 
             edition.collaborators, 
             edition.locked, 
             edition.manuscript_id, 
             edition.public
      FROM edition
      JOIN edition_editor USING(edition_id)
      WHERE (edition.public = 1 $UserFilter)
        AND (edition.public = 1 OR edition_editor.may_read = 1)
      GROUP BY edition.edition_id) AS ed2 ON ed1.manuscript_id = ed2.manuscript_id
JOIN edition_editor ON edition_editor.edition_id = ed2.edition_id
JOIN user ON user.user_id = edition_editor.user_id
LEFT JOIN edition_editor AS current_editor ON current_editor.edition_id = ed2.edition_id
    $CurrentEditorFilter
LEFT JOIN user AS current_editor_details ON current_editor_details.user_id = current_editor.user_id
JOIN manuscript_data_owner ON manuscript_data_owner.edition_id = ed2.edition_id
JOIN manuscript_data USING(manuscript_data_id)
LEFT JOIN (SELECT edition_id, MAX(time) AS last_edit 
           FROM edition_editor
           JOIN main_action USING(edition_id) 
           GROUP BY edition_id) AS last ON last.edition_id = ed2.edition_id
LEFT JOIN (SELECT iaa_edition_catalog.manuscript_id, MIN(CONCAT(proxy, url, SQE_image.filename)) AS thumbnail_url 
		   FROM edition
		   JOIN iaa_edition_catalog USING(manuscript_id)
           JOIN image_to_iaa_edition_catalog USING (iaa_edition_catalog_id)
		   JOIN SQE_image ON SQE_image.image_catalog_id = image_to_iaa_edition_catalog.image_catalog_id AND SQE_image.type = 0 
           JOIN image_urls USING(image_urls_id)
           WHERE iaa_edition_catalog.edition_side = 0
           GROUP BY manuscript_id) AS im ON im.manuscript_id = ed2.manuscript_id
$Where
GROUP BY ed2.edition_id
";

        public static string GetQuery(bool limitUser, bool limitScrolls)
        {
            // Build the WHERE clauses
            var where = limitScrolls ? "WHERE ed1.edition_id = @EditionId" : "";
            var userFilter = limitUser ? "OR edition_editor.user_id = @UserId" : "";
            var currentEditorFilter = limitUser ? "AND current_editor.user_id = @UserId" : "";


            return _baseQuery.Replace("$Where", where)
                .Replace("$UserFilter", userFilter)
                .Replace("$CurrentEditorFilter", currentEditorFilter);
        }


        internal class Result
        {
            public uint EditionId { get; set; }
            public bool Admin { get; set; }
            public string Name { get; set; }
            public uint EditionDataEditorId { get; set; }
            public string ScrollId { get; set; }
            public string Thumbnail { get; set; }
            public bool Locked { get; set; }
            public bool MayLock { get; set; }
            public bool MayWrite { get; set; }
            public bool MayRead { get; set; }
            public DateTime? LastEdit { get; set; }
            public uint UserId { get; set; }
            public string Email { get; set; }
            public string Collaborators { get; set; }
            public string CopyrightHolder { get; set; }
            public bool IsPublic { get; set; }
        }
    }

    internal class EditionNameQuery
    {
        private const string _baseQuery = @"
SELECT manuscript_data_id AS ScrollDataId, manuscript_id AS ScrollId, name AS Name
FROM manuscript_data
JOIN manuscript_data_owner USING(manuscript_data_id)
WHERE edition_id = @EditionId";

        public static string GetQuery()
        {
            return _baseQuery;
        }

        internal class Result
        {
            public uint ScrollDataId { get; set; }
            public uint ScrollId { get; set; }
            public string Name { get; set; }
        }
    }

    internal static class EditionLockQuery
    {
        public const string GetQuery = @"
SELECT locked AS Locked
FROM edition_editor
JOIN edition USING(edition_id)
WHERE edition_id = @EditionId";

        internal class Result
        {
            public bool Locked { get; set; } // locked is TINYINT, which is 8-bit unsigned like C# bool.  Is it ok/safe?
        }
    }

    // TODO: probably delete this.
    internal static class ScrollVersionGroupLimitQuery
    {
        private const string DefaultLimit = " sv1.user_id = 1 ";

        private const string UserLimit = " sv1.user_id = @UserId ";

        private const string CoalesceScrollVersions = @"scroll_version_id IN 
            (SELECT sv2.scroll_version_id
            FROM scroll_version sv1
            JOIN scroll_version_group USING(edition_id)
            JOIN scroll_version sv2 ON sv2.edition_id = scroll_version_group.edition_id
            WHERE sv1.scroll_version_id = @ScrollVersionId";

        // You must add a parameter `@ScrollVersionId` to any query using this.
        public const string LimitToScrollVersionGroup = CoalesceScrollVersions + ")";

        // You must add a parameter `@ScrollVersionId` to any query using this.
        public const string LimitToScrollVersionGroupNoAuth = CoalesceScrollVersions + " AND " + DefaultLimit + ")";

        // You must add the parameters `@ScrollVersionId` and `@UserId` to any query using this.
        public const string LimitToScrollVersionGroupAndUser =
            CoalesceScrollVersions + " AND (" + DefaultLimit + " OR " + UserLimit + "))";

        public const string LimitScrollVersionGroupToDefaultUser = @"
            scroll_version.user_id = 1 ";

        public const string LimitScrollVersionGroupToUser =
            LimitScrollVersionGroupToDefaultUser + " OR scroll_version.user_id = @UserId ";
    }

    #region editor queries

    internal static class GetEditionEditorsWithPermissionsQuery
    {
        public const string GetQuery = @"
SELECT SQE.user.email AS Email, edition_editor.may_read AS MayRead, edition_editor.may_write AS MayLock, 
       edition_editor.may_lock AS MayLock, edition_editor.is_admin AS IsAdmin
FROM SQE.edition_editor
JOIN SQE.user USING(user_id)
WHERE edition_editor.edition_id = @EditionId
";
    }

    internal static class CreateEditionEditorQuery
    {
        // You must add a parameter `@UserId`, `@EditionId`, `@MayLock` (0 = false, 1 = true),
        // and `@Admin` (0 = false, 1 = true) to use this.
        public const string GetQuery = @"
INSERT INTO edition_editor (user_id, edition_id, may_write, may_lock, is_admin) 
VALUES (@UserId, @EditionId, 1, @MayLock, @IsAdmin)";
    }

    internal static class CreateDetailedEditionEditorQuery
    {
        // You must add a parameter `@UserId`, `@EditionId`, `@MayRead` (0 = false, 1 = true), `@MayWrite` (0 = false, 1 = true),
        // `@MayLock` (0 = false, 1 = true), and `@Admin` (0 = false, 1 = true) to use this.
        public const string GetQuery = @"
INSERT INTO edition_editor (user_id, edition_id, may_read, may_write, may_lock, is_admin) 
SELECT user_id, @EditionId, @MayRead, @MayWrite, @MayLock, @IsAdmin
FROM SQE.user
WHERE SQE.user.email = @Email
";
    }

    internal static class UpdateEditionEditorPermissionsQuery
    {
        // You must add a parameter `@UserId`, `@EditionId`, `@MayRead` (0 = false, 1 = true), `@MayWrite` (0 = false, 1 = true),
        // `@MayLock` (0 = false, 1 = true), and `@Admin` (0 = false, 1 = true) to use this.
        public const string GetQuery = @"
UPDATE edition_editor
JOIN user ON user.user_id = edition_editor.user_id 
    AND user.email = @Email 
SET may_read = @MayRead,
    may_write = @MayWrite,
    may_lock = @MayLock,
    is_admin = @IsAdmin 
WHERE edition_editor.edition_id = @EditionId
";
    }

    #endregion editor queries

    internal static class CopyEditionQuery
    {
        // You must add the parameter `@EditionId` to use this, the parameters `@CopyrightHolder`, `@Collaborators` are optional.
        public const string GetQuery =
            @"INSERT INTO edition (manuscript_id, locked, copyright_holder, collaborators)  
            (SELECT manuscript_id, 0, COALESCE(@CopyrightHolder, copyright_holder), @Collaborators
            FROM edition
            WHERE edition_id = @EditionId)";
    }

    internal static class CopyEditionDataForTableQuery
    {
        // You must add a parameter `@ScrollVersionId` and `@CopyToScrollVersionId` to use this.
        public static string GetQuery(string tableName, string tableIdColumn)
        {
            return $@"INSERT IGNORE INTO {tableName} ({tableIdColumn}, edition_editor_id, edition_id) 
            SELECT {tableIdColumn}, @EditionEditorId, @CopyToEditionId 
            FROM {tableName} 
            WHERE edition_id = @EditionId";
        }
    }

    internal static class GetOwnerTableDataForQuery
    {
        // You must add a parameter `@EditionId`.
        public static string GetQuery(string tableName, string tableIdColumn)
        {
            return $@"SELECT {tableIdColumn} 
            FROM {tableName} 
            WHERE edition_id = @EditionId";
        }
    }

    internal static class WriteOwnerTableData
    {
        public static string GetQuery(string tableName,
            string tableIdColumn,
            uint editionId,
            uint editionEditorId,
            List<uint> dataIds)
        {
            return $@"INSERT INTO {tableName} (edition_id, edition_editor_id, {tableIdColumn})
            VALUES {string.Join(
                    ",",
                    dataIds.Select(x => $"({editionId},{editionEditorId},{x.ToString()})"))
                }";
        }
    }

    internal static class UpdateEditionLegalDetailsQuery
    {
        // You must add the parameter `@EditionId` and `@Collaborators` to use this, the parameter `@CopyrightHolder` is optional.
        public const string GetQuery = @"
UPDATE edition 
SET copyright_holder = COALESCE(@CopyrightHolder, copyright_holder), 
    collaborators = @Collaborators 
WHERE edition_id = @EditionId";
    }

    /// <summary>
    ///     Delete all entries for a specific edition from the specified table.
    ///     We ensure here that the user requesting this is indeed an admin (even though that should also have been
    ///     done in API logic elsewhere).
    /// </summary>
    internal static class DeleteEditionFromTable
    {
        private const string _sql = @"
DELETE $Table
FROM $Table
JOIN edition_editor ON edition_editor.edition_id = @EditionId 
  AND edition_editor.user_id = @UserId
WHERE $Table.edition_id = @EditionId AND edition_editor.is_admin = 1
";

        public static string GetQuery(string table)
        {
            return _sql.Replace("$Table", table);
        }
    }

    internal static class LockEditionQuery
    {
        internal const string GetQuery = @"
UPDATE edition
SET locked = 1
WHERE edition_id = @EditionId
";
    }

    internal static class UnlockEditionQuery
    {
        internal const string GetQuery = @"
UPDATE edition
SET locked = 0
WHERE edition_id = @EditionId
";
    }

    internal static class EditionScriptQuery
    {
        internal const string GetQuery = @"
SELECT sign_interpretation_roi.sign_interpretation_id AS Id,
    sign_interpretation.character AS Letter,
    AsWKB(roi_shape.path) AS Polygon,
    roi_position.translate_x AS TranslateX,
    roi_position.translate_y AS TranslateY,
    roi_position.stance_rotation AS LetterRotation,
    artefact_position.rotate AS ImageRotation,
    CONCAT(image_urls.proxy, image_urls.url, SQE_image.filename) AS ImageURL,
    image_urls.suffix AS ImageSuffix
FROM sign_interpretation_roi_owner
JOIN sign_interpretation_roi USING(sign_interpretation_roi_id)
JOIN roi_position USING(roi_position_id)
JOIN roi_shape USING(roi_shape_id)
JOIN artefact_position USING(artefact_id)
JOIN artefact_position_owner ON artefact_position_owner.artefact_position_id = artefact_position.artefact_position_id
    AND artefact_position_owner.edition_id = sign_interpretation_roi_owner.edition_id
JOIN artefact_shape USING(artefact_id)
JOIN artefact_shape_owner ON artefact_shape_owner.artefact_shape_id = artefact_shape.artefact_shape_id
    AND artefact_shape_owner.edition_id = sign_interpretation_roi_owner.edition_id
JOIN SQE_image USING(sqe_image_id)
JOIN image_urls USING(image_urls_id)
JOIN sign_interpretation ON sign_interpretation.sign_interpretation_id = sign_interpretation_roi.sign_interpretation_id
JOIN position_in_stream ON position_in_stream.sign_interpretation_id = sign_interpretation_roi.sign_interpretation_id
JOIN position_in_stream_owner ON position_in_stream_owner.position_in_stream_id = position_in_stream.position_in_stream_id
    AND position_in_stream_owner.edition_id = sign_interpretation_roi_owner.edition_id
JOIN edition ON edition.edition_id = sign_interpretation_roi_owner.edition_id
JOIN edition_editor ON edition_editor.edition_id = sign_interpretation_roi_owner.edition_id

WHERE sign_interpretation_roi_owner.edition_id = @EditionId
    AND (edition.public OR edition_editor.user_id = @UserId)
ORDER BY sign_interpretation_roi.sign_interpretation_id
";
    }

    internal static class EditionEditorUserIds
    {
        internal const string GetQuery = @"
SELECT user_id
FROM (  SELECT edition_id 
        FROM SQE.edition_editor
        WHERE edition_id = @EditionId AND user_id = @UserId
    ) as valid_user
JOIN SQE.edition_editor USING(edition_id)
";
    }

    internal static class RecordEditionEditorRequest
    {
        internal const string GetQuery = @"
INSERT INTO edition_editor_request (
                                        token, 
                                        admin_user_id, 
                                        editor_user_id, 
                                        edition_id, 
                                        is_admin, 
                                        may_lock, 
                                        may_write
                                    )
               
VALUES (
            @Token, 
            @AdminUserId, 
            @EditorUserId, 
            @EditionId, 
            @IsAdmin, 
            @MayLock, 
            @MayWrite
        )

ON DUPLICATE KEY 
    UPDATE     is_admin = @IsAdmin, 
               may_lock = @MayLock, 
               may_write = @MayWrite, 
               date = CURRENT_DATE()
";
    }

    internal static class FindEditionEditorRequestByToken
    {
        internal const string GetQuery = @"
SELECT  edition_editor_request.edition_id AS EditionId, 
        edition_editor_request.is_admin AS IsAdmin, 
        edition_editor_request.may_lock AS MayLock, 
        edition_editor_request.may_write AS MayWrite,
        user.email AS Email
FROM edition_editor_request
    JOIN user ON user.user_id = edition_editor_request.editor_user_id
WHERE token = @Token
    AND editor_user_id = @EditorUserId
";
    }

    internal static class FindEditionEditorRequestByEditorEdition
    {
        internal const string GetQuery = @"
SELECT  edition_editor_request.token AS Token
FROM edition_editor_request
WHERE editor_user_id = @EditorUserId
    AND edition_id = @EditionId
    AND admin_user_id = @AdminUserId
";
    }
    
    internal static class FindEditionEditorRequestByAdminId
    {
        internal const string GetQuery = @"
SELECT  edition_editor_request.edition_id AS EditionId,
        manuscript_data.name AS EditionName,
        user.email AS Email,
        user.forename AS EditorForename,
        user.surname AS EditorSurname,
        user.organization AS EditorOrganization,
        edition_editor_request.date AS Date,
        edition_editor_request.is_admin AS IsAdmin,
        edition_editor_request.may_lock AS MayLock,
        edition_editor_request.may_write AS MayRead,
        TRUE AS MayRead
FROM edition_editor_request
    JOIN user ON user.user_id = edition_editor_request.editor_user_id
    JOIN manuscript_data_owner USING(edition_id)
    JOIN manuscript_data USING(manuscript_data_id)
WHERE edition_editor_request.admin_user_id = @AdminUserId
";
    }
    
    internal static class FindEditionEditorRequestByEditorId
    {
        internal const string GetQuery = @"
SELECT  edition_editor_request.edition_id AS EditionId,
        manuscript_data.name AS EditionName,
        user.email AS Email,
        user.forename AS AdminForename,
        user.surname AS AdminSurname,
        user.organization AS AdminOrganization,
        edition_editor_request.token AS Token,
        edition_editor_request.date AS Date,
        edition_editor_request.is_admin AS IsAdmin,
        edition_editor_request.may_lock AS MayLock,
        edition_editor_request.may_write AS MayRead,
        TRUE AS MayRead
FROM edition_editor_request
    JOIN user ON user.user_id = edition_editor_request.admin_user_id
    JOIN manuscript_data_owner USING(edition_id)
    JOIN manuscript_data USING(manuscript_data_id)
WHERE edition_editor_request.editor_user_id = @EditorUserId
";
    }

    internal static class DeleteEditionEditorRequest
    {
        internal const string GetQuery = @"
DELETE FROM edition_editor_request
WHERE token = @Token AND editor_user_id = @EditorUserId
";
    }
}