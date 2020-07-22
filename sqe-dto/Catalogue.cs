using System;
using System.Text.Json.Serialization;

namespace SQE.API.DTO
{
    public class CatalogueMatchDTO
    {
        public enum SideDesignation
        {
            recto,
            verso
        }

        public uint imageCatalogId { get; set; }
        public string institution { get; set; }
        public string catalogueNumber1 { get; set; }
        public string catalogueNumber2 { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SideDesignation catalogSide { get; set; }
        public string imagedObjectId { get; set; }
        public string proxy { get; set; }
        public string url { get; set; }
        public string filename { get; set; }
        public string suffix { get; set; }
        public string thumbnail { get; set; }
        public string license { get; set; }
        public uint iaaEditionCatalogueId { get; set; }
        public uint manuscriptId { get; set; }
        public string editionName { get; set; }
        public string editionVolume { get; set; }
        public string editionLocation1 { get; set; }
        public string editionLocation2 { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SideDesignation editionSide { get; set; }
        public string comment { get; set; }
        public uint textFragmentId { get; set; }
        public string name { get; set; }
        public uint editionId { get; set; }
        public bool confirmed { get; set; }
        public string matchAuthor { get; set; }
        public DateTime dateOfMatch { get; set; }
    }

    public class CatalogueMatchListDTO
    {
        public CatalogueMatchDTO[] matches { get; set; }
    }
}