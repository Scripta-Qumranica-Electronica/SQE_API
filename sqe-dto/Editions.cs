using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SQE.API.DTO
{
	// TODO: get rid of owner and instead provide a list of editors (don't give the email addresses)
	public class EditionDTO
	{
		[Required]
		public uint id { get; set; }

		[Required]
		public string name { get; set; }

		[Required]
		public uint editionDataEditorId { get; set; }

		[Required]
		public PermissionDTO permission { get; set; }

		[Required]
		public UserDTO owner { get; set; }

		public string thumbnailUrl { get; set; }

		[Required]
		public List<DetailedEditorRightsDTO> shares { get; set; }

		[Required]
		public EditionManuscriptMetricsDTO metrics { get; set; }

		[Required]
		public bool locked { get; set; }

		[Required]
		public bool isPublic { get; set; }

		public DateTime? lastEdit { set; get; }

		[Required]
		public string copyright { get; set; }
	}

	public class EditionGroupDTO
	{
		[Required]
		public EditionDTO primary { get; set; }

		[Required]
		public IEnumerable<EditionDTO> others { get; set; }
	}

	public class EditionListDTO
	{
		[Required]
		public List<List<EditionDTO>> editions { get; set; }
	}

	public class PermissionDTO
	{
		[Required]
		public bool mayRead { get; set; }

		[Required]
		public bool mayWrite { get; set; }

		[Required]
		public bool isAdmin { get; set; }
	}

	public class UpdateEditorRightsDTO : PermissionDTO
	{
		[Required]
		public bool mayLock { get; set; }
	}

	public class InviteEditorDTO : UpdateEditorRightsDTO
	{
		[Required]
		[RegularExpression(
				@"^.*@.*\..*$"
				, ErrorMessage = "The email address appears to be improperly formatted")]
		public string email { get; set; }
	}

	public class DetailedEditorRightsDTO : UpdateEditorRightsDTO
	{
		[Required]
		[RegularExpression(
				@"^.*@.*\..*$"
				, ErrorMessage = "The email address appears to be improperly formatted")]
		public string email { get; set; }

		[Required]
		public uint editionId { get; set; }
	}

	public class DetailedUpdateEditorRightsDTO : UpdateEditorRightsDTO
	{
		[Required]
		public uint editionId { get; set; }

		[Required]
		public string editionName { get; set; }

		[Required]
		public DateTime date { get; set; }
	}

	public class AdminEditorRequestDTO : DetailedUpdateEditorRightsDTO
	{
		public string editorName { get; set; }

		[Required]
		public string editorEmail { get; set; }
	}

	public class EditorInvitationDTO : DetailedUpdateEditorRightsDTO
	{
		[Required]
		public Guid token { get; set; }

		[Required]
		public string requestingAdminName { get; set; }

		[Required]
		public string requestingAdminEmail { get; set; }
	}

	public class EditorInvitationListDTO
	{
		[Required]
		public List<EditorInvitationDTO> editorInvitations { get; set; }
	}

	public class AdminEditorRequestListDTO
	{
		[Required]
		public List<AdminEditorRequestDTO> editorRequests { get; set; }
	}

	public class TextEditionDTO
	{
		[Required]
		public uint manuscriptId { get; set; }

		[Required]
		public string editionName { get; set; }

		[Required]
		public uint editorId { get; set; }

		[Required]
		public string licence { get; set; }

		[Required]
		public Dictionary<string, EditorDTO> editors { get; set; }

		[Required]
		public List<TextFragmentDTO> textFragments { get; set; }
	}

	public class DeleteTokenDTO
	{
		[Required]
		public uint editionId { get; set; }

		[Required]
		public string token { get; set; }
	}

	public class CommentaryCreateDTO
	{
		public string commentary { get; set; }
	}

	public class CommentaryDTO : CommentaryCreateDTO
	{
		[Required]
		public uint creatorId { get; set; }

		[Required]
		public uint editorId { get; set; }
	}

	/// <summary>
	///  This is a list of all entities in an edition, including the edition itself.
	///  This is initially intended to be used with the DeleteDTO object
	/// </summary>
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum EditionEntities
	{
		edition
		, artefact
		, artefactGroup
		, attribute
		, textFragment
		, line
		, signInterpretation
		, roi
		,
	}

	public class DeleteDTO
	{
		public DeleteDTO(EditionEntities entity, List<uint> ids)
		{
			this.entity = entity;
			this.ids = ids;
		}

		public DeleteDTO(EditionEntities entity, uint id)
		{
			this.entity = entity;
			ids = new List<uint> { id };
		}

		public DeleteDTO() { }

		[JsonConverter(typeof(JsonStringEnumConverter))]
		[Required]
		public EditionEntities entity { get; set; }

		[Required]
		public List<uint> ids { get; set; }
	}

	#region Request DTO's

	public class EditionUpdateRequestDTO : EditionCopyDTO
	{
		/// <summary>
		///  Metadata to be added to or updated for an edition
		/// </summary>
		/// <param name="name">Name of the work being edited. If null, the name is left the same as it currently is.</param>
		/// <param name="copyrightHolder">
		///  Name of the copyright holder. This is a required field.
		///  If left null, then the name of the first editor of the edition is input.
		/// </param>
		/// <param name="collaborators">
		///  Name(s) of the collaborator(s) working officially on the edition.
		///  This may be null, in which case the names of all edition editors are collected automatically
		///  and added to the edition license.
		/// </param>
		/// <param name="length">Editor's estimated metrics of the manuscript (a null object will use the database defaults)</param>
		public EditionUpdateRequestDTO(
				string                              name
				, string                            copyrightHolder
				, string                            collaborators
				, UpdateEditionManuscriptMetricsDTO metrics = null)
		{
			this.name = name;
			this.copyrightHolder = copyrightHolder;
			this.collaborators = collaborators;
			this.metrics = metrics;
		}

		public EditionUpdateRequestDTO() : this(string.Empty, string.Empty, string.Empty) { }

		public UpdateEditionManuscriptMetricsDTO metrics { get; set; }
	}

	public class EditionCopyDTO
	{
		public EditionCopyDTO(string name, string copyrightHolder, string collaborators)
		{
			this.name = name;
			this.collaborators = collaborators;
			this.copyrightHolder = copyrightHolder;
		}

		public EditionCopyDTO() : this(string.Empty, string.Empty, string.Empty) { }

		[StringLength(
				255
				, MinimumLength = 1
				, ErrorMessage =
						"The name of the edition must be either null for no change or between 1 and 255 characters long")]
		public string name { get; set; }

		public string copyrightHolder { get; set; }
		public string collaborators   { get; set; }
	}

	public class UpdateEditionManuscriptMetricsDTO
	{
		[Required]
		public uint width { get; set; }

		[Required]
		public uint height { get; set; }

		[Required]
		public int xOrigin { get; set; }

		[Required]
		public int yOrigin { get; set; }
	}

	public class EditionManuscriptMetricsDTO : UpdateEditionManuscriptMetricsDTO
	{
		[Required]
		public uint ppi { get; set; }

		[Required]
		public uint editorId { get; set; }
	}

	#endregion Request DTO's
}
