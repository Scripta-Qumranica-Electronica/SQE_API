using System.Collections.Generic;
using SQE.DatabaseAccess.Models;

namespace SQE.DatabaseAccess.Helpers
{
    public static class SignInterpretationFactory
    {
        public static SignInterpretationData CreateTerminatorInterpretation(TableData.Table table,
            TableData.TerminatorType terminatorType)
        {
            return _createSimpleInterpretation("",
                SignInterpretationAttributeFactory.CreateElementTerminatorAttributes(table, terminatorType));
        }

        public static SignInterpretationData CreateCharacterInterpretation(
            string character,
            float width = 1,
            Readability? readability = null,
            float? probability = null
        )
        {
            return _createSimpleInterpretation(character,
                SignInterpretationAttributeFactory.CreateCharacterAttribute(width), readability, probability);
        }

        public static SignInterpretationData CreateSpaceInterpretation(float width = 1,
            float? probability = null)
        {
            return _createSimpleInterpretation(" ", SignInterpretationAttributeFactory.CreateSpaceAttribute(width),
                null, probability);
        }

        public static SignInterpretationData CreateVacatInterpretation(float width = 5,
            float? probability = null)
        {
            return _createSimpleInterpretation(" ", SignInterpretationAttributeFactory.CreateVacatAttribute(width),
                null, probability);
        }

        public static SignInterpretationData CreateDamagedInterpretation(float width = 1,
            float? probability = null)
        {
            return _createSimpleInterpretation(" ", SignInterpretationAttributeFactory.CreateDamageAttribute(width),
                null, probability);
        }


        private static SignInterpretationData _createSimpleInterpretation(string character,
            SignInterpretationAttributeData attributeData,
            Readability? readability = null,
            float? probability = null)
        {
            return _createSimpleInterpretation(
                character,
                new List<SignInterpretationAttributeData> { attributeData },
                readability,
                probability);
        }

        private static SignInterpretationData _createSimpleInterpretation(string character,
            List<SignInterpretationAttributeData> attributeData,
            Readability? readability = null,
            float? probability = null)
        {
            var signInterpretation = new SignInterpretationData
            {
                Character = character,
                Attributes = attributeData
            };

            if (readability != null)
                signInterpretation.Attributes.Add(
                    SignInterpretationAttributeFactory.CreateReadabilityAttribute((Readability)readability));

            if (probability != null)
                signInterpretation.Attributes.Add(
                    SignInterpretationAttributeFactory.CreateProbabilityAttribute((float)probability));

            return signInterpretation;
        }
    }
}