using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices;
using SQE.DatabaseAccess.Models;

namespace sqe_api
{
	public struct OldNew
	{
		public uint? OldId;
		public uint? NewId;
	}

	/// <summary>
	///     Provides an extended version of LineData which additional functionality to retrieve and handle the signs
	/// </summary>
	public class Line
	{
		public readonly LineData LineData;
		public           SignData FirstAnchor { get; }
		public           SignData LastAnchor  { get; }



		public uint StartAnchorId
		{
			get;
		}

		public uint EndAnchorId
		{
			get;
		}

		public uint AnchorBefore = 0;
		public uint AnchorAfter  = 0;

		private readonly List<SignInterpretation> _signInterpretations =
				new List<SignInterpretation>();

		// referenc to the realted textEdition
		private Scroll _textEdition;

		public Line(LineData lineData) : this(null, lineData) { }

		public Line(Scroll textEdition, LineData lineData)
		{
			_textEdition = textEdition;
			LineData = lineData;
			FirstAnchor = _getLineAnchor(true);
			LastAnchor = _getLineAnchor(false);
			StartAnchorId = LineData.Signs[0].SignInterpretations[0].SignInterpretationId.Value;
			if (LineData.Signs.Last().SignInterpretations[0].NextSignInterpretations.Count>0)
			EndAnchorId = LineData.Signs.Last().SignInterpretations[0].NextSignInterpretations[0].NextSignInterpretationId;

				; LineData.Signs.RemoveAll(
					s => s.SignInterpretations.Any(
							si => si.Attributes.Any(a => a.AttributeValueId == 9)));

			foreach (var sign in LineData.Signs)
			foreach (var signInterpretation in sign.SignInterpretations)
			{
				var processedSignInterpretation = new SignInterpretation(
						signInterpretation
						, sign.SignId.GetValueOrDefault());

				_signInterpretations.Add(processedSignInterpretation);
			}
		}

		public List<SignInterpretationSequence> getSequences()
		{
			var sequences = new List<SignInterpretationSequence>();

			foreach (var signInterpretation in _signInterpretations.FindAll(
					s => s.SignId == LineData.Signs[0].SignId))
				sequences.Add(new SignInterpretationSequence(signInterpretation));

			return _getSequences(sequences);
		}

		/// <summary>
		///     Returns a list of allsignificant signs in the line (thus stripping of all break information)
		/// </summary>
		/// <returns></returns>
		private List<SignInterpretationSequence> _getSequences(
				List<SignInterpretationSequence> sequences)
		{
			var newSequences = new List<SignInterpretationSequence>();
			var hasFollowers = false;

			foreach (var s in sequences)
			foreach (var nextSignInterpretationId in s.nextSignInterpretationIds)
			{
				var nextSignInterpretation = GetSignInterpretationById(nextSignInterpretationId);

				if (nextSignInterpretation == null)
					continue;

				//                   var newSequence = s.createNewSequence(nextSignInterpretation);
				newSequences.Add(s.createNewSequence(nextSignInterpretation));

				if (newSequences.Last().HasFollowers())
					hasFollowers = true;
			}

			return hasFollowers
					? _getSequences(newSequences)
					: newSequences.Count == 0
							? sequences
							: newSequences;
		}

		public SignInterpretation GetSignInterpretationById(uint id)
		{
			return _signInterpretations.Find(s => s.SignInterpretationId == id);
		}

		public void ChangeToSource(
				uint   sqeSignInterpretationId
				, Line sourceLine
				, uint sourceSignInterpretationId)
		{
			var sqeSignData = GetSignDataBySignInterpretationId(sqeSignInterpretationId);

			var sourceSignData =
					sourceLine.GetSignDataBySignInterpretationId(sourceSignInterpretationId);

			var oldNewIds = new List<OldNew>();
			var mustBeDeleted = new List<SignInterpretationData>();
			var mustBeAdded = new List<SignInterpretationData>();

			foreach (var sqeSignInterpretation in sqeSignData.SignInterpretations)
			{
				var sourceSignInterpretation = sourceSignData.SignInterpretations.Find(
						s => s.Character.Equals(sqeSignInterpretation.Character));

				if (sourceSignInterpretation != null)

						//A matching character was found
				{
					//Mark the sourceSign as processed by setting its Charactor to null and its connections
					// to the sqe sign interpretation by setting its SignInterpretationID to the same as the sqe.
					sourceSignInterpretation.Character = null;

					oldNewIds.Add(
							new OldNew()
							{
									OldId = sourceSignInterpretation.SignInterpretationId
									, NewId = sqeSignInterpretation.SignInterpretationId
									,
							});

					sourceSignInterpretation.SignInterpretationId =
							sqeSignInterpretation.SignInterpretationId;

					// Copy the data.
					sqeSignInterpretation.SignInterpretationRois =
							sourceSignInterpretation.SignInterpretationRois;

					sqeSignInterpretation.InterpretationCommentary =
							sourceSignInterpretation.InterpretationCommentary;

					sqeSignInterpretation.Attributes = sourceSignInterpretation.Attributes;
				}
				else
					mustBeDeleted.Add(sqeSignInterpretation);
			}
		}

		public SignData GetSignDataBySignInterpretationId(uint signInterpretationId)
		{
			var signData = LineData.Signs.Find(
					s => s.SignInterpretations.Any(
							si => si.SignInterpretationId == signInterpretationId));


			return signData;
		}

		private SignData _getLineAnchor(bool forStart)
		{
			return LineData.Signs.Find(
					s => s.SignInterpretations.Any(
							si => si.Attributes.Any(
									a => a.AttributeValueId
										 == (uint) (forStart
												 ? 10
												 : 11))));
		}

		/*
		public SignData GetFirstAnchorSign() => _getAnchorSign(10);
		public SignData GetLastAnchorSign() => _getAnchorSign(11);


		private SignData _getAnchorSign(uint attrributeId) => LineData.Signs.Find(
				s => s.SignInterpretations.Any(
						si => si.Attributes.Any(
								a => a.AttributeValueId
									 == attrributeId)));
									 */

		public uint GetLineId() => LineData.LineId.GetValueOrDefault();

	}
}
