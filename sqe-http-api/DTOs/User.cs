using System.ComponentModel.DataAnnotations;


namespace SQE.SqeHttpApi.Server.DTOs
{
    public class LoginRequestDTO
    {
        [Required]
        public string userName { get; set; }

        [Required]
        public string password { get; set; }
    }

    public class UserDTO
    {
        public uint userId { get; set; }
        public string userName { get; set; }
    }

    public class LoginResponseDTO : UserDTO
    {
        public string token { get; set; }
    }
}
