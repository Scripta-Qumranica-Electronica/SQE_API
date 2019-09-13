using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Services
{
	public interface IRoiService
	{
		Task<InterpretationRoiDTO> GetRoiAsync(EditionUserInfo editionUser, uint roiId);

		Task<InterpretationRoiDTO> CreateRoiAsync(EditionUserInfo editionUser, SetInterpretationRoiDTO newRois);

		Task<InterpretationRoiDTOList>
			CreateRoisAsync(EditionUserInfo editionUser, SetInterpretationRoiDTOList newRois);

		Task<InterpretationRoiDTO> UpdateRoiAsync(EditionUserInfo editionUser, uint roiId,
			SetInterpretationRoiDTO updatedRoi);

		Task<InterpretationRoiDTOList> UpdateRoisAsync(EditionUserInfo editionUser,
			InterpretationRoiDTOList updatedRois);

		Task<NoContentResult> DeleteRoisAsync(EditionUserInfo editionUser,
			List<uint> deleteRois);

		Task<NoContentResult> DeleteRoiAsync(EditionUserInfo editionUser,
			uint deleteRoi);
	}

	public class RoiService : IRoiService
	{
		private readonly IRoiRepository _roiRepository;

		public RoiService(IRoiRepository roiRepository)
		{
			_roiRepository = roiRepository;
		}

		public async Task<InterpretationRoiDTO> GetRoiAsync(EditionUserInfo editionUser, uint roiId)
		{
			var roi = await _roiRepository.GetSignInterpretationRoiByIdAsync(editionUser, roiId);
			return new InterpretationRoiDTO()
			{
				artefactId = roi.ArtefactId,
				editorId = roi.SignInterpretationRoiAuthor,
				exceptional = roi.Exceptional,
				interpretationRoiId = roi.SignInterpretationRoiId,
				position = roi.Position,
				shape = roi.Shape,
				signInterpretationId = roi.SignInterpretationId,
				valuesSet = roi.ValuesSet
			};
		}

		public async Task<InterpretationRoiDTO> CreateRoiAsync(EditionUserInfo editionUser,
			SetInterpretationRoiDTO newRois)
		{
			return (await CreateRoisAsync(
				editionUser,
				new SetInterpretationRoiDTOList { rois = new List<SetInterpretationRoiDTO> { newRois } }
			)).rois.FirstOrDefault();
		}

		public async Task<InterpretationRoiDTOList> CreateRoisAsync(EditionUserInfo editionUser,
			SetInterpretationRoiDTOList newRois)
		{
			return new InterpretationRoiDTOList
			{
				rois = (
						await _roiRepository.CreateRoisAsync( // Write new rois
							editionUser,
							newRois.rois
								.Select( // Serialize the SetInterpretationRoiDTOList to a List of SetSignInterpretationROI
									x => new SetSignInterpretationROI
									{
										SignInterpretationId = x.signInterpretationId,
										ArtefactId = x.artefactId,
										Exceptional = x.exceptional,
										Position = x.position,
										Shape = x.shape,
										ValuesSet = x.valuesSet
									}
								)
								.ToList()
						)
					)
					.Select( // Serialize the ROI Repository response to a List of InterpretationRoiDTO
						x => new InterpretationRoiDTO
						{
							artefactId = x.ArtefactId,
							editorId = x.SignInterpretationRoiAuthor,
							exceptional = x.Exceptional,
							interpretationRoiId = x.SignInterpretationRoiId,
							signInterpretationId = x.SignInterpretationId,
							position = x.Position,
							shape = x.Shape,
							valuesSet = x.ValuesSet
						}
					)
					.ToList()
			};
		}

		public async Task<InterpretationRoiDTO> UpdateRoiAsync(EditionUserInfo editionUser, uint roiId,
			SetInterpretationRoiDTO updatedRoi)
		{
			var fullUpdatedRoi = new InterpretationRoiDTO()
			{
				artefactId = updatedRoi.artefactId,
				interpretationRoiId = roiId,
				signInterpretationId = updatedRoi.signInterpretationId,
				exceptional = updatedRoi.exceptional,
				valuesSet = updatedRoi.valuesSet,
				position = updatedRoi.position,
				shape = updatedRoi.shape
			};
			return (await UpdateRoisAsync(
				editionUser,
				new InterpretationRoiDTOList { rois = new List<InterpretationRoiDTO> { fullUpdatedRoi } }
			)).rois.FirstOrDefault();
		}

		public async Task<InterpretationRoiDTOList> UpdateRoisAsync(EditionUserInfo editionUser,
			InterpretationRoiDTOList updatedRois)
		{
			return new InterpretationRoiDTOList
			{
				rois = (
						await _roiRepository.UpdateRoisAsync( // Write new rois
							editionUser,
							updatedRois.rois
								.Select( // Serialize the InterpretationRoiDTOList to a List of SignInterpretationROI
									x => new SignInterpretationROI
									{
										SignInterpretationRoiId = x.interpretationRoiId,
										SignInterpretationId = x.signInterpretationId,
										ArtefactId = x.artefactId,
										Exceptional = x.exceptional,
										Position = x.position,
										Shape = x.shape,
										ValuesSet = x.valuesSet
									}
								)
								.ToList()
						)
					)
					.Select( // Serialize the ROI Repository response to a List of InterpretationRoiDTO
						x => new InterpretationRoiDTO
						{
							artefactId = x.ArtefactId,
							editorId = x.SignInterpretationRoiAuthor,
							exceptional = x.Exceptional,
							interpretationRoiId = x.SignInterpretationRoiId,
							signInterpretationId = x.SignInterpretationId,
							position = x.Position,
							shape = x.Shape,
							valuesSet = x.ValuesSet
						}
					)
					.ToList()
			};
		}

		public async Task<NoContentResult> DeleteRoiAsync(EditionUserInfo editionUser,
			uint deleteRoi)
		{
			return await DeleteRoisAsync(editionUser, new List<uint> { deleteRoi });
		}

		public async Task<NoContentResult> DeleteRoisAsync(EditionUserInfo editionUser,
			List<uint> deleteRois)
		{
			await _roiRepository.DeletRoisAsync(editionUser, deleteRois);
			return new NoContentResult();
		}
	}
}