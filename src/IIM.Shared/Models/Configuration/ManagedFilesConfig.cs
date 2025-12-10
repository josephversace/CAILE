namespace IIM.Shared.Models;

public class ManagedFilesConfig
{
	public bool EnableEncryption { get; set; }
	public int MaxFileSizeMb { get; set; }
	public bool ChainOfCustodyRequired { get; set; }
	public int IntegrityCheckInterval { get; set; }
}
