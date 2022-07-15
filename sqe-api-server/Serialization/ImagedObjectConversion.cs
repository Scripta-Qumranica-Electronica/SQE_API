using System.Collections.Generic;
using System.Linq;
using Dapper;
using SQE.API.DTO;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Serialization
{
	public static partial class ExtensionsDTO
	{
		public static InstitutionalImageDTO ToDTO(this InstitutionImage image)
			=> new InstitutionalImageDTO
			{
					id = image.Name
					, thumbnailUrl = image.Thumbnail
					, license = image.License
					,
			};

		public static InstitutionalImageListDTO ToDTO(this IEnumerable<InstitutionImage> images)
		{
			return new InstitutionalImageListDTO
			{
					institutionalImages = images.Select(x => x.ToDTO()).ToList(),
			};
		}

		public static SimpleImageDTO ToDTO(this ImagedObjectImage image)
		{
			return new SimpleImageDTO
			{
					catalogNumber = image.image_catalog_id
					, id = image.image_catalog_id
					, lightingDirection = image.img_type switch
										  {
												  0   => SimpleImageDTO.Direction.top
												  , 1 => SimpleImageDTO.Direction.top
												  , 2 => SimpleImageDTO.Direction.left
												  , 3 => SimpleImageDTO.Direction.right
												  , _ => SimpleImageDTO.Direction.top
												  ,
										  }
					, lightingType = image.img_type switch
									 {
											 0   => SimpleImageDTO.Lighting.direct
											 , 1 => SimpleImageDTO.Lighting.direct
											 , 2 => SimpleImageDTO.Lighting.raking
											 , 3 => SimpleImageDTO.Lighting.raking
											 , _ => SimpleImageDTO.Lighting.direct
											 ,
									 }
					, master = image.master
					, ppi = image.ppi
					, side = image.side == 0
							? SideDesignation.recto
							: SideDesignation.verso
					, type = image.wave_start == image.wave_end
							? "infrared"
							: "color"
					, imageManifest = image.image_manifest
					, url = $"{image.proxy}{image.url}{image.filename}"
					, waveLength = new string[2]
					{
							image.wave_start.ToString(), image.wave_end.ToString(),
					}
					,
			};
		}

		public static SimpleImageListDTO ToDTO(this IEnumerable<ImagedObjectImage> images)
		{
			return new SimpleImageListDTO { images = images.Select(x => x.ToDTO()).ToArray() };
		}

		public static ImageSearchResponseListDTO ToDTO(this IEnumerable<SearchImagedObject> siol)
		{
			return new ImageSearchResponseListDTO
			{
					imagedObjects = siol.Select(x => x.ToDTO()).AsList(),
			};
		}

		public static ImageSearchResponseDTO ToDTO(this SearchImagedObject sio)
			=> new ImageSearchResponseDTO
			{
					id = sio.Id
					, rectoThumbnail = sio.RectoThumbnail
					, versoThumbnail = sio.VersoThumbnail
					,
			};
	}
}
