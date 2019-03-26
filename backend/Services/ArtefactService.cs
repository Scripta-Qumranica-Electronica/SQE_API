﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.Backend.Server.DTOs;
using SQE.Backend.DataAccess;



namespace SQE.Backend.Server.Services
{
    public interface IArtefactService
    {
        Task<Artefact> GetAtrefact(int? userId);

    }
    public class ArtefactService : IArtefactService
    {
        public Task<Artefact> GetAtrefact(int? userId)
        {
            throw new NotImplementedException();
        }
    }
}
