using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.DatabaseAccess.Helpers;

/// <summary>
///  This enum sets a mutation request to one of the three types.
/// </summary>
public enum MutateType
{
	Create
	, Update
	, Delete
	,
}

/// <summary>
///  This is an object containing all the necessary data for a single mutation in the database.
/// </summary>
public class MutationRequest
{
	/// <summary>
	///  Initializes a new instance of the <see cref="T:SQE.DatabaseAccess.Helpers.MutationRequest" /> class.
	/// </summary>
	/// <param name="action">
	///  Set the mutate to Create, Update, or Delete.
	///  For Update or Delete, the id for the record being updated or deleted must be passed in tablePkId.
	/// </param>
	/// <param name="parameters">
	///  These are the parameters for the columns that will be inserted/updated in the SQL query.
	///  The parameter names must start with @ and use the column Name exactly as it is written in the database (e.g.,
	///  `@scroll_id`).
	///  For Delete actions this should be empty.
	/// </param>
	/// <param name="tableName">Name of the table you are altering.</param>
	/// <param name="tablePkId">Id of the record being updated or deleted.  This will be null with an Insert action.</param>
	public MutationRequest(
			MutateType          action
			, DynamicParameters parameters
			, string            tableName
			, uint?             tablePkId = null)
	{
		Action = action;
		Parameters = parameters ?? new DynamicParameters();
		TableName = tableName;
		TablePkId = tablePkId;

		// The columns we are writing must all have values in the Parameters.
		// We pull these out of the parameters.  ColumnNames is used to build
		// the insert statements: INSERT INTO table_x (...ColumnNames).
		ColumnNames = Parameters.ParameterNames.ToList();

		// Fail creating the object if missing record id for update/delete.
		if (((action == MutateType.Update) || (action == MutateType.Delete))
			&& !tablePkId.HasValue)
		{
			throw new ArgumentException(
					"The primary key of the record is necessary for Update and Delete actions"
					, nameof(tablePkId));
		}

		if (tablePkId.HasValue) // Add the record id to the parameters.
			Parameters.Add("@OwnedTableId", tablePkId.Value);
	}

	public MutateType Action { get; }

	public List<string> ColumnNames { get; }

	// Itay, we still need this, since we add more to the SQL parameters than
	// just the column names after this mutation request is created (e.g.
	// @EditionId and maybe @OwnedTableId).  But now this is computed
	// automatically from the Parameters in the constructor. So we no longer
	// need any safety checks in the constructor.
	public DynamicParameters Parameters { get; }
	public string            TableName  { get; }
	public uint?             TablePkId  { get; }
}

// This is used to know if we need to wrap the insert parameter in ST_GeomFromText()
// I don't love this.  Perhaps we can directly insert the binary blob, or maybe we can find
// a less computationally expensive way to do this.
public static class GeometryColumns
{
	public static readonly List<string> columns = new()
	{
			"region_in_sqe_imageartefact_shape"
			, "artefact_B_offsetartefact_stack"
			, "pathartefact_stack"
			, "region_on_image1image_to_image_map"
			, "point_on_image1point_to_point_map"
			, "point_on_image2point_to_point_map"
			, "pathroi_shape"
			, "shapescribal_font_glyph_metrics"
			,
	};
}

/// <summary>
///  This is a return type giving necessary information for each mutation.
/// </summary>
public class AlteredRecord
{
	/// <summary>
	///  Initializes a new instance of the <see cref="T:SQE.DatabaseAccess.Helpers.AlteredRecord" /> class.
	/// </summary>
	/// <param name="tableName">Name of the table that was altered.</param>
	/// <param name="oldId">Id of the record that was altered. Only present with update/delete.</param>
	/// <param name="newId">Id of the new record that was created. Only present with update/create.</param>
	public AlteredRecord(string tableName, uint? oldId, uint? newId)
	{
		TableName = tableName;
		OldId = oldId;
		NewId = newId;
	}

	public string TableName { get; set; }
	public uint?  OldId     { get; set; }
	public uint?  NewId     { get; set; }
}

