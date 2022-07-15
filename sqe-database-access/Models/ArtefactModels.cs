using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class EditionArtefact
	{
		public uint   EditionId     { get; set; }
		public uint   ArtefactId    { get; set; }
		public uint   PixelsPerInch { get; set; }
		public string Url           { get; set; }
	}

	public class ArtefactDataModel
	{
		public uint   ArtefactId { get; set; }
		public string Name       { get; set; }
	}

	public class ArtefactModel : ArtefactDataModel
	{
		public uint     ArtefactDataEditorId { get; set; }
		public string   Mask                 { get; set; }
		public uint     MaskEditorId         { get; set; }
		public decimal? Scale                { get; set; }
		public decimal? Rotate               { get; set; }
		public int?     TranslateX           { get; set; }
		public int?     TranslateY           { get; set; }
		public bool?    Mirror               { get; set; }
		public uint     PositionEditorId     { get; set; }
		public int?     ZIndex               { get; set; }
		public byte     CatalogSide          { get; set; }
		public uint     ImageId              { get; set; }
		public uint     ImageCatalogId       { get; set; }
		public string   ImagedObjectId       { get; set; }
		public string   WorkStatusMessage    { get; set; }
	}

	public class ArtefactGroupEntry
	{
		public uint   ArtefactGroupId   { get; set; }
		public string ArtefactGroupName { get; set; }
		public uint   ArtefactId        { get; set; }
	}

	public class ArtefactGroup
	{
		public uint       ArtefactGroupId { get; set; }
		public string     ArtefactName    { get; set; }
		public List<uint> ArtefactIds     { get; set; }
	}

	internal class ArtefactGroupMember
	{
		public uint ArtefactGroupMemberId { get; set; }
		public uint ArtefactGroupId       { get; set; }
		public uint ArtefactId            { get; set; }
	}

	internal class ArtefactGroupData
	{
		public uint   ArtefactGroupDataId { get; set; }
		public uint   ArtefactGroupId     { get; set; }
		public string Name                { get; set; }
	}
}
