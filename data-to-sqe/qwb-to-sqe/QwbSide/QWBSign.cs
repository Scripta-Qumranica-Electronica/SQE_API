using System.Linq;

namespace qwb_to_sqe
{
	public class QwbSign : Sign
	{
		public int[] OtherAttributes;

		public QwbSign(string character, params int[] attributeValues) : base(
				character
				, attributeValues) { }

		public bool HasCirc { set => SetBoolAttribute(19, value); }

		public bool IsSuperScript { set => SetBoolAttribute(31, value); }

		public bool IsSubScript { set => SetBoolAttribute(30, value); }

		public bool IsReconstructed { set => SetBoolAttribute(20, value); }

		public bool IsWrongAddition { set => SetBoolAttribute(23, value); }

		public bool IsForgotten { set => SetBoolAttribute(22, value); }

		public bool IsOriginal { set => SetBoolAttribute(41, value); }

		public bool IsCorrected { set => SetBoolAttribute(42, value); }

		public bool IsInserted { set => SetBoolAttribute(43, value); }

		public bool IsMarginVariant { set => SetBoolAttribute(38, value); }

		public bool IsQuestionable { set => SetBoolAttribute(44, value); }

		public bool IsConjecture { set => SetBoolAttribute(21, value); }

		public string Others
		{
			set => OtherAttributes = value.Split(", ").Select(int.Parse).ToArray();
		}

		public void expandVacat()
		{
			stepValue(4);
		}

		public void expandDestroyedRegion()
		{
			stepValue(5, 3);
		}

		public void expandDestroyedSign()
		{
			stepValue(5);
		}

		private void stepValue(int attributeValueId, int by = 1)
		{
			if (Attributes.ContainsKey(attributeValueId))
			{
				//    ((SQESignAttribute) Attributes[attributeValueId]).value+=by;
			}
			else
				AddAttribute(attributeValueId, by);
		}
	}
}
