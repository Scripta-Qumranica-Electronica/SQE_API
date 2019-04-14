using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class User
    {
        public string UserName { get; set; }
        public uint UserId { get; set; }
        public string Token { get; set; }
    }
}
