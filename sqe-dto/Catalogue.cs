using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SQE.API.DTO
{
	public enum SideDesignation
	{
		recto
		, verso
		,
	}

	public class CatalogueMatchInputDTO
	{
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public SideDesignation catalogSide { get; set; }

		[Required]
		public string imagedObjectId { get; set; }

		[Required]
		public uint manuscriptId { get; set; }

		[Required]
		public string manuscriptName { get; set; }

		[Required]
		public string editionName { get; set; }

		[Required]
		public string editionVolume { get; set; }

		[Required]
		public string editionLocation1 { get; set; }

		[Required]
		public string editionLocation2 { get; set; }

		[JsonConverter(typeof(JsonStringEnumConverter))]
		[Required]
		public SideDesignation editionSide { get; set; }

		public string comment { get; set; }

		[Required]
		public uint textFragmentId { get; set; }

		[Required]
		public uint editionId { get; set; }

		public bool? confirmed { get; set; }
	}

	public class CatalogueMatchDTO : CatalogueMatchInputDTO
	{
		[Required]
		public uint imageCatalogId { get; set; }

		[Required]
		public string institution { get; set; }

		[Required]
		public string catalogueNumber1 { get; set; }

		public string catalogueNumber2 { get; set; }
		public string proxy            { get; set; }

		[Required]
		public string url { get; set; }

		[Required]
		public string filename { get; set; }

		[Required]
		public string suffix { get; set; }

		[Required]
		public string thumbnail { get; set; }

		[Required]
		public string license { get; set; }

		[Required]
		public uint iaaEditionCatalogueId { get; set; }

		[Required]
		public string manuscriptName { get; set; }

		[Required]
		public string name { get; set; }

		[Required]
		public string matchAuthor { get; set; }

		public string matchConfirmationAuthor { get; set; }

		[Required]
		public uint matchId { get; set; }

		[Required]
		public DateTime dateOfMatch { get; set; }

		public DateTime? dateOfConfirmation { get; set; }
	}

	public class CatalogueMatchListDTO
	{
		[Required]
		public CatalogueMatchDTO[] matches { get; set; }
	}
}
