namespace IIM.Shared.Models;

public class Neo4jConfig
{
	public string Url { get; set; } = "";
	public string HttpUrl { get; set; } = "";
	public string Username { get; set; } = "";
	public string Password { get; set; } = "";
	public string Database { get; set; } = "neo4j";
	public int TimeoutSeconds { get; set; }
}
