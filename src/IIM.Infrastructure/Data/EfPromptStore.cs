using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using IIM.Shared.Interfaces;
using IIM.Shared.Models.Configuration;

namespace IIM.Infrastructure.Data
{
public sealed class EfPromptStore : IPromptStore
	{
		private const string Category = "Prompts";
		private readonly IConfigRepository _repo;

		public EfPromptStore(IConfigRepository repo)
		{
			_repo = repo;
		}

		public async Task<IReadOnlyDictionary<string, PromptDefinition>> GetAllAsync(
			CancellationToken ct = default)
		{
			var settings = await _repo.GetAllAsync(ct);

			return settings
				.Where(s => s.Category == Category)
				.ToDictionary(
					s => s.Key.Replace("prompt.", ""),
					s => JsonSerializer.Deserialize<PromptDefinition>(s.Value)!);
		}

		public async Task<PromptDefinition?> GetAsync(
			string promptId,
			CancellationToken ct = default)
		{
			return await _repo.GetJsonAsync<PromptDefinition>(
				key: $"prompt.{promptId}",
				ct);
		}

		public async Task SaveAsync(
			PromptDefinition prompt,
			CancellationToken ct = default)
		{
			await _repo.SetJsonAsync(
				key: $"prompt.{prompt.Id}",
				value: prompt,
				category: Category,
				ct: ct);
		}

		public async Task DeleteAsync(string promptId,CancellationToken ct = default)
		{
			await _repo.DeleteAsync($"prompt.{promptId}", ct);
		}

		public async Task<bool> ExistsAsync(string promptId,CancellationToken ct = default)
		{
			var setting = await _repo.GetByKeyAsync($"prompt.{promptId}", ct);
			return setting != null;
		}

		public async Task<IReadOnlyList<(string Id, DateTimeOffset UpdatedAt)>> ListAsync(
	CancellationToken ct = default)
		{
			var settings = await _repo.GetAllAsync(ct);

			return settings
				.Where(s => s.Category == Category)
				.Select(s => (
					Id: s.Key.Replace("prompt.", ""),
					UpdatedAt: s.UpdatedAt
				))
				.ToList();
		}


	}

}
