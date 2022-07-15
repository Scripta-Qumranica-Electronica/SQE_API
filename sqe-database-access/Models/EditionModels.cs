using System;
using System.Collections.Generic;
using System.Linq;
using SQE.DatabaseAccess.Helpers;

namespace SQE.DatabaseAccess.Models
{
	public class Edition
	{
		public uint                        EditionId               { get; set; }
		public string                      Name                    { get; set; }
		public uint                        EditionDataEditorId     { get; set; }
		public uint                        ManuscriptId            { get; set; }
		public uint                        Width                   { get; set; }
		public uint                        Height                  { get; set; }
		public int                         XOrigin                 { get; set; }
		public int                         YOrigin                 { get; set; }
		public uint                        PPI                     { get; set; }
		public uint                        ManuscriptMetricsEditor { get; set; }
		public Permission                  Permission              { get; set; }
		public string                      Thumbnail               { get; set; }
		public bool                        Locked                  { get; set; }
		public bool                        IsPublic                { get; set; }
		public DateTime?                   LastEdit                { get; set; }
		public User                        Owner                   { get; set; }
		public string                      Copyright               { get; set; }
		public string                      CopyrightHolder         { get; set; }
		public string                      Collaborators           { get; set; }
		public List<EditorWithPermissions> Editors                 { get; set; }
	}

	public class Permission
	{
		public bool MayRead  { get; set; }
		public bool MayWrite { get; set; }
		public bool MayLock  { get; set; }
		public bool IsAdmin  { get; set; }
	}

	public class EditorPermissions : Permission
	{
		public string Email { get; set; }
	}

	public class DetailedEditorPermissions : EditorPermissions
	{
		public uint     EditionId   { get; set; }
		public string   EditionName { get; set; }
		public DateTime Date        { get; set; }
	}

	public class DetailedEditorRequestPermissions : DetailedEditorPermissions
	{
		public string EditorForename     { get; set; }
		public string EditorSurname      { get; set; }
		public string EditorOrganization { get; set; }
	}

	public class DetailedEditorInvitationPermissions : DetailedEditorPermissions
	{
		public string AdminForename     { get; set; }
		public string AdminSurname      { get; set; }
		public string AdminOrganization { get; set; }
		public Guid   Token             { get; set; }
	}

	public class DetailedEditionPermission : EditorPermissions
	{
		public uint EditionId { get; set; }
	}

	public class TextEdition
	{
		public readonly List<TextFragmentData> fragments = new List<TextFragmentData>();

		public uint   manuscriptId     { get; set; }
		public string editionName      { get; set; }
		public string copyrightHolder  { get; set; }
		public string collaborators    { get; set; }
		public uint   manuscriptAuthor { get; set; }
		public string licence          { get; set; }

		/// <summary>
		///  Call this, if you want the the licence to be added on the output.
		///  If the collaborators field is empty, it will be populated from the list of editors.
		/// </summary>
		public void AddLicence(List<EditorInfo> editors = null)
		{
			var collab = collaborators;

			if (string.IsNullOrEmpty(collab))
			{
				collab = editors == null
						? copyrightHolder
						: string.Join(
								", "
								, editors.Select(
										x => x.Forename
											 + (!string.IsNullOrEmpty(x.Forename)
												&& !string.IsNullOrEmpty(x.Surname)
													 ? " "
													 : "")
											 + x.Surname
											 + (string.IsNullOrEmpty(x.Organization)
													 ? ""
													 : " (" + x.Organization + ")")));
			}

			licence = Licence.printLicence(copyrightHolder, collab);
		}
	}

	public class UpdateEntity
	{
		public UpdateEntity(uint oldId, uint newId)
		{
			this.oldId = oldId;
			this.newId = newId;
		}

		public uint oldId { get; set; }
		public uint newId { get; set; }
	}
}
