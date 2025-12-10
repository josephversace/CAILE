namespace IIM.Shared.Dtos
{
	public class LocalModelInfoDto
	{
		public string Name { get; set; } = "";
		public string Path { get; set; } = "";
		public long SizeBytes { get; set; }
		public string? Description { get; set; }
	}
}
