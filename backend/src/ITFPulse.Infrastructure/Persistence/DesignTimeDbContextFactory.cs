using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ITFPulse.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ITFPulseDbContext>
{
    public ITFPulseDbContext CreateDbContext(string[] args)
    {
        // Model generation needs no running database. Applying a migration requires explicit configuration.
        var connection = Environment.GetEnvironmentVariable("ITFPULSE_ConnectionStrings__ITFPulse")
            ?? "Host=localhost;Database=design_time_only";
        return new(new DbContextOptionsBuilder<ITFPulseDbContext>().UseNpgsql(connection).Options);
    }
}
