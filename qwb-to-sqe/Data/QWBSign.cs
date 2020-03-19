using System;
using System.Linq;

namespace qwb_to_sqe
{
    public class QwbSign : Sign
    {

        public QwbSign(string character, params int[] attributeValues) : base(character, attributeValues)
        {
        }

       public bool HasCirc{
            set => this.SetBoolAttribute(19, value);
        }
        public bool IsSuperScript{
            set => this.SetBoolAttribute(31, value);
        }
        public bool IsSubScript{
            set => this.SetBoolAttribute(30, value);
        }
        public bool IsReconstructed{
            set => this.SetBoolAttribute(20, value);
        }
        public bool IsWrongAddition{
            set => this.SetBoolAttribute(23, value);
        }
        public bool IsForgotten{
            set => this.SetBoolAttribute(22, value);
        }
        public bool IsOriginal{
            set => this.SetBoolAttribute(41, value);
        }
        public bool IsCorrected{
            set => this.SetBoolAttribute(42, value);
        }
        public bool IsInserted{
            set => this.SetBoolAttribute(43, value);
        }
        public bool IsMarginVariant
        {
            set =>this.SetBoolAttribute(38, value);
            
        }

        public bool IsQuestionable{
            set => this.SetBoolAttribute(44, value);
        }
        
        public bool IsConjecture{
            set => this.SetBoolAttribute(21, value);
        }

        public string Others
        {
            set => OtherAttributes = value.Split(", ").Select(Int32.Parse).ToArray();
        }

        public int[] OtherAttributes;




        
        
        public void expandVacat()
        {
            stepValue(4);
        }
        public void expandDestroyedRegion()
        {
           stepValue(5,3);
        }
        public void expandDestroyedSign()
        {
            stepValue(5);
        }

        private void stepValue(int attributeValueId, int by=1)
        {
            if (Attributes.ContainsKey(attributeValueId))
            {
                ((SQESignAttribute) Attributes[attributeValueId]).value+=by;
            }
            else
            {
                this.AddAttribute(attributeValueId, @by);
            }
        }


        
    }
}