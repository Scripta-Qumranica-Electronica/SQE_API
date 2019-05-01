﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.Backend.Server.DTOs;
using SQE.Backend.DataAccess;

namespace SQE.Backend.Server.Services
{
    public interface IImagedFragmentsService
    {
        Task<ImagedFragmentListDTO> GetImagedFragments(uint? userId, uint scrollVersionId);
        Task<ImagedFragmentDTO> GetImagedFragment(uint? userId, uint scrollVersionId, string fragmentId);
    }
    public class ImagedFragmentsService : IImagedFragmentsService
    {
        IImagedFragmentsRepository _repo;
        IImageRepository _imageRepo;
        IImageService _imageService;
        IArtefactService _artefactService;

        public ImagedFragmentsService(IImagedFragmentsRepository repo, IImageRepository imageRepo, IImageService imageService, IArtefactService artefactService)
        {
            _repo = repo;
            _imageRepo = imageRepo;
            _imageService = imageService;
            _artefactService = artefactService;
        }

        async public Task<ImagedFragmentListDTO> GetImagedFragments(uint? userId, uint scrollVersionId)
        {
            var imagedFragments = await _repo.GetImagedFragments(userId, scrollVersionId, null);

            if (imagedFragments == null)
            {
                throw new NotFoundException((uint)scrollVersionId);
            }
            var result = new ImagedFragmentListDTO
            {
                result = new List<ImagedFragmentDTO>(),
            };
            var images = await _imageRepo.GetImages(userId, scrollVersionId, null); //send imagedFragment from here 

            var imageDict = new Dictionary<string, List<ImageDTO>>();
            foreach (var image in images)
            {
                var fragmentId = getFragmentId(image);
                if (!imageDict.ContainsKey(fragmentId))
                {
                    imageDict[fragmentId] = new List<ImageDTO>();
                }
                imageDict[fragmentId].Add(_imageService.ImageToDTO(image));
            }

            foreach (var i in imagedFragments)
            {
                if (imageDict.TryGetValue(i.Id, out var imagedFragment))
                    result.result.Add(ImagedFragmentModelToDTO(i, imagedFragment, null));
            }

            return result;
        }
        private string getFragmentId(DataAccess.Models.Image image)
        {
            return image.Institution + "-" + image.Catlog1 + "-" + image.Catalog2;
        }
        internal static ImagedFragmentDTO ImagedFragmentModelToDTO(DataAccess.Models.ImagedFragment model, List<ImageDTO> images, List<ArtefactDTO> artefact)
        {

            return new ImagedFragmentDTO
            {
                id = model.Id.ToString(),
                recto = getRecto(images),
                verso = getVerso(images),
                Artefacts = artefact
           
            };
        }
        private static ImageStackDTO getRecto(List<ImageDTO> images)
        {
            var img = new List<ImageDTO>();
            foreach (var image in images)
            {
                if (image.side == "recto")
                {
                    img.Add(image);
                }
            }
            if (img.Count == 0)
            {
                return null;
            }
            var masterIndex = img.FindIndex(i => i.master);
            var catalog_id = img[0].catalog_number;
            return new ImageStackDTO
            {
                id = catalog_id,
                masterIndex = masterIndex,
                images = img
            };
        }

        private static ImageStackDTO getVerso(List<ImageDTO> images)
        {
            var img = new List<ImageDTO>();
            foreach (var image in images)
            {
                if (image.side == "verso")
                {
                    img.Add(image);
                }
            }
            if (img.Count == 0)
            {
                return null;
            }
            var masterIndex = img.FindIndex(i => i.master);
            var catalog_id = img[0].catalog_number;
            return new ImageStackDTO
            {
                id = catalog_id,
                masterIndex = masterIndex,
                images = img
            };
        }

        async public Task<ImagedFragmentDTO> GetImagedFragment(uint? userId, uint scrollVersionId, string fragmentId)
        {
            var images = await _imageRepo.GetImages(userId, scrollVersionId, fragmentId); //send imagedFragment from here 
            var imagedFragments = await _repo.GetImagedFragments(userId, scrollVersionId, fragmentId); //should be only one!
            if(imagedFragments.Count() == 0){
                throw new NotFoundException(scrollVersionId);
            }
            var artefacts = await _artefactService.GetAtrefactAsync(userId,null, scrollVersionId, fragmentId);
            var img = new List<ImageDTO>();
            foreach (var image in images)
            {
                img.Add(_imageService.ImageToDTO(image));
                //var fragmentId = getFragmentId(image);
            }
            var result = ImagedFragmentModelToDTO(imagedFragments.First(), img, artefacts);

            return result;
        }
    }
}
