using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;

namespace SQE.SqeHttpApi.DataAccess
{
	public interface IRoiRepository
	{
		Task<List<SignInterpretationROI>> CreateRoisAsync(EditionUserInfo editionUser,
			List<SetSignInterpretationROI> newRois);
	}

	public class RoiRepository : DbConnectionBase, IRoiRepository
	{
		private readonly IDatabaseWriter _databaseWriter;

		public RoiRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
		{
			_databaseWriter = databaseWriter;
		}

		public async Task<List<SignInterpretationROI>> CreateRoisAsync(EditionUserInfo editionUser,
			List<SetSignInterpretationROI> newRois)
		{
			return null;
		}

		#region Private methods

		#endregion Private methods
	}
}