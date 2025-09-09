using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Infrastructure.Data
{
    public class FileDbContext : DbContext
    {
        public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }

        // Example audit entity:
        public DbSet<ManagedFile> ManagedFiles { get; set; }
        public DbSet<FileMetadata> FileMetadatas { get; set; }

        public DbSet<WorkspaceFolder> Folders { get; set; }
        public DbSet<ChainOfCustodyEntry> CustodyChains { get; set; }

    }
}
