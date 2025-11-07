using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using SQE.API.DTO;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.DatabaseAccess;

public interface IArtefactRepository
{
	Task<ArtefactModel> GetEditionArtefactAsync(
			UserInfo editionUser
			, uint   artefactId
			, bool   withMask = false);

	Task<IEnumerable<ArtefactModel>> GetEditionArtefactListAsync(
			UserInfo editionUser
			, bool   withMask = false);

	Task<List<AlteredRecord>> UpdateArtefactAllAsync(
			UserInfo        editionUser
			, uint          artefactId
			, string        shape
			, uint?         masterImageId = null
			, string        workStatus    = null
			, string        name          = null
			, decimal?      scale         = null
			, decimal?      rotate        = null
			, int?          translateX    = null
			, int?          translateY    = null
			, int?          zIndex        = null
			, bool?         mirrored      = null
			, IDbConnection connection    = null);

	Task<List<AlteredRecord>> UpdateArtefactShapeAsync(
			UserInfo        editionUser
			, uint          artefactId
			, string        shape
			, uint?         masterImageId = null
			, IDbConnection connection    = null);

	Task<List<AlteredRecord>> UpdateArtefactStatusAsync(
			UserInfo editionUser
			, uint   artefactId
			, string workStatus);

	Task<List<AlteredRecord>> UpdateArtefactNameAsync(
			UserInfo editionUser
			, uint   artefactId
			, string name);

	Task<List<AlteredRecord>> UpdateArtefactPositionAsync(
			UserInfo   editionUser
			, uint     artefactId
			, decimal? scale
			, decimal? rotate
			, int?     translateX
			, int?     translateY
			, int?     zIndex
			, bool     mirrored);

	Task<List<AlteredRecord>> BatchUpdateArtefactPositionAsync(
			UserInfo                           editionUser
			, List<UpdateArtefactPlacementDTO> transforms
			, IDbConnection                    connection = null);

	Task<uint> CreateNewArtefactAsync(
			UserInfo   editionUser
			, uint?    masterImageId
			, string   shape
			, string   artefactName
			, decimal? scale
			, decimal? rotate
			, int?     translateX
			, int?     translateY
			, int?     zIndex
			, string   workStatus
			, bool     mirrored);

	Task DeleteArtefactAsync(UserInfo editionUser, uint artefactId);

	Task<List<TextFragmentData>> ArtefactTextFragmentsAsync(UserInfo editionUser, uint artefactId);

	Task<List<TextFragmentData>> ArtefactSuggestedTextFragmentsAsync(
			UserInfo editionUser
			, uint   artefactId);

	Task<List<ArtefactGroup>> ArtefactGroupsOfEditionAsync(UserInfo editionUser);

	Task<ArtefactGroup> GetArtefactGroupAsync(UserInfo editionUser, uint artefactGroupId);

	Task<ArtefactGroup> CreateArtefactGroupAsync(
			UserInfo     editionUser
			, string     artefactGroupName
			, List<uint> artefactIds);

	Task<ArtefactGroup> UpdateArtefactGroupAsync(
			UserInfo     editionUser
			, uint       artefactGroupId
			, string     artefactGroupName
			, List<uint> artefactIds);

	Task DeleteArtefactGroupAsync(UserInfo editionUser, uint artefactGroupId);
}

public class ArtefactRepository(IDatabaseAccessor adb) : IArtefactRepository
{
	public async Task<ArtefactModel> GetEditionArtefactAsync(
			UserInfo editionUser
			, uint   artefactId
			, bool   withMask = false)
	{
		var artefacts = (await adb.QueryAsync<ArtefactModel>(
				ArtefactOfEditionQuery.GetQuery(editionUser.userId, withMask)
				, new
				{
						editionUser.EditionId
						, UserId = editionUser.userId
						, ArtefactId = artefactId
						,
				})).ToList();

		if (!artefacts.Any())
		{
			throw new StandardExceptions.DataNotFoundException(
					"artefact"
					, artefactId
					, "artefact_id");
		}

		return artefacts.First();
	}

