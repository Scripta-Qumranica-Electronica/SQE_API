using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.Backend.Server.DTOs;
using SQE.Backend.DataAccess;



namespace SQE.Backend.Server.Services
{
    public interface IArtefactService
    {
        Task<ArtefactListDTO> GetAtrefactAsync(uint? userId, int? artefactId, int? scrollVersionId);

    }
    public class ArtefactService : IArtefactService
    {
        IArtefactRepository _repo;

        public ArtefactService(IArtefactRepository repo)
        {
            _repo = repo;
        }

        public async Task<ArtefactListDTO> GetAtrefactAsync(uint? userId, int? artefactId, int? scrollVersionId)
        {

            var artefacts = await _repo.GetArtefact(userId, artefactId, scrollVersionId);

            if (artefacts == null)
            {
                throw new NotFoundException((uint)scrollVersionId);
            }
            var result = new ArtefactListDTO
            {
                ArtefactList = new List<ArtefactDTO>(),
            };


            foreach (var a in artefacts)
            {
                result.ArtefactList.Add(ArtefactToDTO(a));
            }

            return result;
        }

        public ArtefactDTO ArtefactToDTO(DataAccess.Models.Artefact model)
        {

            return new ArtefactDTO
            {
                id = model.Id,
                imageFragmentId = model.ImagedFragmentId,
                scrollVersionId = model.ScrollVersionId,
                name = model.Name,
                zOrder = model.Zorder,
                side = ArtefactDTO.artSide.recto,
                transformMatrix = model.TransformMatrix,
                mask = new PolygonDTO()

            };
        }

    }
}
