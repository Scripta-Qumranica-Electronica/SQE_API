using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SQE.API.DTO;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Serialization
{
    public static partial class ExtensionsDTO
    {
        public static CatalogueMatchDTO ToDTO(this CatalogueMatch cat)
        {
            return new CatalogueMatchDTO()
            {
                catalogSide = cat.CatalogSide == 0
                    ? CatalogueMatchDTO.SideDesignation.recto
                    : CatalogueMatchDTO.SideDesignation.verso,
                catalogueNumber1 = cat.CatalogueNumber1,
                catalogueNumber2 = cat.CatalogueNumber2,
                comment = cat.Comment,
                confirmed = cat.Confirmed,
                dateOfMatch = cat.Date,
                editionId = cat.EditionId,
                editionLocation1 = cat.EditionLocation1,
                editionLocation2 = cat.EditionLocation2,
                editionName = cat.EditionName,
                editionSide = cat.EditionSide == 0
                    ? CatalogueMatchDTO.SideDesignation.recto
                    : CatalogueMatchDTO.SideDesignation.verso,
                editionVolume = cat.EditionVolume,
                filename = cat.Filename,
                institution = cat.Institution,
                imageCatalogId = cat.ImageCatalogId,
                imagedObjectId = cat.ImagedObjectId,
                iaaEditionCatalogueId = cat.IaaEditionCatalogueId,
                license = cat.License,
                manuscriptId = cat.ManuscriptId,
                manuscriptName = cat.ManuscriptName,
                matchAuthor = cat.MatchAuthor,
                name = cat.Name,
                proxy = cat.Proxy,
                suffix = cat.Suffix,
                thumbnail = $"{cat.Proxy}{cat.Url}{cat.Filename}/full/150,/0/{cat.Suffix}",
                textFragmentId = cat.TextFragmentId,
                url = cat.Url,
            };
        }

        public static CatalogueMatchListDTO ToDTO(this IEnumerable<CatalogueMatch> catList)
        {
            return new CatalogueMatchListDTO()
            {
                matches = catList.Select(x => x.ToDTO()).ToArray()
            };
        }
        public static InstitutionalImageDTO ToInstitutionalImageDTO(this CatalogueMatch cat)
        {
            return new InstitutionalImageDTO()
            {
                id = cat.ImagedObjectId,
                license = cat.License,
                thumbnailUrl = $"{cat.Proxy}${cat.Url}${cat.Filename}/full/150,/0/${cat.Suffix}"
            };
        }
        public static InstitutionalImageListDTO ToInstitutionalImageListDTO(this IEnumerable<CatalogueMatch> catList)
        {
            return new InstitutionalImageListDTO()
            {
                institutionalImages = catList.Select(x => x.ToInstitutionalImageDTO()).ToList()
            };
        }

        public static TextFragmentDataDTO ToTextFragmentDataDTO(this CatalogueMatch cat)
        {
            return new TextFragmentDataDTO()
            {
                editorId = 0,
                id = cat.TextFragmentId,
                name = cat.Name
            };
        }

        public static TextFragmentDataListDTO ToTextFragmentDataListingDTO(this IEnumerable<CatalogueMatch> catList)
        {
            return new TextFragmentDataListDTO()
            {
                textFragments = catList.Select(x => x.ToTextFragmentDataDTO()).ToList()
            };
        }
    }
}