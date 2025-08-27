using IIM.Infrastructure.Data;
using IIM.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Infrastructure.Data
{
    public static class DatabaseServiceExtensions
    {
        /// <summary>
        /// Adds SQLite database support with Entity Framework Core
        /// </summary>
        public static IServiceCollection AddIIMDatabases(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            services.AddDbContext<AuditDbContext>(options =>
                   options.UseSqlite(configuration.GetConnectionString("AuditDb"))); 
            services.AddScoped<IAuditRepository, EfAuditRepository>();


            services.AddDbContext<ConfigDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("ConfigDb")));
            services.AddScoped<IConfigRepository, EfConfigRepository>();


            services.AddDbContext<ModelDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("ModelDb")));
            services.AddScoped<IModelRepository, EfModelRepository>();

            services.AddScoped<IModelParameterSetRepository, EfModelParameterSetRepository>();


            return services;
        }
    }
}
