namespace IIM.Shared.Models;

public class QdrantConfig
{
	public string Host { get; set; } = "localhost";
	public int GrpcPort { get; set; } = 6334;
	public int HttpPort { get; set; } = 6333;
	public string ApiKey { get; set; } = "";
	public string DefaultCollection { get; set; } = "documents";
	public uint VectorSize { get; set; } = 768; 
	public int TimeoutSeconds { get; set; } = 30;
	public bool UseTls { get; set; } = false;
}