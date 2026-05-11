using IIM.Shared.Interfaces;
using IIM.Shared.Models.Configuration;
using IIM.Shared.Models.Core;

namespace IIM.Api.Services
{
	public sealed class PromptSnapshotProvider : IPromptSnapshotProvider
	{
		private readonly IPromptStore _store;
		private PromptSnapshot? _cached;

		public PromptSnapshotProvider(IPromptStore store)
		{
			_store = store;
		}

		public async Task<PromptSnapshot> GetSnapshotAsync(
			bool forceReload = false,
			CancellationToken ct = default)
		{
			if (_cached is not null && !forceReload)
				return _cached;

			// 1️⃣ Start with defaults
			var merged = new Dictionary<string, PromptDefinition>(
				PromptDefaults.All,
				StringComparer.OrdinalIgnoreCase);

			// 2️⃣ Overlay DB overrides
			var overrides = await _store.GetAllAsync(ct);
			foreach (var kv in overrides)
			{
				merged[kv.Key] = kv.Value;
			}

			_cached = new PromptSnapshot(merged);
			return _cached;
		}
	}
}
