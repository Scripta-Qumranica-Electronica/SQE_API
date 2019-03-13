using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.Backend.DataAccess.Models
{
    public class ScrollVersion
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Permission Permission { get; set; }
        public string Thumbnail { get; set; }
        public bool Locked { get; set; }
        public bool IsPublic {get; set;}
        public DateTime? LastEdit { get; set; }
        public User Owner { get; set; }
    }

    public class Permission
    {
        public bool CanWrite { get; set; }
        public bool CanAdmin { get; set; }
    }

    public class Share
    {
        public User User { get; set; }
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
        public string svg { get; set; }
    }
}