	/// <summary>
	///  Returns artefact details for all artefacts belonging to the specified edition.
	/// </summary>
	/// <param name="editionUser"></param>
	/// <param name="withMask">Optionally include the mask data for the artefacts</param>
	/// <returns></returns>
	public async Task<IEnumerable<ArtefactModel>> GetEditionArtefactListAsync(
			UserInfo editionUser
			, bool   withMask = false) => await adb.QueryAsync<ArtefactModel>(
			ArtefactsOfEditionQuery.GetQuery(editionUser.userId, withMask)
			, new
			{
					editionUser.EditionId
					, UserId = editionUser.userId
					,
			});

	public async Task<List<AlteredRecord>> UpdateArtefactAllAsync(
			UserInfo        editionUser
			, uint          artefactId
			, string        shape
			, uint?         masterImageId = null
			, string        workStatus    = null
			, string        name          = null
			, decimal?      scale         = null
			, decimal?      rotate        = null
			, int?          translateX    = null
			, int?          translateY    = null
			, int?          zIndex        = null
			, bool?         mirrored      = null
			, IDbConnection connection    = null)
	{
		var tasks = new List<AlteredRecord>();

		if (!string.IsNullOrEmpty(shape))
		{
			tasks.AddRange(await UpdateArtefactShapeAsync(editionUser, artefactId, shape));
		}

		if (!string.IsNullOrEmpty(name))
		{
			tasks.AddRange(await UpdateArtefactNameAsync(editionUser, artefactId, name));
		}

		if (scale != null
			|| rotate != null
			|| translateX != null
			|| translateY != null
			|| zIndex != null)
		{
			tasks.AddRange(
					await UpdateArtefactPositionAsync(
							editionUser
							, artefactId
							, scale
							, rotate
							, translateX
							, translateY
							, zIndex
							, mirrored ?? false));
		}

		if (!string.IsNullOrEmpty(workStatus))
		{
			tasks.AddRange(await UpdateArtefactStatusAsync(editionUser, artefactId, workStatus));
		}

		adb.CommitTransaction();

		return tasks;
	}

