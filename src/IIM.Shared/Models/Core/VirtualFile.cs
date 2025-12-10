using System;
using System.Collections.Generic;
using System.Text.Json;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;

public class VirtualFile
{
	public Guid Id { get; set; }
	public Guid WorkspaceId { get; set; }

	public string FileName { get; set; } = string.Empty;
	public string Path { get; set; } = "/";  // arbitrary virtual path
	public long FileSize { get; set; }
	public FileUploadStatus Status { get; set; }

	// Points to StoredFile.Blake3Hash
	public string? StoredFileHash { get; set; }
	public StoredFile? StoredFile { get; set; }

	public DateTime CreatedAt { get; set; }

	public List<string> Tags { get; set; } = new();
	public string? ProposedLabel { get; set; }   // AI suggestion
	public Dictionary<string, string> EnrichmentMetadata { get; set; } = new();


	// Metadata
	public Dictionary<string, string> CustomMetadata { get; set; } = new();
	public string CustomMetadataJson
	{
		get => JsonSerializer.Serialize(CustomMetadata);
		set => CustomMetadata =
			string.IsNullOrWhiteSpace(value)
				? new()
				: JsonSerializer.Deserialize<Dictionary<string, string>>(value)!;
	}

	public List<ChainOfCustodyEntry> ChainOfCustody { get; set; } = new();



}
