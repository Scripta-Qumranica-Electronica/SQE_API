using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace sqe_api
{
	public class ImageData
	{
		public int Dpi;
		public int NativeHeight;
		public int NativeWidth;
	}

	/// <summary>
	///  Provides an easy access to the SQE-Database using the sqe-api of the project.
	/// </summary>
	public class SqeApi
	{
		private readonly ArtefactRepository _artefactRep;
		private readonly EditionRepository  _editionRep;
		private readonly uint?              _userId;
		private readonly UserInfo           _userInfo;
		private readonly UserRepository     _userRep;

		/// <summary>
		///  Initialze with the id of the user to be used.
		/// </summary>
		/// <param name="userId">The id of the user to be used</param>
		public SqeApi(uint? userId)
		{
			_userId = userId;

			var sqeConfiguration = new ConfigurationBuilder()
								   .SetBasePath(Directory.GetCurrentDirectory())
								   .AddJsonFile("appsettings.json", false)
								   .Build();

			var dbw = new DatabaseWriter(sqeConfiguration);
			_editionRep = new EditionRepository(sqeConfiguration, dbw);
			var attrRep = new AttributeRepository(sqeConfiguration, dbw);

			var commRep =
					new SignInterpretationCommentaryRepository(sqeConfiguration, dbw, attrRep);

			var roiRep = new RoiRepository(sqeConfiguration, dbw);
			_artefactRep = new ArtefactRepository(sqeConfiguration, dbw);

			var matRepository = new SignStreamMaterializationRepository(sqeConfiguration)
			{
					RunMaterialization = false,
			};

			var signIntRep = new SignInterpretationRepository(
					sqeConfiguration
					, attrRep
					, commRep
					, roiRep
					, dbw);

			TextRep = new ExpandedTextRepository(
					sqeConfiguration
					, dbw
					, attrRep
					, signIntRep
					, commRep
					, roiRep
					, _artefactRep
					, matRepository);

			_userRep = new UserRepository(sqeConfiguration);
			_userInfo = new UserInfo(userId, null, _userRep);
		}

		public  ExpandedTextRepository TextRep      { get; }
		private IDbConnection          DbConnection => TextRep.GetConnection();

		public void SetEditionId(uint editionId)
		{
			_userInfo.SetEditionId(editionId);
		}

		/// <summary>
		///  Retrieves the data of an edition and returns it as an instance of Scroll.
		///  If no matching edition is found, an exception is thrown.
		/// </summary>
		/// <param name="scrollName">Name of the edition (eg. "CD")</param>
		/// <returns>Scroll</returns>
		/// <exception cref="Exception"></exception>
		public Scroll getEdition(string scrollName)
		{
			// Find the edition with the given scrollName
			var edition = _editionRep.ListEditionsAsync(_userId, null)
									 .Result.ToList()
									 .Find(x => x.Name == scrollName);

			if (edition == null)
				throw new Exception($"Edition {scrollName} not found.");

			// If en edition is found create from it an instance of Scroll anr return it.
			return new Scroll(edition, this, new UserInfo(_userId, edition.EditionId, _userRep));
		}

		public void DeleteSign(uint signId)
		{
			Console.WriteLine(signId);
			TextRep.RemoveSignAsync(_userInfo, signId);
		}

		public uint CreateArtefact(SourceFileInfo fileInfo)
		{
			var oldArteFactId = GetArtefactId(fileInfo.SqeImageId, fileInfo.FileName);
			_artefactRep.DeleteArtefactAsync(_userInfo, oldArteFactId);

			var data = TextRep.GetConnection()
							  .QueryFirst<ImageData>(
									  $@"select native_width as NativeWidth,
											native_height as NativeHeight,
											dpi as Dpi
											from SQE_image where sqe_image_id = {
												fileInfo.SqeImageId
											}");

			var scale = (decimal) 1215 / data.Dpi;
			var w = data.NativeWidth * scale;
			var h = data.NativeHeight * scale;
			var x = (decimal) 0.0;

			if (fileInfo.ImagePart == imagePart.Right)
				x = w / 2;
			else if (fileInfo.ImagePart == imagePart.Left)
				w /= 2;

			var shape = $"POLYGON(({x} {0},"
						+ $"{w} {0},"
						+ $"{w} {h},"
						+ $"{x} {h},"
						+ $"{x} {0}))";

			return _artefactRep.CreateNewArtefactAsync(
									   _userInfo
									   , fileInfo.SqeImageId
									   , shape
									   , fileInfo.ColName
									   , null
									   , decimal.Zero
									   , null
									   , null
									   , null
									   , null
									   , false)
							   .Result;
		}

		public uint GetArtefactId(uint sqeImageId, string name) => TextRep.GetConnection()
																		  .QueryFirstOrDefault<
																				  uint>(
																				  "select artefact_id"
																				  + " from artefact_shape"
																				  + " join artefact_shape_owner using (artefact_shape_id)"
																				  + " join artefact_data using (artefact_id)"
																				  + $" where sqe_image_id={sqeImageId}"
																				  + $" and artefact_shape_owner.edition_id={_userInfo.EditionId}"
																				  + $" and artefact_data.name =\"{name}\"");

		public SignData CreateSigns(
				uint             lineId
				, List<SignData> signData
				, List<uint>     anchorsBefore
				, List<uint>     anchorsAfter
				, uint           artefactId) => _createSigns(
				lineId
				, signData
				, anchorsBefore
				, anchorsAfter
				, null
				, artefactId);

		public SignData UpdateSigns(
				uint             lineId
				, List<SignData> signData
				, List<uint>     anchorsBefore
				, List<uint>     anchorsAfter
				, uint?          signInterpretationId
				, uint           artefactId) => _createSigns(
				lineId
				, signData
				, anchorsBefore
				, anchorsAfter
				, signInterpretationId
				, artefactId);

		public SignData _createSigns(
				uint             lineId
				, List<SignData> signData
				, List<uint>     anchorsBefore
				, List<uint>     anchorsAfter
				, uint?          signInterpretationId
				, uint           artefactId)
		{
			//foreach (var roi in signData.SelectMany(
			//		signSingleData => signSingleData.SignInterpretations.SelectMany(
			//				interpreation => interpreation.SignInterpretationRois)))
			//	roi.ArtefactId = artefactId;

			var newData = TextRep.CreateSignsWithInterpretationsAsync(
										 _userInfo
										 , lineId
										 , signData
										 , anchorsBefore
										 , anchorsAfter
										 , signInterpretationId
										 , true
										 , false)
								 .Result.NewSigns;

			return newData.Last();
		}

		public Scroll GetEdition(string oldScrollName, string newScrollName, bool reset)
		{
			var editionId = _geteditionId(newScrollName, _userId.GetValueOrDefault());

			if (reset && editionId != 0)
			{
				SetEditionId(editionId);
				var token = _editionRep.GetArchiveToken(_userInfo).Result;
				var result = _editionRep.ArchiveEditionAsync(_userInfo, token);
				editionId = 0;
			}

			if (editionId == 0)
			{
				Console.WriteLine($"Create new Edition from {oldScrollName}");
				editionId = _geteditionId(oldScrollName, 1);

				if (editionId == 0)
					throw new Exception($"No editions called {oldScrollName} found.");

				editionId = _editionRep.CopyEditionAsync(
											   new UserInfo(_userId, editionId, _userRep)
											   , newScrollName)
									   .Result;

				Console.WriteLine($"New edition id {editionId} ");
			}

			SetEditionId(editionId);

			var edition = _editionRep.ListEditionsAsync(_userId, editionId)
									 .Result.ToList()
									 .Find(x => x.EditionId == editionId);

			return new Scroll(edition, this, new UserInfo(_userId, edition.EditionId, _userRep));
		}

		public uint _geteditionId(string scrollName, uint userId)
			=> DbConnection.QueryFirstOrDefault<uint>(
					@"select manuscript_data_owner.edition_id
								from manuscript_data
									join manuscript_data_owner using (manuscript_data_id)
									join edition_editor using (edition_editor_id)
								where name like @ScrollName
									and user_id = @UserId"
					, new { ScrollName = scrollName, UserId = userId });

		public List<SignInterpretationData> AddSignInterpretations(
				uint                           signId
				, List<SignInterpretationData> signInterpretations
				, List<uint>                   anchorsBefore
				, List<uint>                   anchorsAfter) => TextRep.AddSignInterpretationsAsync(
																			   _userInfo
																			   , signId
																			   , signInterpretations
																			   , anchorsBefore
																			   , anchorsAfter
																			   , true)
																	   .Result;

		public void DeleteSignInterpretations(List<SignInterpretationData> signInterpretations)
		{
			foreach (var siData in signInterpretations)
			{
				TextRep.RemoveSignInterpretationAsync(
						_userInfo
						, siData.SignInterpretationId.GetValueOrDefault()
						, false
						, false);
			}
		}

		public void UpdateSignInterpretationsData(SignInterpretationData signInterpretationData)
		{
			TextRep.UpdateSignInterpretationDataAsync(_userInfo, signInterpretationData);
		}

		public void PrependLine(uint fragmentId, LineData lineData)
		{
			TextRep.PrependLineAsync(_userInfo, fragmentId, lineData);
		}

		public void InsertLine(uint fragmentId, LineData lineData, uint previousLineId)
		{
			TextRep.AppendLineAsync(
					_userInfo
					, fragmentId
					, lineData
					, previousLineId);
		}
	}
}
