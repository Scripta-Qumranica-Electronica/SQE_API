
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace qwb_to_sqe
{
    public class Sign
    {
        
        public readonly Dictionary<int,SQESignAttribute> Attributes = new Dictionary<int, SQESignAttribute>();
        public string character = "";
        public int is_variant = 0;
        public int sign_id = 0;
        public int sign_interpretation_id = 0;


        protected Sign(string character, params int[] attributeValues)
        {
            this.character = character;
            AddAttributes(attributeValues);
            
        }

        public void AddAttributes(params int[] attributeValues)
        {
            foreach (var attributeValue in attributeValues) AddAttribute(attributeValue);
        }
        public void AddAttribute(int attributeValue, int value = 0, int sequence = 0)
        {
            var attribute = new SQESignAttribute
            {
                attribute_value_id = attributeValue, value = value, sequence = sequence
            };
            Attributes.Add(attributeValue, attribute);
        }

        protected void SetBoolAttribute(int attributeValueId, bool value)
        {
            if (value && !Attributes.ContainsKey(attributeValueId)) AddAttribute(attributeValueId);
            else if (Attributes.ContainsKey(attributeValueId)) Attributes.Remove(attributeValueId);
        }

        // Add a new sign after the given previous sign
        public void addAfter(Sign previousSign)
        {
            //New sign
            if (previousSign.sign_id == 0)
            {
                    
            }
        }
    }
}