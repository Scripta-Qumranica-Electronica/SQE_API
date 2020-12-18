using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;
// ReSharper disable ArrangeRedundantParentheses

namespace SQE.DatabaseAccess
{
	public interface IAttributeRepository
	{
		Task<IEnumerable<SignInterpretationAttributeEntry>> GetAllEditionAttributesAsync(
				UserInfo editionUser);

		Task<IEnumerable<SignInterpretationAttributeEntry>> GetEditionAttributeAsync(
				UserInfo editionUser
				, uint   attributeId);

		Task<uint> CreateEditionAttribute(
				UserInfo                                             editionUser
				, string                                             attributeName
				, string                                             attributeDescription
				, bool                                               editable
				, bool                                               removable
				, bool                                               repeatable
				, bool                                               batchEditable
				, IEnumerable<SignInterpretationAttributeValueInput> attributeValues);

		Task<uint> UpdateEditionAttribute(
				UserInfo                                             editionUser
				, uint                                               attributeId
				, string                                             attributeName
				, string                                             attributeDescription
				, bool                                               editable
				, bool                                               removable
				, bool                                               repeatable
				, bool                                               batchEditable
				, IEnumerable<SignInterpretationAttributeValueInput> createAttributeValues
				, IEnumerable<SignInterpretationAttributeValue>      updateAttributeValues
				, IEnumerable<uint>                                  deleteAttributeValues);

		Task DeleteEditionAttributeAsync(UserInfo editionUser, uint attributeId);

		// Task DeleteEditionAttributeValueAsync(UserInfo editionUser, uint attributeValueId);
		Task<List<SignInterpretationAttributeData>> CreateSignInterpretationAttributesAsync(
				UserInfo                                editionUser
				, uint                                  signInterpretationId
				, List<SignInterpretationAttributeData> newAttributes);

		Task<List<SignInterpretationAttributeData>> CreateSignInterpretationAttributesAsync(
				UserInfo                          editionUser
				, uint                            signInterpretationId
				, SignInterpretationAttributeData newAttribute);

		// Task<List<SignInterpretationAttributeData>> UpdateSignInterpretationAttributesAsync(UserInfo editionUser,
		//     uint signInterpretationId,
		//     List<SignInterpretationAttributeData> updateAttributes);
		//
		// Task<List<uint>> DeleteSignInterpretationAttributesAsync(UserInfo editionUser, List<uint> deleteAttributeIds);

		Task<List<uint>> DeleteAttributeFromSignInterpretationAsync(
				UserInfo editionUser
				, uint   signInterpretationId
				, uint   attributeValueId);

		Task<List<uint>> DeleteAllAttributesForSignInterpretationAsync(
				UserInfo editionUser
				, uint   signInterpretationId);

		Task UpdateAttributeForSignInterpretationAsync(
				UserInfo editionUser
				, uint   signInterpretationId
				, uint   attributeValueId
				, byte?  sequence);

		// Task<SignInterpretationAttributeData> GetSignInterpretationAttributeByIdAsync(UserInfo editionUser,
		//     uint signInterpretationAttributeId);

		Task<List<SignInterpretationAttributeData>> GetSignInterpretationAttributesByDataAsync(
				UserInfo                                    editionUser
				, SignInterpretationAttributeDataSearchData dataSearchData);

		Task<List<SignInterpretationAttributeData>>
				GetSignInterpretationAttributesByInterpretationId(
						UserInfo editionUser
						, uint   signInterpretationId);

		// Task<uint> GetSignInterpretationAttributeIdByIdAsync(UserInfo editionUser,
		//     uint signInterpretationAttributeId);
		//
		// Task<List<uint>> GetSignInterpretationAttributeIdsByDataAsync(
		//     UserInfo editionUser,
		//     SignInterpretationAttributeDataSearchData dataSearchData);
		//
		// Task<List<uint>> GetSignInterpretationAttributeIdsByInterpretationId(
		//     UserInfo editionUser,
		//     uint signInterpretationId);

		Task<List<SignInterpretationAttributeData>> ReplaceSignInterpretationAttributesAsync(
				UserInfo                                editionUser
				, uint                                  signInterpretationId
				, List<SignInterpretationAttributeData> newAttributes);
	}

