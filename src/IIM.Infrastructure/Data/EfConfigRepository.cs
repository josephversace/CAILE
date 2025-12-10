using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Data
{
	public class EfConfigRepository : IConfigRepository
	{
		private readonly ConfigDbContext _db;

		public EfConfigRepository(ConfigDbContext db)
		{
			_db = db;
		}

		public async Task<List<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
			=> await _db.Settings.AsNoTracking().ToListAsync(cancellationToken);

		public async Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
			=> await _db.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

		public async Task AddAsync(Setting setting, CancellationToken cancellationToken = default)
		{
			_db.Settings.Add(setting);
			await _db.SaveChangesAsync(cancellationToken);
		}

		public async Task UpdateAsync(Setting setting, CancellationToken cancellationToken = default)
		{
			_db.Settings.Update(setting);
			await _db.SaveChangesAsync(cancellationToken);
		}

		public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
		{
			var entity = await _db.Settings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
			if (entity != null)
			{
				_db.Settings.Remove(entity);
				await _db.SaveChangesAsync(cancellationToken);
			}
		}

		public async Task SetJsonAsync<T>(
	string key,
	T value,
	string category,
	CancellationToken ct = default)
		{
			var json = JsonSerializer.Serialize(value);

			// Try to find an existing setting by key
			var existing = await _db.Settings
				.SingleOrDefaultAsync(s => s.Key == key, ct);

			if (existing is null)
			{
				// Create new
				var setting = new Setting
				{
					Key = key,
					Value = json,
					Category = category,
					UpdatedAt = DateTimeOffset.UtcNow,

				};

				_db.Settings.Add(setting);
			}
			else
			{
				// Update existing
				existing.Value = json;

				existing.UpdatedAt = DateTimeOffset.UtcNow;


				_db.Settings.Update(existing);
			}

			await _db.SaveChangesAsync(ct);
		}



		public async Task<T?> GetJsonAsync<T>(string key, CancellationToken ct = default)
		{
			var setting = await GetByKeyAsync(key, ct);
			string json = setting?.Value;
			if (string.IsNullOrWhiteSpace(json))
				return default;

			return JsonSerializer.Deserialize<T>(json);
		}

	}
}