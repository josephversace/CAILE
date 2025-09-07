// File: IIM.Infrastructure.Data/EfAuditRepository.cs
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Data
{
    public class EfFileRepository
    {
        private readonly FileDbContext _db;

        public EfFileRepository(FileDbContext db)
        {
            _db = db;
        }



    }
}