public interface IDatabaseWriter
{
	Task<List<AlteredRecord>> WriteToDatabaseAsync(
			UserInfo                editionUser
			, List<MutationRequest> mutationRequests
			, DatabaseAccessor      dba);

	Task<List<AlteredRecord>> WriteToDatabaseAsync(
			UserInfo           editionUser
			, MutationRequest  mutationRequest
			, DatabaseAccessor dba);
}

public class DatabaseWriter : IDatabaseWriter
{
	/// <summary>
	///  Performs a list mutation requests for a single scroll version and user.
	/// </summary>
	/// <returns>
	///  A list of AlteredRecord objects containing the details of each mutation.
	///  The order of the returned list or results matches the order of the list of mutation requests
	/// </returns>
	/// <param name="editionUser"></param>
	/// <param name="mutationRequests">List of mutation requests.</param>
	/// <param name="connection">Optional database connection to use. If null, a new connection will be created.</param>
	public async Task<List<AlteredRecord>> WriteToDatabaseAsync(
			UserInfo                editionUser
			, List<MutationRequest> mutationRequests
			, DatabaseAccessor      dba)
	{
		// Check if the edition is locked
		if (editionUser.EditionLocked)
			throw new StandardExceptions.LockedDataException(editionUser);

		// Check the permissions and throw if user has no rights to alter this edition
		if (!editionUser.MayWrite)
			throw new StandardExceptions.NoWritePermissionsException(editionUser);

		var results = await _writeToDatabaseAsync(editionUser, mutationRequests, dba);

		return results;
	}

	public async Task<List<AlteredRecord>> WriteToDatabaseAsync(
			UserInfo           editionUser
			, MutationRequest  mutationRequest
			, DatabaseAccessor dba) => await WriteToDatabaseAsync(
			editionUser
			, new List<MutationRequest> { mutationRequest }
			, dba);

