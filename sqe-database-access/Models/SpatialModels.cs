using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class ScriptTextFragment
	{
		public string           TextFragmentName { get; set; }
		public uint             TextFragmentId   { get; set; }
		public List<ScriptLine> Lines            { get; set; }
	}

	public class ScriptLine
	{
		public string                         LineName  { get; set; }
		public uint                           LineId    { get; set; }
		public List<ScriptArtefactCharacters> Artefacts { get; set; }
	}

	public class ScriptArtefactCharacters
	{
		public string          ArtefactName       { get; set; }
		public uint            ArtefactId         { get; set; }
		public decimal?        ArtefactScale      { get; set; }
		public decimal?        ArtefactRotate     { get; set; }
		public int?            ArtefactTranslateX { get; set; }
		public int?            ArtefactTranslateY { get; set; }
		public int?            ArtefactZIndex     { get; set; }
		public List<Character> Characters         { get; set; }
	}

	public class Character
	{
		public uint                          SignId                      { get; set; }
		public uint                          SignInterpretationId        { get; set; }
		public char                          SignInterpretationCharacter { get; set; }
		public List<SpatialRoi>              Rois                        { get; set; }
		public List<CharacterAttribute>      Attributes                  { get; set; }
		public List<CharacterStreamPosition> NextCharacters              { get; set; }
	}

	public class SpatialRoi
	{
		public uint   SignInterpretationRoiId { get; set; }
		public byte[] RoiShape                { get; set; }
		public uint   RoiTranslateX           { get; set; }
		public uint   RoiTranslateY           { get; set; }
		public ushort RoiRotate               { get; set; }
	}

	public class CharacterAttribute
	{
		public uint   SignInterpretationAttributeId { get; set; }
		public string AttributeName                 { get; set; }
		public string AttributeValue                { get; set; }
	}

	public class CharacterStreamPosition
	{
		public uint PositionInStreamId       { get; set; }
		public uint NextSignInterpretationId { get; set; }
	}

	public class Glyph
	{
		public char   Character     { get; set; }
		public uint   CreatorId     { get; set; }
		public uint   EditorId      { get; set; }
		public string Shape         { get; set; }
		public short  YOffset       { get; set; }
		public uint   ScribalFontId { get; set; }
	}

	public class KerningPair
	{
		public char  FirstCharacter  { get; set; }
		public char  SecondCharacter { get; set; }
		public uint  CreatorId       { get; set; }
		public uint  EditorId        { get; set; }
		public short XKern           { get; set; }
		public short YKern           { get; set; }
		public uint  ScribalFontId   { get; set; }
	}

	public class FontInfo
	{
		public uint   CreatorId     { get; set; }
		public uint   EditorId      { get; set; }
		public ushort SpaceSize     { get; set; }
		public ushort LineSpaceSize { get; set; }
		public uint   ScribalFontId { get; set; }
	}
}
