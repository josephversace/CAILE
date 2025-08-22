using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Core.Configuration
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

        // Example audit entity:
        public DbSet<AuditEvent> AuditLogs { get; set; }
    }

    public class AuditConfiguration
    {
        public string LogPath { get; set; } = @"C:\ProgramData\IIM\Audit";
        public bool EnableDetailedLogging { get; set; }
        public int RetentionDays { get; set; }
        public bool RequireTamperEvidence { get; set; }
        public string LogLevel { get; set; }
        public bool IncludeRequestBody { get; set; }
        public bool IncludeResponseBody { get; set; }
        public bool SensitiveDataMasking { get; set; }
    }

}
