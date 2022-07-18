using System;
using System.Collections.Generic;
using SQE.DatabaseAccess.Models;

namespace from_goettingen.Source
{
	public enum Attributes
	{
		VACAT    = 4
		, SPACE  = 2
		, LETTER = 1
		,
	}

	public class SourceSign : IEquatable<SourceSign>
							  , IComparable<SourceSign>
	{
		public string Sign { get; }

		private readonly List<SourceRoi> _rois       = new List<SourceRoi>();
		private readonly List<int>       _attributes = new List<int>();
		private readonly int             _sequence;
		private readonly List<string>    _varReadings = new List<string>();
		private          string          _commentary;
		private readonly string          _additionalSignString = "";
		private          int?            _sqeSignPos           = null;

		public SourceSign(int sequence, string charSign, string commentary)
		{
			//Normalize commentaries.
			if (!String.IsNullOrWhiteSpace(commentary))
				_commentary = commentary.Trim();

			// No charSign is given
			if (String.IsNullOrWhiteSpace(charSign)
				|| charSign.ToLower() == "s")
			{
				Sign = "";

				// If commentary is null - assume it is a space
				if (_commentary == null)
					addAttribute(Attributes.SPACE);
				else if (_commentary.ToLower().Contains("vacat"))

						// else if commentary contais the word vacat, assume a vacat is meant
				{
					addAttribute(Attributes.VACAT);

					// reset commentary if only vacat is mentioned.
					if (_commentary.ToLower() == "vacat")
						_commentary = null;
				}
				else if (charSign == "v")
				{
					Sign = "";
					addAttribute(Attributes.VACAT);
				}
				else
				{
					Sign = "?";
					addAttribute(Attributes.LETTER);
				}
			}
			else
			{
				Sign = charSign[0].ToString();
				_additionalSignString = charSign.Substring(1);
				addAttribute(Attributes.LETTER);
			}

			_sequence = sequence;
		}

		public void addRois(List<SourceRoi> rois)
		{
			_rois.AddRange(rois);
		}

		public void addCommentary(String commentary)
		{
			_commentary += " - " + commentary;
		}

		private void addAttribute(Attributes attribute)
		{
			_attributes.Add((int) attribute);
		}

		public void addAttributes(List<int> attributes)
		{
			_attributes.AddRange(attributes);
		}

		public bool Equals(SourceSign other) => other != null && _sequence == other._sequence;

		public int CompareTo(SourceSign other) => _sequence.CompareTo(other._sequence);

		public bool HasSameCharacter(SignData sqeSign)
		{
			foreach (var sqeSignSignInterpretation in sqeSign.SignInterpretations)
			{
				if (sqeSignSignInterpretation.Character.Equals(Sign))
				{
					_sqeSignPos = -1;

					return true;
				}
			}

			return false;
		}

		public void addVarReading(String newSign)
		{
			_varReadings.Add(newSign);
		}

		private void addVarReadings(List<string> varReadings)
		{
			_varReadings.AddRange(varReadings);
		}

		private bool hasAttribute(int attributeId) => _attributes.Contains(attributeId);

		public bool isVacat() => hasAttribute(4);

		public bool isSpace() => hasAttribute(2);

		private SignInterpretationAttributeData _createAttributeData(int attribute)
		{
			var attrData =
					new SignInterpretationAttributeData { AttributeValueId = (uint) attribute };

			return attrData;
		}

		private SignInterpretationData _createSignInterpretation(
				string  character
				, uint  signInterpretationId
				, bool? isVariant)
		{
			var interpretationData = new SignInterpretationData
			{
					SignInterpretationId = signInterpretationId
					, IsVariant = isVariant.GetValueOrDefault()
					, Character = character
					,
			};

			foreach (var r in _rois)
				interpretationData.SignInterpretationRois.Add(r.getRoiData());

			foreach (var a in _attributes)
				interpretationData.Attributes.Add(_createAttributeData(a));

			interpretationData.Commentaries.Add(
					new SignInterpretationCommentaryData()
					{
							AttributeId = null,
							Commentary = _commentary
							,
					});

			return interpretationData;
		}

		public SignData asSignData(ref uint signInterpretationId)
		{
			var signData = new SignData { SignId = signInterpretationId };

			signData.SignInterpretations.Add(
					_createSignInterpretation(Sign, signInterpretationId++, false));

			foreach (var c in _varReadings)
			{
				signData.SignInterpretations.Add(
						_createSignInterpretation(c, signInterpretationId++, true));
			}

			return signData;
		}

		/// <summary>
		/// If the first sign had been in fact - erronously - a string of more than two signs,
		/// then the additional ones had been stored in _additionalSigns. The function creates discrete
		/// signs from them which differ from the main sign only in the character.
		/// </summary>
		/// <returns>List of SourceSigns</returns>
		public List<SourceSign> GetAdditionalSigns()
		{
			var additionalSigns = new List<SourceSign>();

			foreach (var additionalCharacter in _additionalSignString)
			{
				var newSign = new SourceSign(
						_sequence + 1
						, additionalCharacter.ToString()
						, _commentary);

				newSign.addAttributes(_attributes);
				newSign.addRois(_rois);
				newSign.addVarReadings(_varReadings);
				additionalSigns.Add(newSign);
			}

			return additionalSigns;
		}
	}
}
