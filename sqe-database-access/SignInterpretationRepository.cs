using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess
{
    public interface ISignInterpretationRepository
    {
        Task<SignInterpretationData> GetSignInterpretationById(UserInfo user, uint signInterpretationId);
    }

    public class SignInterpretationRepository : DbConnectionBase, ISignInterpretationRepository
    {
        private readonly IDatabaseWriter _databaseWriter;
        private readonly IAttributeRepository _attributeRepository;
        private readonly ISignInterpretationCommentaryRepository _interpretationCommentaryRepository;
        private readonly IRoiRepository _roiRepository;

        public SignInterpretationRepository(
            IConfiguration config,
            IDatabaseWriter databaseWriter,
            IAttributeRepository attributeRepository,
            ISignInterpretationCommentaryRepository interpretationCommentaryRepository,
            IRoiRepository roiRepository) : base(config)
        {
            _databaseWriter = databaseWriter;
            _attributeRepository = attributeRepository;
            _interpretationCommentaryRepository = interpretationCommentaryRepository;
            _roiRepository = roiRepository;
        }

        public async Task<SignInterpretationData> GetSignInterpretationById(UserInfo user, uint signInterpretationId)
        {
            // We use several existing quick functions to get the specifics of a sign interpretation,
            // so wrap it in a transaction to make sure the result is consistent.
            using (var transactionScope = new TransactionScope())
            using (var conn = OpenConnection())
            {
                var attributes =
                    _attributeRepository.GetSignInterpretationAttributesByInterpretationId(user, signInterpretationId);
                var commentaries =
                    _interpretationCommentaryRepository.GetSignInterpretationCommentariesByInterpretationId(user,
                        signInterpretationId);
                var roiIds = await _roiRepository.GetSignInterpretationRoisIdsByInterpretationId(user, signInterpretationId);
                var rois = roiIds.Select(x => _roiRepository.GetSignInterpretationRoiByIdAsync(user, x));

                SignInterpretationData returnSignInterpretation = null;
                var _ = await conn.QueryAsync(SignInterpretationQuery.GetQuery,
                    new[]
                    {
                        typeof(SignInterpretationData), typeof(NextSignInterpretation), typeof(uint?)
                    },
                    objects =>
                    {
                        var signInterpretationData = objects[0] as SignInterpretationData;
                        var nextSignInterpretation = objects[1] as NextSignInterpretation;
                        var signStreamSelectionId = objects[2] as uint?;

                        // Since the Query searches for a single sign interpretation id, we only ever create a single object
                        returnSignInterpretation ??= signInterpretationData;

                        if (!returnSignInterpretation.NextSignInterpretations.Contains(nextSignInterpretation))
                            returnSignInterpretation.NextSignInterpretations.Add(nextSignInterpretation);

                        if (signStreamSelectionId.HasValue && !returnSignInterpretation.SignStreamSectionIds.Contains(signStreamSelectionId.Value))
                            returnSignInterpretation.SignStreamSectionIds.Add(signStreamSelectionId.Value);

                        return returnSignInterpretation;
                    },
                    new
                    {
                        EditionId = user.EditionId,
                        SignInterpretationId = signInterpretationId
                    },
                    splitOn:
                    "NextSignInterpretationId, SignStreamSectionId");

                returnSignInterpretation.Attributes = await attributes;
                returnSignInterpretation.Commentaries = (await commentaries).AsList();
                returnSignInterpretation.SignInterpretationRois = (await Task.WhenAll(rois)).AsList();

                return returnSignInterpretation;
            }
        }
    }
}