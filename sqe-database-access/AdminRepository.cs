// TODO: Add all documentation

using System.Threading.Tasks;
using SQE.DatabaseAccess.Helpers;

namespace SQE.DatabaseAccess;

public interface IAdminRepository
{
	Task<bool> CheckDatabaseAccessibleAsync();
}

public class AdminRepository(IDatabaseAccessor dba) : IAdminRepository
{
	public async Task<bool> CheckDatabaseAccessibleAsync()
	{
		const int expectedOne = 1;

		var returnedOne = await dba.QuerySingleAsync<uint>(
				"SELECT @ExpectedValue;"
				, new { ExpectedValue = expectedOne });

		return returnedOne == expectedOne;
	}
}
