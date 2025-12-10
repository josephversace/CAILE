using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IIM.Shared.Models;

namespace IIM.Infrastructure.Data
{
	public class AuthDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
	{
		public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<ApplicationUser>(b =>
			{
				b.Property(u => u.IsActive).HasDefaultValue(true);
			});
		}
	}
}
