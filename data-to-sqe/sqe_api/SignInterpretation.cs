using System.Collections.Generic;
using System.Linq;
using SQE.DatabaseAccess.Models;

namespace sqe_api
{
	public class SignInterpretation
	{
		private readonly SignInterpretationData _data;

		public SignInterpretation(SignInterpretationData data, uint signId)
		{
			_data = data;
			SignId = signId;
		}

		public uint   SignInterpretationId   => _data.SignInterpretationId.GetValueOrDefault();
		public string Character              => _data.Character;
		public string CharacterForComparison => _getCharacterForComparison();

		public uint SignId { get; }

		public List<uint> NextSignInterpretationIds => _data.NextSignInterpretations
															.Select(s => s.NextSignInterpretationId)
															.ToList();

		private bool IsSpace() => _attributeExists(2);

		private bool IsDamage() => _attributeExists(5);

		private bool IsVacat() => _attributeExists(4) || _attributeExists(3);

		private bool IsBreak() => _attributeExists(9);

		private bool _attributeExists(uint attributeValueId)
		{
			return _data.Attributes.Exists(a => a.AttributeValueId == attributeValueId);
		}

		private string _getCharacterForComparison()
		{
			if (IsSpace())
				return " ";

			if (IsVacat())
				return "V";

			if (IsBreak())
				return "X";

			return Character == ""
					? "?"
					: Character;
		}
	}
}
