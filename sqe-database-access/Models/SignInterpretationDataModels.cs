using System.Collections.Generic;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.DatabaseAccess.Models
{
	public class BasicSignInterpretationAttribute
	{
		public uint AttributeId      { get; set; }
		public uint AttributeValueId { get; set; }
	}

	public class BasicSingleSignInterpretation
	{
		public uint                                      SignInterpretationId     { get; set; }
		public string                                    Character                { get; set; }
		public uint                                      NextSignInterpretationId { get; set; }
		public byte                                      IsVariant                { get; set; }
		public HashSet<BasicSignInterpretationAttribute> Attributes               { get; set; }
	}

	public class BasicSignInterpretation : BasicSingleSignInterpretation
	{
		public HashSet<uint> NextSignInterpretationIds { get; set; }
	}

	public class SignInterpretationData
	{
		private uint? _signInterpretationId;
		public  uint? SignId { get; set; }

		public uint? SignInterpretationId
		{
			get => _signInterpretationId;
			set => _setSignInterpretationId(value);
		}

		public uint?      SignInterpretationAttributeCreatorId { get; set; }
		public List<uint> SignStreamSectionIds                 { get; set; } = new List<uint>();

		public List<uint> QwbWordIds { get; set; } = new List<uint>();

		public List<SignInterpretationAttributeData> Attributes { get; set; } =
			new List<SignInterpretationAttributeData>();

		public List<SignInterpretationCommentaryData> Commentaries { get; set; } =
			new List<SignInterpretationCommentaryData>();

		// NOTE Ingo changed the collection of nextSignInterpretationIds from hashset to list
		public List<NextSignInterpretation> NextSignInterpretations { get; set; } =
			new List<NextSignInterpretation>();

		public List<SignInterpretationRoiData> SignInterpretationRois { get; set; } =
			new List<SignInterpretationRoiData>();

		public string Character                       { get; set; }
		public bool   IsVariant                       { get; set; }
		public string InterpretationCommentary        { get; set; }
		public uint?  InterpretationCommentaryCreator { get; set; }
		public uint?  InterpretationCommentaryEditor  { get; set; }

		private void _setSignInterpretationId(uint? newSignInterpretaionId)
		{
			_signInterpretationId = newSignInterpretaionId;

			foreach (var attribute in Attributes)
				attribute.SignInterpretationId = _signInterpretationId;

			foreach (var commentary in Commentaries)
				commentary.SignInterpretationId = _signInterpretationId;

			foreach (var roi in SignInterpretationRois)
				roi.SignInterpretationId = _signInterpretationId;
		}
	}

	public class NextSignInterpretation
	{
		public NextSignInterpretation(uint nextSignInterpretationId, uint signSequenceAuthor)
		{
			NextSignInterpretationId = nextSignInterpretationId;
			SignSequenceAuthor = signSequenceAuthor;
		}

		public NextSignInterpretation() { }

		public uint NextSignInterpretationId { get; set; }
		public uint SignSequenceAuthor       { get; set; }
		public bool IsMain                   { get; set; }
		public uint PositionCreatorId        { get; set; }
		public uint PositionEditorId         { get; set; }

		// The override for Equals and GetHashCode methods here enable the
		// HashSet nextSignInterpretations of the SignInterpretation object
		// to ensure that no duplicate values will be inserted into the set.
		public override bool Equals(object obj) => obj is NextSignInterpretation q
												   && (q.NextSignInterpretationId
													   == NextSignInterpretationId)
												   && (q.SignSequenceAuthor == SignSequenceAuthor);

		public override int GetHashCode() => NextSignInterpretationId.GetHashCode()
											 ^ SignSequenceAuthor.GetHashCode();
	}
}
