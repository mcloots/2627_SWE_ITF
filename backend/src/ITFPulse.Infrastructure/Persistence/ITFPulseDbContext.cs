using ITFPulse.Domain.Posts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Infrastructure.Persistence
{
    public sealed class ITFPulseDbContext : DbContext
    {
        public ITFPulseDbContext(DbContextOptions<ITFPulseDbContext> options) : base(options)
        {
        }

        public DbSet<Post> Posts => Set<Post>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("itfpulse");

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(ITFPulseDbContext).Assembly);
        }
    }
}
