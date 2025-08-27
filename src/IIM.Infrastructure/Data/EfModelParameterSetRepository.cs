using DocumentFormat.OpenXml.Office2010.Excel;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Data
{
    public class EfModelParameterSetRepository : IModelParameterSetRepository
    {
        private readonly ModelDbContext _db;

        public EfModelParameterSetRepository(ModelDbContext db)
        {
            _db = db;
        }

        public async Task<List<ModelParameterSet>> GetByModelIdAsync(string modelId, CancellationToken cancellationToken = default)
            => await _db.ModelParameterSets
                .AsNoTracking()
                .Where(p => p.ModelId == modelId)
                .ToListAsync(cancellationToken);

        public async Task<ModelParameterSet?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => await _db.ModelParameterSets
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        public async Task AddAsync(ModelParameterSet parameterSet, CancellationToken cancellationToken = default)
        {
            _db.ModelParameterSets.Add(parameterSet);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(ModelParameterSet parameterSet, CancellationToken cancellationToken = default)
        {
            _db.ModelParameterSets.Update(parameterSet);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.ModelParameterSets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (entity != null)
            {
                _db.ModelParameterSets.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

    }
}
