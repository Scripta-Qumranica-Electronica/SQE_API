using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SQE.SqeHttpApi.Server.DTOs;

namespace api_test.Helpers
{
    public static class Login
    {
        public static async Task<string> GetJWT(HttpClient client)
        {
            const string name = "test";
            const string password = "asdf";
            var login = new LoginRequestDTO(){ userName = name, password = password};
            var json = JsonConvert.SerializeObject(login);
            var stringContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
            var response = await client.PostAsync("/v1/users/login", stringContent);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var parsedClass = JsonConvert.DeserializeObject<LoginResponseDTO>(responseBody);
            return parsedClass.token;
        }
    }
}