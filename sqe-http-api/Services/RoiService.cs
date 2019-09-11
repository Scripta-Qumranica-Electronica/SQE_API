using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Services
{
	public interface IRoiService
	{
		Task<InterpretationRoiDTO> CreateRoiAsync(EditionUserInfo editionUser, SetInterpretationRoiDTO newRois);

		Task<InterpretationRoiDTOList>
			CreateRoisAsync(EditionUserInfo editionUser, SetInterpretationRoiDTOList newRois);

		Task<InterpretationRoiDTO> UpdateRoiAsync(EditionUserInfo editionUser,
			uint roiId,
			SetInterpretationRoiDTO updatedRoi);
	}

	public class RoiService : IRoiService
	{
		private readonly IRoiRepository _roiRepository;

		public RoiService(IRoiRepository roiRepository)
		{
			_roiRepository = roiRepository;
		}

		public async Task<InterpretationRoiDTO> CreateRoiAsync(EditionUserInfo editionUser,
			SetInterpretationRoiDTO newRois)
		{
			return (await CreateRoisAsync(
				editionUser,
				new SetInterpretationRoiDTOList {rois = new List<SetInterpretationRoiDTO> {newRois}}
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
							position = x.Position,
							shape = x.Shape,
							valuesSet = x.ValuesSet
						}
					)
					.ToList()
			};
		}

		public async Task<InterpretationRoiDTO> UpdateRoiAsync(EditionUserInfo editionUser,
			uint roiId,
			SetInterpretationRoiDTO updatedRoi)
		{
			return null;
		}
	}
}