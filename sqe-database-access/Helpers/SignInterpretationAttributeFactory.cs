using System.Collections.Generic;
using System.Linq;
using SQE.DatabaseAccess.Models;

namespace SQE.DatabaseAccess.Helpers
{
    public enum Readability : uint
    {
        IncompleteButClear = 18,
        IncompleteButNotClear = 19
    }

    public static class SignInterpretationAttributeFactory
    {
        public static SignInterpretationAttributeData CreateCharacterAttribute(
            float width = 1)
        {
            return _createNumericAttribute(1, width);
        }

        public static SignInterpretationAttributeData CreateSpaceAttribute(float width = 1)
        {
            return _createNumericAttribute(2, width);
        }

        public static SignInterpretationAttributeData CreateVacatAttribute(float width = 5)
        {
            return _createNumericAttribute(2, width);
        }


        public static List<SignInterpretationAttributeData> CreateElementTerminatorAttributes(
            TableData.Table table,
            TableData.TerminatorType terminatorType)
        {
            return TableData.AllTerminators(table, terminatorType).Select(value =>
                 new SignInterpretationAttributeData() { AttributeValueId = value }).ToList();
        }


        public static SignInterpretationAttributeData CreateProbabilityAttribute(float value)
        {
            return _createNumericAttribute(16, value);
        }

        public static SignInterpretationAttributeData CreateReadabilityAttribute(Readability readability)
        {
            return _createNumericAttribute((uint)readability, 0);
        }




        private static SignInterpretationAttributeData _createNumericAttribute(
            uint attributeValueId,
            float value)
        {
            return new SignInterpretationAttributeData()
            {
                AttributeValueId = attributeValueId,
                NumericValue = value
            };
        }

    }




}