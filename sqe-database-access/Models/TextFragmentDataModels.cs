using System;
using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class TextFragmentData
	{
		public List<LineData> Lines                  { get; set; } = new List<LineData>();
		public string         TextFragmentName       { get; set; }
		public uint?          TextFragmentId         { get; set; }
		public uint?          PreviousTextFragmentId { get; set; }
		public uint?          NextTextFragmentId     { get; set; }
		public uint?          TextFragmentEditorId   { get; set; }
	}

	public class TextFragmentSearch
	{
		public uint   EditionId      { get; set; }
		public string EditionName    { get; set; }
		public uint   TextFragmentId { get; set; }
		public string Name           { get; set; }
		public string Editors        { get; set; }
	}

	public class CachedTextEdition
	{
		public uint     EditionId               { get; set; }
		public uint     TextFragmentId          { get; set; }
		public string   CachedTranscription     { get; set; }
		public DateTime CachedTranscriptionDate { get; set; }
		public DateTime QueryDate               { get; set; }
	}
}
