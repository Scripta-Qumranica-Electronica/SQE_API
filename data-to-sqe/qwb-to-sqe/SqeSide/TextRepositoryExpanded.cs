using System.Linq;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace qwb_to_sqe.Repositories
{
	public class TextRepositoryExpanded : TextRepository
	{
		private static readonly string _terminatorsQuery = @"
            SELECT pis1.sign_interpretation_id
            FROM word as w1
                JOIN position_in_stream_to_word_rel pistwr1 on w1.word_id = pistwr1.word_id
                    JOIN position_in_stream pis1 on pistwr1.position_in_stream_id = pis1.position_in_stream_id
                    LEFT JOIN position_in_stream pis2 on pis2.next_sign_interpretation_id=pis1.sign_interpretation_id
                LEFT JOIN position_in_stream_to_word_rel pistwr2 on pis2.position_in_stream_id=pistwr2.position_in_stream_id
            LEFT JOIN word w2 on w2.word_id=pistwr2.word_id
            WHERE w1.qwb_word_id=@QWBWordId
                AND pistwr2.position_in_stream_id is null
            UNION ALL
            SELECT pis1.sign_interpretation_id
            FROM word as w1
                JOIN position_in_stream_to_word_rel pistwr1 on w1.word_id = pistwr1.word_id
                    JOIN position_in_stream pis1 on pistwr1.position_in_stream_id = pis1.position_in_stream_id
                    LEFT JOIN position_in_stream pis2 on pis1.next_sign_interpretation_id=pis2.sign_interpretation_id
                LEFT JOIN position_in_stream_to_word_rel pistwr2 on pis2.position_in_stream_id=pistwr2.position_in_stream_id
            LEFT JOIN word w2 on w2.word_id=pistwr2.word_id
            WHERE w1.qwb_word_id=@QWBWordId
                AND pistwr2.position_in_stream_id is null
";

		public TextRepositoryExpanded(
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

		public TextEdition GetSQEWord(UserInfo editionUser, uint qwbWordId)
		{
			var terminators = _getWordTerminators(qwbWordId);

			return null; // _getEntityById(editionUser, terminators).Result;
		}

		private Terminators _getWordTerminators(uint qwbWordId)
		{
			using (var connection = OpenConnection())
			{
				return new Terminators(
						connection.Query<uint>(_terminatorsQuery, new { QWBWorId = qwbWordId })
								  .ToArray());
			}
		}
	}
}