	public async Task<List<AlteredRecord>> UpdateArtefactShapeAsync(
			UserInfo        editionUser
			, uint          artefactId
			, string        shape
			, uint?         masterImageId = null
			, IDbConnection connection    = null)
	{
		/* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
write the value directly into the database, but it does not seem to work right yet.  Thus we currently
use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().

var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
var res = string.Join("", binaryMask);
var Mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
		const string tableName = "artefact_shape";

		var artefactShapeId = await GetArtefactPkAsync(editionUser, artefactId, tableName);

		if (artefactShapeId == 0)
		{
			if (!masterImageId.HasValue)
				throw new StandardExceptions.ImproperInputDataException("artefact shape");

			return await InsertArtefactShapeAsync(
					editionUser
					, artefactId
					, masterImageId.Value
					, shape);
		}

		var sqeImageId = await GetArtefactShapeSqeImageIdAsync(editionUser, artefactId);

		var artefactChangeParams = new DynamicParameters();
		artefactChangeParams.Add("@region_in_sqe_image", shape);
		artefactChangeParams.Add("@artefact_id", artefactId);
		artefactChangeParams.Add("@sqe_image_id", sqeImageId);

		var artefactChangeRequest = new MutationRequest(
				MutateType.Update
				, artefactChangeParams
				, tableName
				, artefactShapeId);

		try
		{
			return await WriteArtefactAsync(editionUser, artefactChangeRequest);
		}
		catch (MySqlException e)
		{
			// Capture any errors caused by improperly formatted WKT shapes, which become null in this query.
			if (e.Message.IndexOf(
						"Column 'region_in_sqe_image' cannot be null"
						, StringComparison.Ordinal)
				> -1)
				throw new StandardExceptions.ImproperInputDataException("mask");

			throw;
		}
	}

	public async Task<List<AlteredRecord>> UpdateArtefactStatusAsync(
			UserInfo editionUser
			, uint   artefactId
			, string workStatus)
	{
		const string tableName = "artefact_status";

		var artefactStatusId = await GetArtefactPkAsync(editionUser, artefactId, tableName);

		if (artefactStatusId == 0)
			return await InsertArtefactStatusAsync(editionUser, artefactId, workStatus);

		var artefactChangeParams = new DynamicParameters();
		artefactChangeParams.Add("@artefact_id", artefactId);

		if (!string.IsNullOrEmpty(workStatus))
			artefactChangeParams.Add("@work_status_id", await SetWorkStatusAsync(workStatus));

		var artefactChangeRequest = new MutationRequest(
				MutateType.Update
				, artefactChangeParams
				, tableName
				, artefactStatusId);

		return await WriteArtefactAsync(editionUser, artefactChangeRequest);
	}

	public async Task<List<AlteredRecord>> UpdateArtefactNameAsync(
			UserInfo editionUser
			, uint   artefactId
			, string name)
	{
		const string tableName = "artefact_data";

		var artefactDataId = await GetArtefactPkAsync(editionUser, artefactId, tableName);

		if (artefactDataId == 0)
		{
			throw new StandardExceptions.DataNotFoundException(
					"artefact name"
					, artefactId
					, "artefact_id");
		}

		var artefactChangeParams = new DynamicParameters();
		artefactChangeParams.Add("@Name", name);
		artefactChangeParams.Add("@artefact_id", artefactId);

		var artefactChangeRequest = new MutationRequest(
				MutateType.Update
				, artefactChangeParams
				, tableName
				, artefactDataId);

		return await WriteArtefactAsync(editionUser, artefactChangeRequest);
	}

	public async Task<List<AlteredRecord>> BatchUpdateArtefactPositionAsync(
			UserInfo                           editionUser
			, List<UpdateArtefactPlacementDTO> transforms
			, IDbConnection                    connection = null)
	{
		List<AlteredRecord> updates;

		var updateMutations = new MutationRequest[transforms.Count];

		foreach (var (transform, index) in transforms.Select((x, idx) => (x, idx)))
		{
			updateMutations[index] = await FormatArtefactPositionUpdateRequestAsync(
					editionUser
					, transform.artefactId
					, transform.placement?.scale
					, transform.placement?.rotate
					, transform.isPlaced
							? transform.placement?.translate?.x
							: null
					, // set to null if explicitly not placed
					transform.isPlaced
							? transform.placement?.translate?.y
							: null
					, // set to null if explicitly not placed
					transform.placement?.zIndex
					, transform.placement?.mirrored ?? false);
		}

		updates = await adb.WriteToDatabaseAsync(editionUser, updateMutations.AsList());

		adb.CommitTransaction();

		return updates;
	}

	public async Task<List<AlteredRecord>> UpdateArtefactPositionAsync(
			UserInfo   editionUser
			, uint     artefactId
			, decimal? scale
			, decimal? rotate
			, int?     translateX
			, int?     translateY
			, int?     zIndex
			, bool     mirrored) => await WriteArtefactAsync(
			editionUser
			, await FormatArtefactPositionUpdateRequestAsync(
					editionUser
					, artefactId
					, scale
					, rotate
					, translateX
					, translateY
					, zIndex
					, mirrored));

	public async Task<uint> CreateNewArtefactAsync(
			UserInfo   editionUser
			, uint?    masterImageId
			, string   shape
			, string   artefactName
			, decimal? scale
			, decimal? rotate
			, int?     translateX
			, int?     translateY
			, int?     zIndex
			, string   workStatus
			, bool     mirrored)
	{
		/* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
write the value directly into the database, but it does not seem to work right yet.  Thus we currently
use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().

var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
var res = string.Join("", binaryMask);
var Mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
		await adb.BeginTransactionAsync();

		// TODO make sure the imaged object is part of the edition already (add it automatically)
		// Create a new artefact
		await adb.ExecuteAsync("INSERT INTO artefact (artefact_id) VALUES(NULL)");

		var artefactId = await adb.QuerySingleAsync<uint>(LastInsertId.GetQuery);

		if (artefactId == 0)
		{
			throw new StandardExceptions.DataNotWrittenException("create artefact");
		}

		if (!string.IsNullOrEmpty(shape))
		{
			await InsertArtefactShapeAsync(
					editionUser
					, artefactId
					, masterImageId
					, shape);
		}

		await InsertArtefactStatusAsync(editionUser, artefactId, workStatus);

		await InsertArtefactNameAsync(editionUser, artefactId, artefactName ?? "");

		if (scale.HasValue
			|| rotate.HasValue
			|| translateX.HasValue
			|| translateY.HasValue
			|| zIndex.HasValue)
		{
			await InsertArtefactPositionAsync(
					editionUser
					, artefactId
					, scale
					, rotate
					, translateX
					, translateY
					, zIndex
					, mirrored);
		}

		//Cleanup
		adb.CommitTransaction();

		return artefactId;
	}

	public async Task DeleteArtefactAsync(UserInfo editionUser, uint artefactId)
	{
		// First ensure that the artefact has no related ROIs
		const string roiCheckSql = @"SELECT sign_interpretation_roi.roi_position_id
FROM roi_position
JOIN sign_interpretation_roi USING(roi_position_id)
JOIN sign_interpretation_roi_owner USING(sign_interpretation_roi_id)
WHERE artefact_id = @ArtefactId
AND edition_id = @EditionId";

		var rois = await adb.QueryAsync<uint>(
				roiCheckSql
				, new { ArtefactId = artefactId, editionUser.EditionId });

		if (rois.Count() > 0)
		{
			throw new StandardExceptions.InputDataRuleViolationException(
					"must delete all ROIs for an artefact before deleting the artefact");
		}

		// Begin building the delete request
		var mutations = new List<MutationRequest>();

		foreach (var table in ArtefactTableNames.All())
		{
			if (table != ArtefactTableNames.Stack)
			{
				var pk = await GetArtefactPkAsync(editionUser, artefactId, table);

				if (pk != 0)
				{
					mutations.Add(
							new MutationRequest(
									MutateType.Delete
									, new DynamicParameters()
									, table
									, pk));
				}
			}
			else
			{
				var pks = await GetArtefactStackPksAsync(editionUser, artefactId, table);

				mutations.AddRange(
						pks.Select(pk => new MutationRequest(
										   MutateType.Delete
										   , new DynamicParameters()
										   , table
										   , pk)));
			}
		}

		var _ = await adb.WriteToDatabaseAsync(editionUser, mutations);
		adb.CommitTransaction();
	}

	public async Task<List<TextFragmentData>> ArtefactTextFragmentsAsync(
			UserInfo editionUser
			, uint   artefactId) => (await adb.QueryAsync<TextFragmentData>(
			FindArtefactTextFragments.GetQuery
			, new
			{
					editionUser.EditionId
					, UserId = editionUser.userId
					, ArtefactId = artefactId
					,
			})).ToList();

	public async Task<List<TextFragmentData>> ArtefactSuggestedTextFragmentsAsync(
			UserInfo editionUser
			, uint   artefactId) => (await adb.QueryAsync<TextFragmentData>(
			FindSuggestedArtefactTextFragments.GetQuery
			, new
			{
					editionUser.EditionId
					, UserId = editionUser.userId
					, ArtefactId = artefactId
					,
			})).ToList();

	public async Task<List<ArtefactGroup>> ArtefactGroupsOfEditionAsync(UserInfo editionUser)
	{
		// The query gets a table with rows ArtefactGroupId, ArtefactGroupName, and ArtefactId
		// for every artefact in a group.
		return (await adb.QueryAsync<ArtefactGroupEntry>(
					   FindArtefactGroups.GetQuery
					   , new { editionUser.EditionId }))

			   // Group the results according to the artefact group id and name as keys,
			   // and the list of ArtefactId is the val
			   .GroupBy(
					   x => new
					   {
							   x.ArtefactGroupId
							   , ArtefactName = x.ArtefactGroupName
							   ,
					   }
					   , // this is the key
					   x => x.ArtefactId
					   ,                               // this is the val
					   (key, val) => new ArtefactGroup // the return object
					   {
							   ArtefactGroupId = key.ArtefactGroupId
							   , ArtefactName = key.ArtefactName
							   , ArtefactIds = val.ToList()
							   ,
					   })
			   .ToList();
	}

	public async Task<ArtefactGroup> GetArtefactGroupAsync(
			UserInfo editionUser
			, uint   artefactGroupId)
	{
		// The query gets a table with rows ArtefactGroupId, ArtefactGroupName, and ArtefactId
		// for every artefact in a group.
		return (await adb.QueryAsync<ArtefactGroupEntry>(
						FindArtefactGroup.GetQuery
						, new
						{
								editionUser.EditionId
								, ArtefactGroupId = artefactGroupId
								,
						})).GroupBy(
								   x => new
								   {
										   x.ArtefactGroupId
										   , ArtefactName = x.ArtefactGroupName
										   ,
								   }
								   , // this is the key
								   x => x.ArtefactId
								   ,                               // this is the val
								   (key, val) => new ArtefactGroup // the return object
								   {
										   ArtefactGroupId = key.ArtefactGroupId
										   , ArtefactName = key.ArtefactName
										   , ArtefactIds = val.ToList()
										   ,
								   })
						   .FirstOrDefault();
	}

	public async Task<ArtefactGroup> CreateArtefactGroupAsync(
			UserInfo     editionUser
			, string     artefactGroupName
			, List<uint> artefactIds)
	{
		uint artefactGroupId;

		// Check if the requested artefact IDs are already part of a group.
		await _verifyArtefactsFreeForGroup(editionUser, artefactIds.ToList());

		await adb.BeginTransactionAsync();

		// Create a new artefact group
		var insertedArtefactGroup = await adb.ExecuteAsync(
				"INSERT INTO artefact_group (artefact_group_id) VALUES(NULL)");

		if (insertedArtefactGroup != 1)
			throw new StandardExceptions.DataNotWrittenException("create artefact group");

		artefactGroupId = await adb.QuerySingleAsync<uint>(LastInsertId.GetQuery);

		if (artefactGroupId == 0)
			throw new StandardExceptions.DataNotWrittenException("create artefact group");

		var createArtefactGroupInserts = artefactIds.Select(artefactId =>
															{
																var artefactInGroupParams =
																		new DynamicParameters();

																artefactInGroupParams.Add(
																		"@artefact_id"
																		, artefactId);

																artefactInGroupParams.Add(
																		"@artefact_group_id"
																		, artefactGroupId);

																return new MutationRequest(
																		MutateType.Create
																		, artefactInGroupParams
																		, "artefact_group_member");
															})
													.AsList();

		if (!string.IsNullOrEmpty(artefactGroupName))
		{
			var artefactGroupNameParams = new DynamicParameters();
			artefactGroupNameParams.Add("@name", artefactGroupName);

			artefactGroupNameParams.Add("@artefact_group_id", artefactGroupId);

			createArtefactGroupInserts.Add(
					new MutationRequest(
							MutateType.Create
							, artefactGroupNameParams
							, "artefact_group_data"));
		}

		var responses = await adb.WriteToDatabaseAsync(editionUser, createArtefactGroupInserts);

		if (responses.Count != createArtefactGroupInserts.Count())
			throw new StandardExceptions.DataNotWrittenException("create a new artefact group");

		adb.CommitTransaction();

		// TODO: Consider returning return a manufactured object based on the method parameters without
		// making a database query?
		return await GetArtefactGroupAsync(editionUser, artefactGroupId);
	}

	public async Task<ArtefactGroup> UpdateArtefactGroupAsync(
			UserInfo     editionUser
			, uint       artefactGroupId
			, string     artefactGroupName
			, List<uint> artefactIds)
	{
		// Get group members and name (if any)
		var (members, groupData) =
				await _getArtefactGroupInternalInfo(editionUser, artefactGroupId);

		if (!members.Any())
		{
			throw new StandardExceptions.DataNotFoundException(
					"artefact group id"
					, artefactGroupId);
		}

		var hashedUpdateArtefactIds = new HashSet<uint>(artefactIds);

		var deletes = members.Where(x => !hashedUpdateArtefactIds.Contains(x.ArtefactId));

		var adds = artefactIds.Except(members.Select(x => x.ArtefactId)).ToList();

		// Check if the requested artefact IDs to be added are already part of a group.
		if (adds.Any())
			await _verifyArtefactsFreeForGroup(editionUser, adds.ToList());

		var alterations = deletes.Select(x =>
										 {
											 var artefactInGroupParams = new DynamicParameters();

											 artefactInGroupParams.Add(
													 "@artefact_id"
													 , x.ArtefactId);

											 artefactInGroupParams.Add(
													 "@artefact_group_id"
													 , artefactGroupId);

											 return new MutationRequest(
													 MutateType.Delete
													 , artefactInGroupParams
													 , "artefact_group_member"
													 , x.ArtefactGroupMemberId);
										 })
								 .Concat(
										 adds.Select(x =>
													 {
														 var artefactInGroupParams =
																 new DynamicParameters();

														 artefactInGroupParams.Add(
																 "@artefact_id"
																 , x);

														 artefactInGroupParams.Add(
																 "@artefact_group_id"
																 , artefactGroupId);

														 return new MutationRequest(
																 MutateType.Create
																 , artefactInGroupParams
																 , "artefact_group_member");
													 }))
								 .AsList();

		if (!string.IsNullOrEmpty(artefactGroupName))
		{
			if (groupData == null)
			{
				var artefactGroupNameParams = new DynamicParameters();
				artefactGroupNameParams.Add("@name", artefactGroupName);

				artefactGroupNameParams.Add("@artefact_group_id", artefactGroupId);

				alterations.Add(
						new MutationRequest(
								MutateType.Create
								, artefactGroupNameParams
								, "artefact_group_data"));
			}
			else
			{
				var artefactGroupNameParams = new DynamicParameters();
				artefactGroupNameParams.Add("@name", artefactGroupName);

				artefactGroupNameParams.Add("@artefact_group_id", artefactGroupId);

				alterations.Add(
						new MutationRequest(
								MutateType.Update
								, artefactGroupNameParams
								, "artefact_group_data"
								, groupData.ArtefactGroupDataId));
			}
		}

		var responses = await adb.WriteToDatabaseAsync(editionUser, alterations);

		if (responses.Count != alterations.Count)
			throw new StandardExceptions.DataNotWrittenException("update an artefact group");

		adb.CommitTransaction();

		// TODO: Consider returning return a manufactured object based on the method parameters without
		// making a database query?
		return await GetArtefactGroupAsync(editionUser, artefactGroupId);
	}

	public async Task DeleteArtefactGroupAsync(UserInfo editionUser, uint artefactGroupId)
	{
		// Get group members and name (if any)
		var (members, groupData) =
				await _getArtefactGroupInternalInfo(editionUser, artefactGroupId);

		if (members.Count() == 0)
		{
			throw new StandardExceptions.DataNotFoundException(
					"artefact group id"
					, artefactGroupId);
		}

		var alterations = members.Select(x =>
										 {
											 var artefactInGroupParams = new DynamicParameters();

											 artefactInGroupParams.Add(
													 "@artefact_id"
													 , x.ArtefactId);

											 artefactInGroupParams.Add(
													 "@artefact_group_id"
													 , artefactGroupId);

											 return new MutationRequest(
													 MutateType.Delete
													 , artefactInGroupParams
													 , "artefact_group_member"
													 , x.ArtefactGroupMemberId);
										 })
								 .ToList();

		if (groupData != null)
		{
			var artefactGroupNameParams = new DynamicParameters();
			artefactGroupNameParams.Add("@Name", groupData.Name);

			artefactGroupNameParams.Add("@ArtefactGroupId", artefactGroupId);

			alterations.Add(
					new MutationRequest(
							MutateType.Delete
							, artefactGroupNameParams
							, "artefact_group_data"
							, groupData.ArtefactGroupDataId));
		}

		var responses = await adb.WriteToDatabaseAsync(editionUser, alterations.AsList());

		if (responses.Count != alterations.Count)
			throw new StandardExceptions.DataNotWrittenException("delete an artefact group");

		adb.CommitTransaction();

		// Verify the delete
		var artGroup = await GetArtefactGroupAsync(editionUser, artefactGroupId);

		if (artGroup != null)
			throw new StandardExceptions.DataNotWrittenException("delete an artefact group");
	}

	private async Task<MutationRequest> FormatArtefactPositionUpdateRequestAsync(
			UserInfo   editionUser
			, uint     artefactId
			, decimal? scale
			, decimal? rotate
			, int?     translateX
			, int?     translateY
			, int?     zIndex
			, bool?    mirrored)
	{
		const string tableName = "artefact_position";

		var notPositioned = !scale.HasValue
							&& !rotate.HasValue
							&& !translateX.HasValue
							&& !translateY.HasValue
							&& !zIndex.HasValue;

		var artefactPositionId = await GetArtefactPkAsync(editionUser, artefactId, tableName);

		// It is not necessary for every artefact to have a position (they may get positioning via artefact stack).
		// If no artefact_position already exists we need to create a new entry here.
		if ((artefactPositionId == 0)
			&& !notPositioned)
		{
			return FormatArtefactPositionInsertion(
					artefactId
					, scale
					, rotate
					, translateX
					, translateY
					, zIndex
					, mirrored);
		}

		var artefactChangeParams = new DynamicParameters();

		if (scale.HasValue)
			artefactChangeParams.Add("@scale", scale);

		if (rotate.HasValue)
			artefactChangeParams.Add("@rotate", rotate);

		if (zIndex.HasValue)
			artefactChangeParams.Add("@z_index", zIndex);

		artefactChangeParams.Add("@translate_x", translateX);
		artefactChangeParams.Add("@translate_y", translateY);
		artefactChangeParams.Add("@artefact_id", artefactId);

		var artefactChangeRequest = new MutationRequest(
				notPositioned
						? MutateType.Delete
						: MutateType.Update
				, // delete if the artefact is not positioned at all
				artefactChangeParams
				, tableName
				, artefactPositionId);

		return artefactChangeRequest;
	}

	public async Task<List<AlteredRecord>> InsertArtefactShapeAsync(
			UserInfo editionUser
			, uint   artefactId
			, uint?  masterImageId
			, string shape)
	{
		/* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
write the value directly into the database, but it does not seem to work right yet.  Thus we currently
use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().

var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
var res = string.Join("", binaryMask);
var Mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/

		var artefactChangeParams = new DynamicParameters();
		artefactChangeParams.Add("@region_in_sqe_image", shape);
		artefactChangeParams.Add("@sqe_image_id", masterImageId);
		artefactChangeParams.Add("@artefact_id", artefactId);

		var artefactChangeRequest = new MutationRequest(
				MutateType.Create
				, artefactChangeParams
				, "artefact_shape");

		return await WriteArtefactAsync(editionUser, artefactChangeRequest);
	}

	public async Task<List<AlteredRecord>> InsertArtefactStatusAsync(
			UserInfo editionUser
			, uint   artefactId
			, string workStatus)
	{
		var artefactChangeParams = new DynamicParameters();
		artefactChangeParams.Add("@artefact_id", artefactId);

		if (!string.IsNullOrEmpty(workStatus))
			artefactChangeParams.Add("@work_status_id", await SetWorkStatusAsync(workStatus));

		var artefactChangeRequest = new MutationRequest(
				MutateType.Create
				, artefactChangeParams
				, "artefact_status");

		return await WriteArtefactAsync(editionUser, artefactChangeRequest);
	}

	public async Task<List<AlteredRecord>> InsertArtefactNameAsync(
			UserInfo editionUser
			, uint   artefactId
			, string name)
	{
		var artefactChangeParams = new DynamicParameters();
		artefactChangeParams.Add("@name", name);
		artefactChangeParams.Add("@artefact_id", artefactId);

		var artefactChangeRequest = new MutationRequest(
				MutateType.Create
				, artefactChangeParams
				, "artefact_data");

		return await WriteArtefactAsync(editionUser, artefactChangeRequest);
	}

	public async Task<List<AlteredRecord>> InsertArtefactPositionAsync(
			UserInfo   editionUser
			, uint     artefactId
			, decimal? scale
			, decimal? rotate
			, int?     translateX
			, int?     translateY
			, int?     zIndex
			, bool?    mirrored) => await WriteArtefactAsync(
			editionUser
			, FormatArtefactPositionInsertion(
					artefactId
					, scale
					, rotate
					, translateX
					, translateY
					, zIndex
					, mirrored));

	private static MutationRequest FormatArtefactPositionInsertion(
			uint       artefactId
			, decimal? scale
			, decimal? rotate
			, int?     translateX
			, int?     translateY
			, int?     zIndex
			, bool?    mirrored)
	{
		var artefactChangeParams = new DynamicParameters();

		if (scale.HasValue)
			artefactChangeParams.Add("@scale", scale);

		if (rotate.HasValue)
			artefactChangeParams.Add("@rotate", rotate);

		if (zIndex.HasValue)
			artefactChangeParams.Add("@z_index", zIndex);

		if (mirrored.HasValue)
			artefactChangeParams.Add("@mirrored", mirrored);

		artefactChangeParams.Add("@translate_x", translateX);
		artefactChangeParams.Add("@translate_y", translateY);
		artefactChangeParams.Add("@artefact_id", artefactId);

		var artefactChangeRequest = new MutationRequest(
				MutateType.Create
				, artefactChangeParams
				, "artefact_position");

		return artefactChangeRequest;
	}

	public async Task<List<AlteredRecord>> WriteArtefactAsync(
			UserInfo          editionUser
			, MutationRequest artefactChangeRequest
			, IDbConnection   connection = null) =>

			// Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
			// care of main_action and single_action.
			await adb.WriteToDatabaseAsync(
					editionUser
					, new List<MutationRequest> { artefactChangeRequest });

	private async Task<uint> GetArtefactPkAsync(UserInfo editionUser, uint artefactId, string table)
		=> await adb.QueryFirstOrDefaultAsync<uint>(
				FindArtefactComponentId.GetQuery(table)
				, new
				{
						editionUser.EditionId
						, ArtefactId = artefactId
						,
				});

	private async Task<List<uint>> GetArtefactStackPksAsync(
			UserInfo editionUser
			, uint   artefactId
			, string table)
	{
		var stacks = (await adb.QueryAsync<uint>(
				FindArtefactComponentId.GetQuery(table, true)
				, new
				{
						editionUser.EditionId
						, ArtefactId = artefactId
						,
				})).ToList();

		return stacks;
	}

	private async Task<uint?> GetArtefactShapeSqeImageIdAsync(UserInfo editionUser, uint artefactId)
		=> await adb.QueryFirstOrDefaultAsync<uint?>(
				FindArtefactShapeSqeImageId.GetQuery
				, new
				{
						editionUser.EditionId
						, ArtefactId = artefactId
						,
				});

	private async Task<uint?> SetWorkStatusAsync(string workStatus)
	{
		if (string.IsNullOrEmpty(workStatus))
			return null;

		await adb.ExecuteAsync(SetWorkStatus.GetQuery, new { WorkStatus = workStatus });

		return await adb.QuerySingleAsync<uint>(
				GetWorkStatus.GetQuery
				, new { WorkStatus = workStatus });
	}

	private async Task<(List<ArtefactGroupMember> groupMembers, ArtefactGroupData groupData)>
			_getArtefactGroupInternalInfo(UserInfo editionUser, uint artefactGroupId)
	{
		// Get group members
		var members = await adb.QueryAsync<ArtefactGroupMember>(
				FindArtefactGroupMembers.GetQuery
				, new
				{
						ArtefactGroupId = artefactGroupId
						, editionUser.EditionId
						,
				});

		// Get group name (if any)
		var groupData = await adb.QuerySingleOrDefaultAsync<ArtefactGroupData>(
				FindArtefactGroupDataId.GetQuery
				, new
				{
						ArtefactGroupId = artefactGroupId
						, editionUser.EditionId
						,
				});

		return (members.ToList(), groupData);
	}

	private async Task _verifyArtefactsFreeForGroup(UserInfo editionUser, List<uint> artefactIds)
	{
		// Check if the desired artefacts are already used in another artefact group
		var alreadyUsedArtefacts = (await adb.QueryAsync<uint>(
				ArtefactsAlreadyInGroups.GetQuery
				, new
				{
						editionUser.EditionId
						, ArtefactIds = artefactIds.ToArray()
						,
				})).ToList();

		if (alreadyUsedArtefacts.Count != 0)
		{
			throw new StandardExceptions.InputDataRuleViolationException(
					$"The artefact {
						(alreadyUsedArtefacts.Count > 1
								? "ids"
								: "id")
					} "
					+ $"{string.Join(", ", alreadyUsedArtefacts)} "
					+ $"{
						(alreadyUsedArtefacts.Count > 1
								? "are"
								: "is")
					} already in another group");
		}

		// Check to see if the artefact are in fact part of this edition
		var artefactsInEdition = (await adb.QueryAsync<uint>(
				ArtefactsFromListInEdition.GetQuery
				, new
				{
						editionUser.EditionId
						, ArtefactIds = artefactIds
						,
				})).ToList();

		if (artefactsInEdition.Count != artefactIds.Count())
		{
			var artefactsNotInEdition = artefactIds.Except(artefactsInEdition).ToList();

			throw new StandardExceptions.InputDataRuleViolationException(
					$"The artefact {
						(artefactsNotInEdition.Count > 1
								? "ids"
								: "id")
					} "
					+ $"{string.Join(", ", artefactsNotInEdition)} "
					+ $"{
						(artefactsNotInEdition.Count > 1
								? "are"
								: "is")
					} not part of this edition");
		}
	}

	private static class ArtefactTableNames
	{
		private const string Data     = "artefact_data";
		private const string Shape    = "artefact_shape";
		private const string Position = "artefact_position";
		public const  string Stack    = "artefact_stack";
		private const string Status   = "artefact_status";

		public static IEnumerable<string> All() => new List<string>
		{
				Data
				, Shape
				, Position
				, Stack
				, Status
				,
		};
	}
}