	public class AttributeRepository : DbConnectionBase
									   , IAttributeRepository
	{
		private readonly IDatabaseWriter _databaseWriter;

		public AttributeRepository(IConfiguration config, IDatabaseWriter databaseWriter) :
				base(config) => _databaseWriter = databaseWriter;


		/// <summary>
		///  Get all attributes associated with a particular edition
		/// </summary>
		/// <param name="editionUser">The edition user details object</param>
		/// <returns>The details of the attributes associated with a particular edition</returns>
		public async Task<IEnumerable<SignInterpretationAttributeEntry>>
				GetAllEditionAttributesAsync(UserInfo editionUser)
		{
			using (var connection = OpenConnection())
			{
				return await connection.QueryAsync<SignInterpretationAttributeEntry>(
						GetAllEditionSignInterpretationAttributesQuery.GetQuery()
						, new { editionUser.EditionId });
			}
		}

		/// <summary>
		///  Get attributes associated with a particular edition by its unique id
		/// </summary>
		/// <param name="editionUser">The edition user details object</param>
		/// <param name="attributeId">The unique id of the desired attribute</param>
		/// <returns>The details of the desired attribute</returns>
		public async Task<IEnumerable<SignInterpretationAttributeEntry>> GetEditionAttributeAsync(
				UserInfo editionUser
				, uint   attributeId)
		{
			using (var connection = OpenConnection())
			{
				return await connection.QueryAsync<SignInterpretationAttributeEntry>(
						GetAllEditionSignInterpretationAttributesQuery.GetQuery(attributeId)
						, new
						{
								editionUser.EditionId
								, AttributeId = attributeId
								,
						});
			}
		}

		/// <summary>
		///  Create a new attribute for an edition. The attribute may have 0 or more
		///  values associated with it.
		/// </summary>
		/// <param name="editionUser">The edition user details object</param>
		/// <param name="attributeName">Name of the new attribute</param>
		/// <param name="attributeDescription">Description of the attribute</param>
		/// <param name="attributeValues">A list of 0 or more possible values for the attribute</param>
		/// <param name="batchEditable">Whether the attribute can be edited in batches</param>
		/// <param name="editable">Whether the attribute is editable</param>
		/// <param name="removable">Whether the attribute is removable</param>
		/// <param name="repeatable">Whether the attribute can be set more than one time for the same sign interpretation</param>
		/// <returns>The unique id of the newly created attribute</returns>
		public async Task<uint> CreateEditionAttribute(
				UserInfo                                             editionUser
				, string                                             attributeName
				, string                                             attributeDescription
				, bool                                               editable
				, bool                                               removable
				, bool                                               repeatable
				, bool                                               batchEditable
				, IEnumerable<SignInterpretationAttributeValueInput> attributeValues)
		{
			using (var transactionScope =
					new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			{
				// First check for attribute name collisions
				var existingAttribute = await GetAllEditionAttributesAsync(editionUser);

				// Throw an error if the attribute name is not unique
				if (existingAttribute.Any(x => x.AttributeName == attributeName))
					throw new StandardExceptions.ConflictingDataException("attribute");

				// Write the new attribute
				var newAttributeId = await _createOrUpdateEditionAttribute(
						editionUser
						, attributeName
						, attributeDescription
						, editable
						, removable
						, repeatable
						, batchEditable);

				// Write the new attribute values
				foreach (var attributeValue in attributeValues)
				{
					await _createOrUpdateEditionAttributeValue(
							editionUser
							, newAttributeId
							, attributeValue.AttributeStringValue
							, attributeValue.AttributeStringValueDescription
							, attributeValue.Css);
				}

				// Complete transaction and return the id of the new attribute
				transactionScope.Complete();

				return newAttributeId;
			}
		}

		/// <summary>
		///  Update the details of an attribute in an edition.
		/// </summary>
		/// <param name="editionUser">The edition user details object</param>
		/// <param name="attributeId">The unique id of the attribute to be updated</param>
		/// <param name="attributeName">The new name for the updated attribute (no change if null or empty)</param>
		/// <param name="attributeDescription">The new description of the updated attribute (no change if null or empty)</param>
		/// <param name="createAttributeValues">A list of attribute values to be created</param>
		/// <param name="updateAttributeValues">A list of attribute values to be updated</param>
		/// <param name="deleteAttributeValues">A list of attribute value ids to be deleted</param>
		/// <param name="batchEditable">Whether the attribute can be edited in batches</param>
		/// <param name="editable">Whether the attribute is editable</param>
		/// <param name="removable">Whether the attribute is removable</param>
		/// <param name="repeatable">Whether the attribute can be set more than one time for the same sign interpretation</param>
		/// <returns></returns>
		public async Task<uint> UpdateEditionAttribute(
				UserInfo                                             editionUser
				, uint                                               attributeId
				, string                                             attributeName
				, string                                             attributeDescription
				, bool                                               editable
				, bool                                               removable
				, bool                                               repeatable
				, bool                                               batchEditable
				, IEnumerable<SignInterpretationAttributeValueInput> createAttributeValues
				, IEnumerable<SignInterpretationAttributeValue>      updateAttributeValues
				, IEnumerable<uint>                                  deleteAttributeValues)
		{
			using (var transactionScope =
					new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			{
				// First get the actual details of the attribute
				var existingAttribute =
						(await GetEditionAttributeAsync(editionUser, attributeId)).AsList();

				// Throw an error if the attribute id is not found
				if (!existingAttribute.Any())
					throw new StandardExceptions.DataNotFoundException("attribute", attributeId);

				// Check if there is an attribute name update
				if (!string.IsNullOrEmpty(attributeName)
					&& !existingAttribute.Any(x => x.AttributeName == attributeName))

						// Throw an error if the attribute name is not unique
				{
					if (existingAttribute.Any(x => x.AttributeName == attributeName))
						throw new StandardExceptions.ConflictingDataException("attribute");
				}

				// Write the attribute update if we have a new name or description
				var updatedAttributeId = string.IsNullOrEmpty(attributeName)
										 && string.IsNullOrEmpty(attributeDescription)
						? attributeId
						: await _createOrUpdateEditionAttribute(
								editionUser
								, attributeName
								, attributeDescription
								, editable
								, removable
								, repeatable
								, batchEditable
								, attributeId);

				// Merge the existing attribute values with the newly requested ones if a new attribute was created
				if (updatedAttributeId != attributeId)
				{
					createAttributeValues = createAttributeValues.ToList()
																 .Concat(
																		 existingAttribute.Select(
																				 x => new
																						 SignInterpretationAttributeValueInput
																						 {
																								 AttributeStringValue
																										 = x
																												 .AttributeStringValue
																								 , AttributeStringValueDescription
																										 = x
																												 .AttributeStringValueDescription
																								 , Css
																										 = x
																												 .Css
																								 ,
																						 }));
				}

				// Write the new attribute values
				foreach (var createAttributeValue in createAttributeValues)
				{
					await _createOrUpdateEditionAttributeValue(
							editionUser
							, updatedAttributeId
							, createAttributeValue.AttributeStringValue
							, createAttributeValue.AttributeStringValueDescription
							, createAttributeValue.Css);
				}

				// Write the attribute value updates
				foreach (var updateAttributeValue in updateAttributeValues)
				{
					await _createOrUpdateEditionAttributeValue(
							editionUser
							, updatedAttributeId
							, updateAttributeValue.AttributeStringValue
							, updateAttributeValue.AttributeStringValueDescription
							, updateAttributeValue.Css
							, updateAttributeValue.AttributeValueId);
				}

				// Write the attribute value deletes
				foreach (var deleteAttributeValue in deleteAttributeValues)
				{
					await _databaseWriter.WriteToDatabaseAsync(
							editionUser
							, new MutationRequest(
									MutateType.Delete
									, new DynamicParameters()
									, "attribute_value"
									, deleteAttributeValue));
				}

				// Complete transaction
				transactionScope.Complete();

				return updatedAttributeId;
			}
		}

		/// <summary>
		///  Delete the specified attribute from the edition
		/// </summary>
		/// <param name="editionUser">The edition user details object</param>
		/// <param name="attributeId">The unique id of the attribute to be deleted</param>
		/// <returns></returns>
		public async Task DeleteEditionAttributeAsync(UserInfo editionUser, uint attributeId)
		{
			using (var transactionScope =
					new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			{
				// First get the actual details of the attribute
				var existingAttributes =
						(await GetEditionAttributeAsync(editionUser, attributeId)).ToList();

				// Throw an error if the attribute id is not found
				if (!existingAttributes.Any())
					throw new StandardExceptions.DataNotFoundException("attribute", attributeId);

				// Delete the attribute values
				foreach (var attributeValueId in existingAttributes.Select(x => x.AttributeValueId)
																   .Distinct())
				{
					await _databaseWriter.WriteToDatabaseAsync(
							editionUser
							, new MutationRequest(
									MutateType.Delete
									, new DynamicParameters()
									, "attribute_value"
									, attributeValueId));
				}

				// Delete the attribute
				var deleteAttributeRequest = new MutationRequest(
						MutateType.Delete
						, new DynamicParameters()
						, "attribute"
						, attributeId);

				await _databaseWriter.WriteToDatabaseAsync(editionUser, deleteAttributeRequest);

				// Complete transaction
				transactionScope.Complete();
			}
		}

		/// <summary>
		///  Creates new attributes für a sign interpretation
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <param name="newAttributes">List of new attributes</param>
		/// <returns>List of new attributes</returns>
		public async Task<List<SignInterpretationAttributeData>>
				CreateSignInterpretationAttributesAsync(
						UserInfo                                editionUser
						, uint                                  signInterpretationId
						, List<SignInterpretationAttributeData> newAttributes)
		{
			using (var transactionScope =
					new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			{
				var response = await _createOrUpdateAttributesAsync(
						editionUser
						, signInterpretationId
						, newAttributes
						, MutateType.Create);

				transactionScope.Complete();

				return response;
			}
		}

		public async Task<List<SignInterpretationAttributeData>>
				CreateSignInterpretationAttributesAsync(
						UserInfo                          editionUser
						, uint                            signInterpretationId
						, SignInterpretationAttributeData newAttribute)
			=> await CreateSignInterpretationAttributesAsync(
					editionUser
					, signInterpretationId
					, new List<SignInterpretationAttributeData> { newAttribute });

		/// <summary>
		///  Retrieves all sign interpretation attributes which match the data provided by searchData
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="dataSearchData">Sign interpretation attribute search data object</param>
		/// <returns>List of sign interpretation attribute data - if nothing had been found the list is empty.</returns>
		public async Task<List<SignInterpretationAttributeData>>
				GetSignInterpretationAttributesByDataAsync(
						UserInfo                                    editionUser
						, SignInterpretationAttributeDataSearchData dataSearchData)
		{
			var query = GetSignInterpretationAttributesByDataQuery.GetQuery.Replace(
					"@WhereData"
					, dataSearchData.getSearchParameterString());

			using (var connection = OpenConnection())
			{
				var result = await connection.QueryAsync<SignInterpretationAttributeData>(
						query
						, new { editionUser.EditionId });

				return result == null
						? new List<SignInterpretationAttributeData>()
						: result.ToList();
			}
		}

		/// <summary>
		///  Gets all attributes of a the sign interpretation referred by its id
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <returns>List of sign interpretation attributes</returns>
		public async Task<List<SignInterpretationAttributeData>>
				GetSignInterpretationAttributesByInterpretationId(
						UserInfo editionUser
						, uint   signInterpretationId)
		{
			var searchData = new SignInterpretationAttributeDataSearchData
			{
					SignInterpretationId = signInterpretationId,
			};

			return await GetSignInterpretationAttributesByDataAsync(editionUser, searchData);
		}

		/// <summary>
		///  Deletes the specified attribute value from the sign interpretation referred by its id
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation to be altered</param>
		/// <param name="attributeValueId">Id of attribute value to remove</param>
		/// <returns>List of ids of delete attributes</returns>
		public async Task<List<uint>> DeleteAttributeFromSignInterpretationAsync(
				UserInfo editionUser
				, uint   signInterpretationId
				, uint   attributeValueId)
		{
			var searchData = new SignInterpretationAttributeDataSearchData
			{
					SignInterpretationId = signInterpretationId
					, AttributeValueId = attributeValueId
					,
			};

			var attributes =
					await GetSignInterpretationAttributeIdsByDataAsync(editionUser, searchData);

			return await DeleteSignInterpretationAttributesAsync(editionUser, attributes);
		}

		/// <summary>
		///  Deletes all attributes for the sign interpretation referred by its id
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <returns>List of ids of delete attributes</returns>
		public async Task<List<uint>> DeleteAllAttributesForSignInterpretationAsync(
				UserInfo editionUser
				, uint   signInterpretationId)
		{
			var attributes =
					await GetSignInterpretationAttributeIdsByInterpretationId(
							editionUser
							, signInterpretationId);

			return await DeleteSignInterpretationAttributesAsync(editionUser, attributes);
		}

		/// <summary>
		///  Updates the specified attribute value from the sign interpretation referred by its id
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation to be altered</param>
		/// <param name="attributeValueId">Id of attribute value to update</param>
		/// <param name="sequence">Position of the attribute in the sequential hierarchy</param>
		/// <returns>List of ids of delete attributes</returns>
		public async Task UpdateAttributeForSignInterpretationAsync(
				UserInfo editionUser
				, uint   signInterpretationId
				, uint   attributeValueId
				, byte?  sequence)
		{
			if (!sequence.HasValue)
				return;

			var searchData = new SignInterpretationAttributeDataSearchData
			{
					SignInterpretationId = signInterpretationId
					, AttributeValueId = attributeValueId
					,
			};

			var signInterpretationAttributeId =
					(await GetSignInterpretationAttributeIdsByDataAsync(editionUser, searchData))
					.FirstOrDefault();

			var attributeData = await GetSignInterpretationAttributeByIdAsync(
					editionUser
					, signInterpretationAttributeId);

			attributeData.Sequence ??= sequence;

			await UpdateSignInterpretationAttributesAsync(
					editionUser
					, signInterpretationId
					, new List<SignInterpretationAttributeData> { attributeData });
		}

		/// <summary>
		///  Deletes all existing attributes of a sign interpretation and add the new attributes to it
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <param name="newAttributes">List of new sign interpretation attributes</param>
		/// <returns>List of the new sign interpretation attributes with the new ids</returns>
		/// <exception cref="NotImplementedException"></exception>
		public async Task<List<SignInterpretationAttributeData>>
				ReplaceSignInterpretationAttributesAsync(
						UserInfo                                editionUser
						, uint                                  signInterpretationId
						, List<SignInterpretationAttributeData> newAttributes)
		{
			if ((newAttributes == null)
				|| (newAttributes.Count <= 0))
				return new List<SignInterpretationAttributeData>();

			await DeleteAllAttributesForSignInterpretationAsync(editionUser, signInterpretationId);

			return await CreateSignInterpretationAttributesAsync(
					editionUser
					, signInterpretationId
					, newAttributes);
		}

		// /// <summary>
		// /// Delete the specified attribute value from the edition
		// /// </summary>
		// /// <param name="editionUser">The edition user details object</param>
		// /// <param name="attributeValueId">The unique id of the attribute value to be deleted</param>
		// /// <returns></returns>
		// public async Task DeleteEditionAttributeValueAsync(UserInfo editionUser, uint attributeValueId)
		// {
		//
		// }

		private async Task<uint> _createOrUpdateEditionAttribute(
				UserInfo editionUser
				, string attributeName
				, string attributeDescription
				, bool   editable
				, bool   removable
				, bool   repeatable
				, bool   batchEditable
				, uint?  attributeId = null)
		{
			var createParams = new DynamicParameters();
			createParams.Add("@name", attributeName);
			createParams.Add("@description", attributeDescription);
			createParams.Add("@editable", editable);
			createParams.Add("@removable", removable);
			createParams.Add("@repeatable", repeatable);
			createParams.Add("@batch_editable", batchEditable);

			var mutateRequest = new MutationRequest(
					attributeId.HasValue
							? MutateType.Update
							: MutateType.Create
					, createParams
					, "attribute"
					, attributeId);

			var writeRequest =
					await _databaseWriter.WriteToDatabaseAsync(editionUser, mutateRequest);

			var writtenRequest = writeRequest.First();

			if (writtenRequest?.NewId == null)
			{
				throw new StandardExceptions.DataNotWrittenException(
						$"{(attributeId.HasValue ? "update" : "create")} new attribute");
			}

			return writtenRequest.NewId.Value;
		}

		private async Task _createOrUpdateEditionAttributeValue(
				UserInfo editionUser
				, uint   attributeId
				, string attributeStringValue
				, string attributeValueDescription
				, string attributeValueCss
				, uint?  attributeValueId = null)
		{
			var createParams = new DynamicParameters();
			createParams.Add("@attribute_id", attributeId);
			createParams.Add("@string_value", attributeStringValue);
			createParams.Add("@description", attributeValueDescription);

			var mutateRequest = new MutationRequest(
					attributeValueId.HasValue
							? MutateType.Update
							: MutateType.Create
					, createParams
					, "attribute_value"
					, attributeValueId);

			var writeRequest =
					await _databaseWriter.WriteToDatabaseAsync(editionUser, mutateRequest);

			var writtenRequest = writeRequest.First();

			if (writtenRequest?.NewId == null)
			{
				throw new StandardExceptions.DataNotWrittenException(
						$"{(attributeValueId.HasValue ? "update" : "create")} new attribute value");
			}

			// Check to see if a CSS value should be written, early return for no CSS string
			if (string.IsNullOrEmpty(attributeValueCss))
				return;

			var createCssParams = new DynamicParameters();

			createCssParams.Add("@attribute_value_id", writtenRequest.NewId.Value);

			createCssParams.Add("@css", attributeValueCss);

			var mutateCssRequest = new MutationRequest(
					MutateType.Create
					, createCssParams
					, "attribute_value_css");

			await _databaseWriter.WriteToDatabaseAsync(editionUser, mutateCssRequest);
		}

		/// <summary>
		///  Update the given attributes
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <param name="updateAttributes">List of Attributes with the new values</param>
		/// <returns>Returns the list of Attributes which contain the new ids</returns>
		public async Task<List<SignInterpretationAttributeData>>
				UpdateSignInterpretationAttributesAsync(
						UserInfo                                editionUser
						, uint                                  signInterpretationId
						, List<SignInterpretationAttributeData> updateAttributes)
			=> await _createOrUpdateAttributesAsync(
					editionUser
					, signInterpretationId
					, updateAttributes
					, MutateType.Update);

		/// <summary>
		///  Deletes the attributes with the given ids.
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="deleteAttributeIds">List of ids of the attributes to be deleted</param>
		/// <returns>The list of the ids of deleted Attributes or empty list if the given list was null.</returns>
		/// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
		public async Task<List<uint>> DeleteSignInterpretationAttributesAsync(
				UserInfo     editionUser
				, List<uint> deleteAttributeIds)
		{
			if (deleteAttributeIds == null)
				return new List<uint>();

			var requests = deleteAttributeIds.Select(
													 id => new MutationRequest(
															 MutateType.Delete
															 , new DynamicParameters()
															 , "sign_interpretation_attribute"
															 , id))
											 .ToList();

			var writeResults = await _databaseWriter.WriteToDatabaseAsync(editionUser, requests);

			// Check whether for each attribute a request was processed.
			if (writeResults.Count != deleteAttributeIds.Count)
			{
				throw new StandardExceptions.DataNotWrittenException(
						"delete sign interpretation attribute");
			}

			return deleteAttributeIds;
		}

		/// <summary>
		///  Gets the attribute with the given id
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationAttributeId">Id of the attribute to be retrieved</param>
		/// <returns>Sign interpretation attribute with the given id</returns>
		/// <exception cref="DataNotFoundException"></exception>
		public async Task<SignInterpretationAttributeData> GetSignInterpretationAttributeByIdAsync(
				UserInfo editionUser
				, uint   signInterpretationAttributeId)
		{
			var searchData = new SignInterpretationAttributeDataSearchData
			{
					SignInterpretationAttributeId = signInterpretationAttributeId,
			};

			var result = await GetSignInterpretationAttributesByDataAsync(editionUser, searchData);

			if (result.Count != 1)
			{
				throw new StandardExceptions.DataNotFoundException(
						"sign interpretation attribute"
						, signInterpretationAttributeId);
			}

			return result.First();
		}

		/// <summary>
		///  Retrieves all sign interpretation attribute ids which match the data provided by searchData
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="dataSearchData">Sign interpretation attribute search data object</param>
		/// <returns>List of sign interpretation attribute ids - if nothing had been found the list is empty.</returns>
		public async Task<List<uint>> GetSignInterpretationAttributeIdsByDataAsync(
				UserInfo                                    editionUser
				, SignInterpretationAttributeDataSearchData dataSearchData)
		{
			var query = GetSignInterpretationAttributeIdsByDataQuery.GetQuery.Replace(
					"@WhereData"
					, dataSearchData.getSearchParameterString());

			using (var connection = OpenConnection())
			{
				var result =
						await connection.QueryAsync<uint>(query, new { editionUser.EditionId });

				return result == null
						? new List<uint>()
						: result.ToList();
			}
		}

		/// <summary>
		///  Gets all attribute ids of a the sign interpretation referred by its id
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <returns>List of sign interpretation attribute idss</returns>
		public async Task<List<uint>> GetSignInterpretationAttributeIdsByInterpretationId(
				UserInfo editionUser
				, uint   signInterpretationId)
		{
			var searchData = new SignInterpretationAttributeDataSearchData
			{
					SignInterpretationId = signInterpretationId,
			};

			return await GetSignInterpretationAttributeIdsByDataAsync(editionUser, searchData);
		}

		#region Private functions

		/// <summary>
		///  Creates and executes create or update mutation requests for the given attributes
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <param name="attributes">List of attributes</param>
		/// <param name="action">Mutate type create or update</param>
		/// <returns>
		///  List of set attributes with the new sign interpretation attribute id set or empty list
		///  if the list of attributes had been null.
		/// </returns>
		/// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
		private async Task<List<SignInterpretationAttributeData>> _createOrUpdateAttributesAsync(
				UserInfo                                editionUser
				, uint                                  signInterpretationId
				, List<SignInterpretationAttributeData> attributes
				, MutateType                            action)
		{
			// Let's test whether a list of new attributes is provided and contains attributes
			if (!(attributes?.Count > 0))
				return new List<SignInterpretationAttributeData>();

			// Create requests for the attributes
			var requests = new List<MutationRequest>();

			foreach (var attribute in attributes)
			{
				var signInterpretationAttributeParameters = new DynamicParameters();

				signInterpretationAttributeParameters.Add(
						"@sign_interpretation_id"
						, signInterpretationId);

				signInterpretationAttributeParameters.Add(
						"@attribute_value_id"
						, attribute.AttributeValueId);

				signInterpretationAttributeParameters.Add("@sequence", attribute.Sequence);

				var signInterpretationAttributeRequest = new MutationRequest(
						action
						, signInterpretationAttributeParameters
						, "sign_interpretation_attribute"
						, action == MutateType.Update
								? attribute.SignInterpretationAttributeId
								: null);

				requests.Add(signInterpretationAttributeRequest);
			}

			var writeResults = await _databaseWriter.WriteToDatabaseAsync(editionUser, requests);

			// Check whether for each attribute a request was processed.
			if (writeResults.Count != attributes.Count)
			{
				var actionName = action == MutateType.Create
						? "create"
						: "update";

				throw new StandardExceptions.DataNotWrittenException(
						$"{actionName} sign interpretation attribute");
			}

			// A quick hack to ensure that an attribute and it's value has the edition set as owner
			using (var connection = OpenConnection())
			{

				// Now set the new Ids
				for (var i = 0; i < attributes.Count; i++)
				{
					var newId = writeResults[i].NewId;

					if (newId.HasValue)
						attributes[i].SignInterpretationAttributeId = newId.Value;

					connection.Execute(
							@"insert ignore into attribute_value_owner
					(attribute_value_id, edition_editor_id, edition_id)
					values (@AttributeValueId, @EditionEditorId, @EditionId)"
							, new
							{
									AttributeValueId = attributes[i].AttributeValueId
									, EditionEditorId = editionUser.EditionEditorId
									, EditionId = editionUser.EditionId
									,
							});
					connection.Execute(
							@"insert ignore into attribute_owner
					(attribute_id, edition_editor_id, edition_id)
					values (@AttributeId, @EditionEditorId, @EditionId)"
							, new
							{
									AttributeId = attributes[i].AttributeId
									, @EditionEditorId = editionUser.EditionEditorId
									, @EditionId = editionUser.EditionId
									,
							});
				}
			}

			// Now return the list of new attributes which now also contains the the new ids.
			return attributes;

			//If no list of new attributes had been provided return an empty list.
		}

		#endregion
	}
}
