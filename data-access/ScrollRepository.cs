using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using SQE.Backend.DataAccess.Queries;
using System.Linq;
using SQE.Backend.DataAccess.Models;
using Microsoft.Extensions.Configuration;

namespace SQE.Backend.DataAccess
{
    public interface IScrollRepository
    {
        Task<IEnumerable<ScrollVersion>> ListScrollVersions(int? userId, List<int> scrollIds);
    }
  
    public class ScrollRepository : DBConnectionBase, IScrollRepository
    {
        public ScrollRepository(IConfiguration config) : base(config) { }


        public async Task<IEnumerable<ScrollVersion>> ListScrollVersions(int? userId, List<int> ids) //
        {
            return null;
            /*
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
            } */
        }
    }
}

