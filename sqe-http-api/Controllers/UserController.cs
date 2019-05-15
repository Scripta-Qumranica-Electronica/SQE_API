using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Helpers;


namespace SQE.SqeHttpApi.Server.Controllers
{

    [Produces("application/json")]
    [Authorize]
    [Route("v1/[controller]s")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private IUserService _userService;

        public UserController(IUserService userServiceAuthenticate)
        {
            _userService = userServiceAuthenticate;
        }
        
        /// <summary>
        /// Provides the user details for a user with valid JWT in the Authorize header
        /// </summary>
        [HttpGet]
        public ActionResult<UserDTO> GetCurrentUser()
        {
            var user = _userService.GetCurrentUser();

            if (user == null)
                return Unauthorized(new { message = "No current user", code =601 }); // TODO: Add Error Code

            return user;
        }
        
        /// <summary>
        /// Creates a new user with the submitted data.
        /// </summary>
        /// <param name="userInfo">A JSON object with all data necessary to create a new user account</param>
        /// <returns>Returns a UserDTO for the newly created account. Throws an HTTP 409 if the
        /// username or email is already in use.</returns>
        [AllowAnonymous]
        [HttpPost]
        public ActionResult<UserDTO> CreateNewUser([FromBody] NewUserDTO userInfo)
        {
            return Conflict(new { message = "An account with the username or email address already exists " });
        }

        /// <summary>
        /// Provides a JWT bearer token for valid username and password
        /// </summary>
        /// <param Name="userParam">JSON object with a username and password parameter</param>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> AuthenticateAsync([FromBody]LoginRequestDTO userParam)
        {
            var user = await _userService.AuthenticateAsync(userParam.userName, userParam.password);
            if (user == null)
                return Unauthorized(new { message = "Username or password is incorrect ", code = 600}); // TODO: Add Error Code

            return user;
        }
    }
    
    public class NewUserDTO
    {
        /// <summary>
        /// An object containing all data necessary to create a new user account
        /// </summary>
        /// <param name="userName">Short username for the new user account, must be unique</param>
        /// <param name="email">Email address for the new user account, must be unique</param>
        /// <param name="password">Password for the new user account</param>
        /// <param name="organization">Name of affiliated organization (if any)</param>
        /// <param name="forename">The user's given name (may be empty)</param>
        /// <param name="surname">The user's family name (may be empty)</param>
        public NewUserDTO(string userName, string email, string password, string organization, string forename, string surname)
        {
            this.userName = userName;
            this.email = email;
            this.password = password;
            this.organization = organization;
            this.forename = forename;
            this.surname = surname;
        }

        public string userName { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string organization { get; set; }
        public string forename { get; set; }
        public string surname { get; set; }
    }
}
