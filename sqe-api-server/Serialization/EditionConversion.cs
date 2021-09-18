using System.Collections.Generic;
using System.Linq;
using SQE.API.DTO;
using SQE.API.Server.Services;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Serialization
{
	public static partial class ExtensionsDTO
	{
		public static List<EditionDTO> ToDTO(this IEnumerable<Edition> model)
			=> model.Select(x => x.ToDTO()).ToList();

		public static EditionDTO ToDTO(this Edition model)
		{
			return new EditionDTO
			{
					id = model.EditionId
					, name = model.Name
					, manuscriptId = model.ManuscriptId
					, editionDataEditorId = model.EditionDataEditorId
					, metrics =
							new EditionManuscriptMetricsDTO
							{
									editorId = model.ManuscriptMetricsEditor
									, height = model.Height
									, width = model.Width
									, ppi = model.PPI
									, xOrigin = model.XOrigin
									, yOrigin = model.YOrigin
									,
							}
					, permission = model.Permission.ToDTO()
					, owner = UserService.UserModelToDto(model.Owner)
					, thumbnailUrl = model.Thumbnail
					, locked = model.Locked
					, isPublic = model.IsPublic
					, lastEdit = model.LastEdit
					, copyright =
							model.Copyright
							?? Licence.printLicence(
									model.CopyrightHolder
									, model.Collaborators
									, model.Editors)
					, shares = model.Editors.Select(
											x => new DetailedEditorRightsDTO
											{
													email = x.EditorEmail
													, editionId = model.EditionId
													, isAdmin = x.IsAdmin
													, mayLock = x.MayLock
													, mayRead = x.MayRead
													, mayWrite = x.MayWrite
													,
											})
									.ToList()
					,
			};
		}

		public static PermissionDTO ToDTO(this Permission model) => new PermissionDTO
		{
				isAdmin = model.IsAdmin
				, mayWrite = model.MayWrite
				, mayRead = model.MayRead
				,
		};

		public static TextFragmentSearchResponseDTO ToDTO(this TextFragmentSearch tfs)
			=> new TextFragmentSearchResponseDTO
			{
					editionId = tfs.EditionId
					, id = tfs.TextFragmentId
					, name = tfs.Name
					, editionName = tfs.EditionName
					, editionEditors = new List<string> { tfs.Editors }
					,
			};

		public static TextFragmentSearchResponseListDTO ToDTO(
				this IEnumerable<TextFragmentSearch> tfsl) => new TextFragmentSearchResponseListDTO
		{
				textFragments = tfsl.Select(x => x.ToDTO()).ToList(),
		};

		public static EditionManuscriptMetadataDTO ToDTO(this EditionMetadata em)
			=> new EditionManuscriptMetadataDTO
			{
					abbreviation = em.Abbreviation
					, composition = em.Composition
					, compositionType = em.CompositionType
					, copy = em.Copy
					, frag = em.Frag
					, language = em.Language
					, manuscript = em.Manuscript
					, manuscriptType = em.ManuscriptType
					, material = em.Material
					, otherIdentifications = em.OtherIdentifications
					, publicationNumber = em.PublicationNumber
					, plate = em.Plate
					, site = em.Site
					, period = em.Period
					, script = em.Script
					, publication = em.Publication
					,
			};
	}
}
