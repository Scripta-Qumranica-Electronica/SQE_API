using System.Collections.Generic;
using System.Net.NetworkInformation;
using SQE.DatabaseAccess.Models;
using static SQE.DatabaseAccess.Helpers.SignInterpretationFactory;

namespace SQE.DatabaseAccess.Helpers
{
    public static class SignFactory
    {
        public static SignData CreateSimpleCharacterSign(string character,
            float width = 1,
            Readability? readability = null,
            float? probability = null)
        {
            return new SignData()
            {
                SignInterpretations = new List<SignInterpretationData>() { CreateCharacterInterpretation(character,
                    width, readability, probability)}
            };
        }
        public static SignData CreateSpaceSign(float width = 1, float? probability = null)
        {
            return new SignData()
            {
                SignInterpretations = new List<SignInterpretationData>() { CreateSpaceInterpretation(width, probability) }
            };
        }

        public static SignData CreateVacatSign(float width = 5, float? probability = null)
        {
            return new SignData()
            {
                SignInterpretations = new List<SignInterpretationData>() { CreateVacatInterpretation(width, probability) }
            };
        }

        public static SignData CreateTerminatorSign(TableData.Table table, TableData.TerminatorType terminatorType)
        {
            return new SignData()
            {
                SignInterpretations = new List<SignInterpretationData>()
                {
                    CreateTerminatorInterpretation(table, terminatorType)
                }
            };
        }










    }
}