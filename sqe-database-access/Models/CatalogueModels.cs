using System;

namespace SQE.DatabaseAccess.Models
{
    public class CatalogueMatch
    {
        public uint ImageCatalogId { get; set; }
        public string Institution { get; set; }
        public string CatalogueNumber1 { get; set; }
        public string CatalogueNumber2 { get; set; }
        public byte CatalogSide { get; set; }
        public string ImagedObjectId { get; set; }
        public string Url { get; set; }
        public string Proxy { get; set; }
        public string Suffix { get; set; }
        public string License { get; set; }
        public string Filename { get; set; }
        public uint IaaEditionCatalogueId { get; set; }
        public uint ManuscriptId { get; set; }
        public string ManuscriptName { get; set; }
        public string EditionName { get; set; }
        public string EditionVolume { get; set; }
        public string EditionLocation1 { get; set; }
        public string EditionLocation2 { get; set; }
        public byte EditionSide { get; set; }
        public string Comment { get; set; }
        public uint TextFragmentId { get; set; }
        public string Name { get; set; }
        public uint EditionId { get; set; }
        public bool? Confirmed { get; set; }
        public string MatchAuthor { get; set; }
        public string MatchConfirmationAuthor { get; set; }
        public uint MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public DateTime? MatchConfirmationDate { get; set; }
    }

    public class EditionCatalogueEntry
    {
        public uint IaaEditionCatalogId { get; set; }
        // public string Manuscript { get; set; }
        // public string EditionName { get; set; }
        // public string EditionVolume { get; set; }
        // public string EditionLocation1 { get; set; }
        // public string EditionLocation2 { get; set; }
        // public byte EditionSide { get; set; }
        // public string Comment { get; set; }
        // public uint ManuscriptId { get; set; }
        // public uint EditionId { get; set; }
    }
}