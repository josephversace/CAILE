public class SecretsModel
{
	// API/Setup
	public string SetupToken { get; set; } = "";
	public string JwtKey { get; set; } = "";
	public string DbPassword { get; set; } = "";

	public string Neo4jPassword { get; set; }

	public string SearxngSecret { get; set; } = "";
	// SeaweedFS
	public SeaweedSecrets Seaweed { get; set; } = new();
}

public class SeaweedSecrets
{
	// S3 API access
	public string S3AccessKey { get; set; } = "";
	public string S3SecretKey { get; set; } = "";

	// Internal component auth
	public string JwtSigningKey { get; set; } = "";
	public string JwtSigningReadKey { get; set; } = "";

	// Volume encryption
	public string EncryptionKey { get; set; } = "";
}