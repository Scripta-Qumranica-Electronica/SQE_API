using System.Collections.Generic;

namespace sqe_api
{
	public class SignInterpretationSequence
	{
		public string     charString = "";
		public List<uint> nextSignInterpretationIds;
		public List<uint> signInterpretationIds;

		public SignInterpretationSequence(SignInterpretationSequence oldSequence)
		{
			charString = oldSequence.charString;
			signInterpretationIds = new List<uint>();
			signInterpretationIds.AddRange(oldSequence.signInterpretationIds);
		}

		public SignInterpretationSequence(SignInterpretation signInterpretation)
		{
			signInterpretationIds = new List<uint>();

			addInterpretation(signInterpretation);
		}

		public void addInterpretation(SignInterpretation signInterpretation)
		{
			charString += signInterpretation.CharacterForComparison;
			signInterpretationIds.Add(signInterpretation.SignInterpretationId);
			nextSignInterpretationIds = signInterpretation.NextSignInterpretationIds;
		}

		public SignInterpretationSequence createNewSequence(
				SignInterpretation addedSignInterpretation)
		{
			var newSequence = new SignInterpretationSequence(this);
			newSequence.addInterpretation(addedSignInterpretation);

			return newSequence;
		}

		public bool HasFollowers() => (nextSignInterpretationIds != null)
									  && (nextSignInterpretationIds.Count > 0);

		public uint GetSignInterpretationIdAtPosition(int position)
			=> signInterpretationIds[position];

		public int NumberOfInterpretations() => signInterpretationIds.Count;
	}
}
