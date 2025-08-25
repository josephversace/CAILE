using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Data
{
    public class EfModelRepository : IModelRepository
    {
        private readonly ModelDbContext _db;

        public EfModelRepository(ModelDbContext db)
        {
            _db = db;
        }

        public async Task<List<ModelConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _db.ModelConfigurations.AsNoTracking().ToListAsync(cancellationToken);

        public async Task<ModelConfiguration?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => await _db.ModelConfigurations.AsNoTracking().FirstOrDefaultAsync(m => m.ModelId == id, cancellationToken);

        public async Task AddAsync(ModelConfiguration config, CancellationToken cancellationToken = default)
        {
            _db.ModelConfigurations.Add(config);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ModelConfiguration config, CancellationToken cancellationToken = default)
        {
            _db.ModelConfigurations.Update(config);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.ModelConfigurations.FirstOrDefaultAsync(m => m.ModelId == id, cancellationToken);
            if (entity != null)
            {
                _db.ModelConfigurations.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
