using System.Data;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;

namespace sqe_api
{
	public class ExpandedTextRepository : TextRepository
	{
		public ExpandedTextRepository(
				IConfiguration                            config
				, IDatabaseWriter                         databaseWriter
				, IAttributeRepository                    attributeRepository
				, ISignInterpretationRepository           signInterpretationRepository
				, ISignInterpretationCommentaryRepository commentaryRepository
				, IRoiRepository                          roiRepository
				, IArtefactRepository                     artefactRepository
				, ISignStreamMaterializationRepository    materializationRepository) : base(
				config
				, databaseWriter
				, attributeRepository
				, signInterpretationRepository
				, commentaryRepository
				, roiRepository
				, artefactRepository
				, materializationRepository) { }

		public IDbConnection GetConnection() => OpenConnection();
	}
}
