using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using SQE.Backend.DataAccess.RawModels;
using System.Linq;
using SQE.Backend.DataAccess.Models;
using Microsoft.Extensions.Configuration;

namespace SQE.Backend.DataAccess
{
    public interface IScrollRepository
    {
        Task<IEnumerable<Artefact>> ListArtefactsAsync(int masterVersionId);
        Task<IEnumerable<ScrollVersion>> scrollVersion(int userId, List<int> scrollIds);
    }

    public class ScrollRepository : DBConnectionBase, IScrollRepository
    {
        public ScrollRepository(IConfiguration config) : base(config) { }

        // Task<IEnumerable<ScrollVersion>> GetScrollVersions(List<int> ids = null) IN @ids (@ids = string.join(',', ids.toStirng)
        public async Task<IEnumerable<Artefact>> ListArtefactsAsync(int masterVersionId)
        {
            var sql = @"
select artefact_data.artefact_id As id,
artefact_data.name AS name,
artefact_data_owner.scroll_version_id As scroll_version_id,
artefact_position.transform_matrix As transform_matrix,
artefact_position.z_index AS zOrder
from artefact_data inner join artefact_data_owner 
on artefact_data.artefact_data_id = artefact_data_owner.artefact_data_id 
and artefact_data_owner.scroll_version_id =@ScrollVersionId 
inner join SQE_DEV.artefact_position on artefact_data.artefact_data_id = artefact_position.artefact_id
";
            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ArtefactResponse>(sql, new
                {
                    ScrollVersionId = masterVersionId
                });
                return results.Select(raw => raw.CreateModel());
            }
        }



        public async Task<IEnumerable<ScrollVersion>> scrollVersion(int userId, List<int> ids) //
        {
            string scrollIds = string.Join(",", ids);

            var sql = @"SELECT 
image_urls.url AS thumbnails,
scroll_data.name as name,
scroll_version.scroll_version_id  As id,
scroll_version_group.locked As locked,
user.user_name as user_name,
user.user_id as user_id
from scroll_data
LEFT JOIN edition_catalog ON edition_catalog.scroll_id = scroll_data.scroll_id AND edition_catalog.edition_side = 0
LEFT JOIN image_to_edition_catalog USING(edition_catalog_id)
LEFT JOIN SQE_image ON SQE_image.image_catalog_id = image_to_edition_catalog.image_catalog_id AND SQE_image.type = 0 
LEFT JOIN image_urls USING(image_urls_id)
join scroll_version on scroll_version.scroll_version_id = scroll_data.scroll_id
join scroll_version_group using (scroll_version_group_id)
join user using (user_id)
WHERE (scroll_version.user_id =((SELECT user_id FROM user WHERE user_name = ""sqe_api"") or scroll_version.user_id = @UserId)
and scroll_version.scroll_version_id IN (@ScrollIds))
group by scroll_version.scroll_version_id;";
            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ScrollVersionsQueryResponse>(sql, new
                {
                    UserId = userId,
                    ScrollIds = scrollIds,
                });
                return results.Select(raw =>raw.CreateModel());
            }
        }

  

        private ScrollVersion SetPermissions(ScrollVersion sv, int userId)
        {
            // Set permissions based on the sharing info and userid

            var shareInfo = sv.Sharing.FirstOrDefault(si => si.User.UserId == userId);
            if (shareInfo != null)
                sv.Permission = shareInfo.Permission;
            else
                sv.Permission = new Permission {CanRead = false, CanWrite = false };
            return sv;
        }


    }
}

