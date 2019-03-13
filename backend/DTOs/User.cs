using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace SQE.Backend.Server.DTOs
{
    public class LoginRequest 
    {
        [Required]
        public string userName { get; set; }

        [Required]
        public string password { get; set; }
    }

    public class LoginResponse
    {
        public string userName { get; set; }
        public string token { get; set; }
        public int userId { get; set; }
    }
}
