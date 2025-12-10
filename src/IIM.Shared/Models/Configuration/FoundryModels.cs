namespace IIM.Shared.Models
{
	public class FoundryTierProfile
	{
		public string Id { get; init; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public string MaxChatModel { get; set; }
		public string SupportsVision { get; set; }
		public string Acceleration { get; set; }
		public string VramRequired { get; set; }

		public FoundryTierProfile() { }

		public FoundryTierProfile(string id, string displayName, string desc,
								 string maxChat, string vision, string accel, string vram)
		{
			Id = id;
			DisplayName = displayName;
			Description = desc;
			MaxChatModel = maxChat;
			SupportsVision = vision;
			Acceleration = accel;
			VramRequired = vram;
		}
	}
}
