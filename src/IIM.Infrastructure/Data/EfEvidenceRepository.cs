// File: IIM.Infrastructure.Data/EfAuditRepository.cs
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Data
{
    public class EfEvidenceRepository
    {
        private readonly EvidenceDbContext _db;

        public EfEvidenceRepository(EvidenceDbContext db)
        {
            _db = db;
        }



    }
}
