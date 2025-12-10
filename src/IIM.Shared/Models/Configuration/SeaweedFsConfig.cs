namespace IIM.Shared.Models;

public class SeaweedFsConfig
{
	public string MasterUrl { get; set; } = "";
	public string FilerUrl { get; set; } = "";
	public string S3Url { get; set; } = "";
	public string AccessKey { get; set; } = "";
	public string SecretKey { get; set; } = "";

	public SeaweedFsBuckets Buckets { get; set; } = new();
}

public class SeaweedFsBuckets
{
	public string Primary { get; set; } = "";
	public string Quarantine { get; set; } = "";
	public string Derived { get; set; } = "";
}
