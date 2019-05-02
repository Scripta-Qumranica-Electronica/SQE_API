using Newtonsoft.Json;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class CharAttribute
    {
        public uint charAttributeId { get; set; }
        public uint attributeValueId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? value { get; set; }
    }
}