	private async Task<List<AlteredRecord>> _writeToDatabaseAsync(
			UserInfo                editionUser
			, List<MutationRequest> mutationRequests
			, DatabaseAccessor      dba)
	{
		var alteredRecords = new List<AlteredRecord>();
		foreach (var mutationRequest in mutationRequests)
		{
			// Set the editionId for the mutation.
			// Though we accept a List of mutations, we have the restriction that
			// they all belong to the same editionId and userID.
			// This way, we only do one permission check for the whole batch.
			mutationRequest.Parameters.Add("@EditionId", editionUser.EditionId);

			mutationRequest.Parameters.Add("@EditionEditorId", editionUser.EditionEditorId);

			await AddMainActionAsync(dba, mutationRequest);

			switch (mutationRequest.Action)
			{
				case MutateType.Create:
					// Insert the record and add its response to the alteredRecords response.
					var createdRecord = await InsertAsync(
							dba
							, mutationRequest
							, editionUser.userId.Value);

					//	System.Threading.Thread.Sleep(1000);
					alteredRecords.Add(createdRecord);

					break;

				case MutateType.Update:
					// Update in our system is really Delete + Insert, the old record remains.
					// Delete the old record
					var priorDeletedRecord = await DeleteAsync(dba, mutationRequest);

					// Insert the new record
					var insertedRecord = await InsertAsync(
							dba
							, mutationRequest
							, editionUser.userId.Value);

					// Merge the request responses by copying the deleted Id to the insertRecord object
					insertedRecord.OldId = priorDeletedRecord.OldId;

					// Add info to the return object
					alteredRecords.Add(insertedRecord);

					break;

				case MutateType.Delete:
					// Delete the record and add its response to the alteredRecords response.
					var deletedRecord = await DeleteAsync(dba, mutationRequest);

					alteredRecords.Add(deletedRecord);

					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		// Check if any operation here would invalidate a cached text transcription,
		// if so, invalidate the cached transcription. First gather all referenced
		// text fragments, then Union the results so there are no doubles.
		var lineMutations = mutationRequests
							.Where(x => x.Parameters.ParameterNames.Contains("line_id"))
							.Select(x => x.Parameters.Get<uint>("line_id")).ToList();
		var lineIds = new List<uint>(lineMutations.Count);

		foreach (var lineMutationId in lineMutations)
			lineIds.AddRange(await _textEditionByLineId(editionUser, lineMutationId, dba));

		var signInterpretationMutations = mutationRequests
										  .Where(x => x.Parameters.ParameterNames.Contains(
														 "sign_interpretation_id"))
										  .Select(x => x.Parameters.Get<uint>(
														  "sign_interpretation_id"))
										  .ToList();
		var signInterpretationIds = new List<uint>(signInterpretationMutations.Count);

		foreach (var signInterpretationMutationId in signInterpretationMutations)
		{
			signInterpretationIds.AddRange(
					await _textEditionBySignInterpretationId(
							editionUser
							, signInterpretationMutationId
							, dba));
		}

		var textFragmentIds = mutationRequests
							  .Where(x => x.Parameters.ParameterNames.Contains(
											 "text_fragment_id"))
							  .Select(x => x.Parameters.Get<uint>("text_fragment_id"))
							  .Distinct()
							  .Union(lineIds)
							  .Union(signInterpretationIds);

		foreach (var textFragmentId in textFragmentIds)
			await _invalidateCachedTextEdition(editionUser, textFragmentId, dba);

		return alteredRecords;
	}

	/// <summary>
	///  Insert record into table.  This takes care of writing the new record (if necessary) and makes the necessary
	///  changes to the owner tables.  It also records the action in the database.
	/// </summary>
	/// <param name="connection">An IDbConnection belonging to the current transaction.</param>
	/// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
	/// <returns>The alteredRecord object to be added to the request response.</returns>
	private static async Task<AlteredRecord> InsertAsync(
			DatabaseAccessor  dba
			, MutationRequest mutationRequest
			, uint            userId)
	{
		// Insert the record (or return the id of a preexisting record matching the unique constraints.
		var createInsertId = await InsertOwnedTableAsync(dba, mutationRequest, userId);

		// Insert the link to the editionId in the owner table
		await InsertOwnerTableAsync(dba, mutationRequest, createInsertId);

		// Record the insert
		await AddSingleActionAsync(dba, mutationRequest, SingleAction.Add);

		// Create info for the request's return object
		return new AlteredRecord(mutationRequest.TableName, null, createInsertId);
	}

	/// <summary>
	///  Delete record.  This takes care of deleting the record and by making the necessary
	///  changes to the owner table.  It also records the action in the database.
	/// </summary>
	/// <param name="connection">An IDbConnection belonging to the current transaction</param>
	/// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
	/// <returns>The alteredRecord object to be added to the request response.</returns>
	private static async Task<AlteredRecord> DeleteAsync(
			DatabaseAccessor  dba
			, MutationRequest mutationRequest)
	{
		// Delete the link between this scrollVersionID and the record from the owner table
		await DeleteOwnerTableAsync(dba, mutationRequest);

		// Record the delete
		await AddSingleActionAsync(dba, mutationRequest, SingleAction.Delete);

		// Create info for the request's return object
		return new AlteredRecord(mutationRequest.TableName, mutationRequest.TablePkId, null);
	}

	/// <summary>
	///  This inserts the requested data into its table. If a record with the same data already exists, then the
	///  Id of that record is used in place of creating a duplicate record.
	/// </summary>
	/// <param name="connection">An IDbConnection belonging to the current transaction</param>
	/// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
	/// <param name="userId">Identifier for the user who is making the insertion.</param>
	/// <returns>
	///  Returns the Id of the newly inserted record. If a record with the same data already existed,
	///  then the Id of that record is returned.
	/// </returns>
	private static async Task<uint> InsertOwnedTableAsync(
			DatabaseAccessor  dba
			, MutationRequest mutationRequest
			, uint            userId)
	{
		var query = OwnedTableInsertQuery.GetQuery();

		query = query.Replace("$TableName", mutationRequest.TableName);

		query = query.Replace(
				"$Columns"
				, string.Join(",", mutationRequest.ColumnNames.Select(x => $"`{x}`")));

		query = query.Replace(
				"$Values"
				, string.Join(
						","
						, mutationRequest.ColumnNames.Select(x => GeometryColumns.columns.IndexOf(
																		  x
																		  + mutationRequest
																				  .TableName)
																  > -1
																	 ? $"ST_GeomFromText(@{x})"
																	 : "@" + x)));

		query = query.Replace(
				"$Where"
				, string.Join(
						" AND "
						, mutationRequest.ColumnNames.Select(x => $"`{x}` <=> "
																  + (GeometryColumns.columns
																					.IndexOf(
																							x
																							+ mutationRequest
																									.TableName)
																	 > -1
																		  ? $"ST_GeomFromText(@{
																			  x
																		  })"
																		  : "@" + x))));

		mutationRequest.Parameters.Add("@UserId", userId);

		// Execute query
		var alteredRecords = await dba.ExecuteAsync(query, mutationRequest.Parameters);

		uint insertId;

		if (alteredRecords == 0) // Nothing was inserted because the exact record already existed.
		{
			// Get id of new record (or the record matching the unique constraints of this request).
			query = OwnedTableIdQuery.GetQuery();

			query = query.Replace("$TableName", mutationRequest.TableName);

			query = query.Replace(
					"$Columns"
					, string.Join(",", mutationRequest.ColumnNames.Select(x => $"`{x}`")));

			query = query.Replace(
					"$Values"
					, string.Join(
							","
							, mutationRequest.ColumnNames.Select(x => GeometryColumns.columns
																					 .IndexOf(
																							 x
																							 + mutationRequest
																									 .TableName)
																	  > -1
																		 ? $"ST_GeomFromText(@{x})"
																		 : "@" + x)));

			query = query.Replace(
					"$Where"
					, string.Join(
							" AND "
							, mutationRequest.ColumnNames.Select(x => $"`{x}` <=> "
																	  + (GeometryColumns.columns
																						.IndexOf(
																								x
																								+ mutationRequest
																										.TableName)
																		 > -1
																			  ? $"ST_GeomFromText(@{
																				  x
																			  })"
																			  : "@" + x))));

