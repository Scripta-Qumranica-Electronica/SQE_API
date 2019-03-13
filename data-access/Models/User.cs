using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.Backend.DataAccess.Models
{
    public class User
    {
        public string UserName { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
    }
}
