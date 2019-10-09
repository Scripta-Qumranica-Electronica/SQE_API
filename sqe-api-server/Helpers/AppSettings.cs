namespace SQE.API.Server.Helpers
{
	public class AppSettings
	{
		public string Secret { get; set; }
		public bool UseRedis { get; set; }
		public bool HttpServer { get; set; }
	}
}