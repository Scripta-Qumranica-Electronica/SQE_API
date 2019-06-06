﻿using System;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class Edition
    {
        public uint EditionId { get; set; }
        public string Name { get; set; }
        public string ScrollId { get; set; }
        public Permission Permission { get; set; }
        public string Thumbnail { get; set; }
        public bool Locked { get; set; }
        public bool IsPublic {get; set;}
        public DateTime? LastEdit { get; set; }
        public User Owner { get; set; }
        public string Copyright { get; set; }
        public string CopyrightHolder { get; set; }
        public string Collaborators { get; set; }
    }

    public class ScrollName
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public uint ScrollId { get; set; }
        public uint ScrollDataId { get; set; }
    }

    public class Permission
    {
        public bool CanWrite { get; set; }
        public bool CanLock { get; set; }
        public bool CanAdmin { get; set; }
    }

    public class Share
    {
        public UserToken UserToken { get; set; }
        public Permission Permission { get; set; }
    }

    
    public class Artefact
    {
        public string Id { get; set; }
        public string ScrollVersionId { get; set; }
        public string ImagedFragmentId { get; set; }
        public string Name { get; set; }
        public Polygon Mask { get; set; }
        public string TransformMatrix { get; set; }
        public string Zorder { get; set; }
        public string Size { get; set; }
    }

    public class Polygon
    {
        public string wkt { get; set; }
    }
}