			query = query.Replace("$PrimaryKeyName", mutationRequest.TableName + "_id");

			insertId = await dba.QuerySingleAsync<uint>(query, mutationRequest.Parameters);
		}
		else // A new record was inserted.
		{
			// Get the id of the newly inserted record.
			insertId = await LastInsertIdAsync(dba);
		}

		return insertId;
	}

	/// <summary>
	///  Creates an entry in the owner table linking the editionId to the record with the inserted data.
	/// </summary>
	/// <param name="connection">An IDbConnection belonging to the current transaction</param>
	/// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
	/// <param name="insertId">The primary key Id of the record that was just inserted.</param>
	/// <returns></returns>
	private static async Task InsertOwnerTableAsync(
			DatabaseAccessor  dba
			, MutationRequest mutationRequest
			, uint            insertId)
	{
		// Format query
		var query = OwnerTableInsertQuery.GetQuery;

		query = query.Replace("$OwnerTableName", mutationRequest.TableName + "_owner");

		query = query.Replace("$OwnedTablePkName", mutationRequest.TableName + "_id");

		// Insert the @OwnedTableId parameter
		mutationRequest.Parameters.Add("@OwnedTableId", insertId);

		// Execute query
		var result = await dba.ExecuteAsync(query, mutationRequest.Parameters);

		if (mutationRequest.TableName.Equals("position_in_stream"))
		{
			var r = await dba.QueryAsync<uint>(
					$@"
select position_in_stream_id from position_in_stream_owner where position_in_stream_id={
	insertId
};");
		}
	}

	/// <summary>
	///  Delete the entry in the owner table that links a particular record to a specific editionId
	/// </summary>
	/// <param name="connection">An IDbConnection belonging to the current transaction</param>
	/// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
	/// <returns></returns>
	private static async Task DeleteOwnerTableAsync(
			DatabaseAccessor  dba
			, MutationRequest mutationRequest)
	{
		// Format query
		var query = OwnerTableDeleteQuery.GetQuery;

		query = query.Replace("$OwnerTableName", mutationRequest.TableName + "_owner");

		query = query.Replace("$OwnedTablePkName", mutationRequest.TableName + "_id");

		// Execute query
		var results = await dba.ExecuteAsync(query, mutationRequest.Parameters);

		// If nothing was changed, then the data was not found, so throw an error.
		if (results < 1)
		{
			throw new StandardExceptions.DataNotFoundException(
					mutationRequest.TableName
					, mutationRequest.TablePkId ?? 0);
		}
	}

	/// <summary>
	///  Convenience function to get the last insert Id. Throws on error.
	/// </summary>
	/// <param name="connection">An IDbConnection belonging to the current transaction</param>
	/// <returns>Returns the Id of the last inserted record.</returns>
	private static async Task<uint> LastInsertIdAsync(DatabaseAccessor dba)
	{
		const string sql = "SELECT LAST_INSERT_ID()";

		return await dba.QuerySingleAsync<uint>(sql);
	}

	/// <summary>
	///  Creates an entry in the main_action table for the current mutation request.
	/// </summary>
	/// <param name="connection">An IDbConnection belonging to the current transaction</param>
	/// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
	/// <returns></returns>
	private static async Task AddMainActionAsync(
			DatabaseAccessor  dba
			, MutationRequest mutationRequest)
	{
		// Format and execute the query
		const string query = MainActionInsertQuery.GetQuery;
		await dba.ExecuteAsync(query, mutationRequest.Parameters);

		// Get id of new record.
		var insertId = await LastInsertIdAsync(dba);

		// Insert the @MainActionId into the mutation object's query parameters.
		mutationRequest.Parameters.Add("@MainActionId", insertId);
	}

	/// <summary>
	///  Creates and entry in the single_action table to record the mutation.
	/// </summary>
	/// <param name="connection">An IDbConnection belonging to the current transaction</param>
	/// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
	/// <param name="action"></param>
	/// <returns></returns>
	private static async Task AddSingleActionAsync(
			DatabaseAccessor  dba
			, MutationRequest mutationRequest
			, SingleAction    action)
	{
		// Format query
		var query = SingleActionInsertQuery.GetQuery;

		// Add parameters
		mutationRequest.Parameters.Add("@TableName", mutationRequest.TableName);

		mutationRequest.Parameters.Add("@Action", action.ToString().ToLower());

		// Execute query
		await dba.ExecuteAsync(query, mutationRequest.Parameters);
	}

	private async Task _invalidateCachedTextEdition(
			UserInfo           editionUser
			, uint             textFragmentId
			, DatabaseAccessor dba
			)
	{
		await dba.ExecuteAsync(
			RemoveCachedTextFragment.GetQuery
			, new
			{
					editionUser.EditionId
					, TextFragmentId = textFragmentId
					,
			});
	}

	private async Task<IEnumerable<uint>> _textEditionByLineId(
			UserInfo           editionUser
			, uint             lineId
			, DatabaseAccessor dba) => await dba.QueryAsync<uint>(
			GetTextFragmentIdFromLineId.GetQuery
			, new { editionUser.EditionId, LineId = lineId });

	private async Task<IEnumerable<uint>> _textEditionBySignInterpretationId(
			UserInfo           editionUser
			, uint             signInterpretationId
			, DatabaseAccessor dba) => await dba.QueryAsync<uint>(
			GetTextFragmentIdFromSingInterpretationId.GetQuery
			, new
			{
					editionUser.EditionId
					, SignInterpretationId = signInterpretationId
					,
			});

	/// <summary>
	///  Enum for allowed actions in the single_action database table.
	/// </summary>
	private enum SingleAction
	{
		Add
		, Delete
		,
	}
}
