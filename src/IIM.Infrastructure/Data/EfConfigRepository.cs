using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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
    }
}
