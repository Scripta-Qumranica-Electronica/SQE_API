namespace SQE.API.Server.Helpers
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string UseRedis { get; set; }
        public string HttpServer { get; set; }
        public uint EmailTokenDaysValid { get; set; }
    }
}