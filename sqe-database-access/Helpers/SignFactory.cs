using System.Collections.Generic;
using SQE.DatabaseAccess.Models;

namespace SQE.DatabaseAccess.Helpers
{
	public static class SignFactory
	{
		public static SignData CreateSimpleCharacterSign(
				string         character
				, float        width       = 1
				, Readability? readability = null
				, float?       probability = null) => new SignData
		{
				SignInterpretations = new List<SignInterpretationData>
				{
						SignInterpretationFactory.CreateCharacterInterpretation(
								character
								, width
								, readability
								, probability)
						,
				}
				,
		};

		public static SignData CreateSpaceSign(float width = 1, float? probability = null)
			=> new SignData
			{
					SignInterpretations = new List<SignInterpretationData>
					{
							SignInterpretationFactory.CreateSpaceInterpretation(
									width
									, probability)
							,
					}
					,
			};

		public static SignData CreateVacatSign(float width = 5, float? probability = null)
			=> new SignData
			{
					SignInterpretations = new List<SignInterpretationData>
					{
							SignInterpretationFactory.CreateVacatInterpretation(
									width
									, probability)
							,
					}
					,
			};

		public static SignData CreateDamageSign(float width = 1, float? probability = null)
			=> new SignData
			{
					SignInterpretations = new List<SignInterpretationData>
					{
							SignInterpretationFactory.CreateDamagedInterpretation(
									width
									, probability)
							,
					}
					,
			};

		public static SignData CreateTerminatorSign(
				TableData.Table            table
				, TableData.TerminatorType terminatorType) => new SignData
		{
				SignInterpretations = new List<SignInterpretationData>
				{
						SignInterpretationFactory.CreateTerminatorInterpretation(
								table
								, terminatorType)
						,
				}
				,
		};
	}
}
