namespace SQE.API.DTO;

public class ServiceStatusDTO
{
	public string service       { get; set; }
	public string statusMessage { get; set; }
	public bool   isHealthy     { get; set; }
}
