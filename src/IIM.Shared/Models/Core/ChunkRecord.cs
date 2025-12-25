// src/IIM.Shared/Models/ChunkRecord.cs
using System.Collections.Generic;

namespace IIM.Shared.Models;

public sealed record ChunkRecord(
	string Blake3Hash,
	int ChunkIndex,
	string Text,
	List<string>? EntityIds = null
);