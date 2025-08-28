using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Infrastructure.Data
{
    public class EvidenceDbContext : DbContext
    {
        public EvidenceDbContext(DbContextOptions<EvidenceDbContext> options) : base(options) { }

        // Example audit entity:
        public DbSet<Evidence> Evidence { get; set; }
        public DbSet<EvidenceMetadata> Metadata { get; set; }

        public DbSet<ChainOfCustodyEntry> CustodyChains { get; set; }

    }
}
