using ITFPulse.Application.Abstractions;
using ITFPulse.Application.Abstractions.Persistence;
using ITFPulse.Infrastructure.Persistence;
using ITFPulse.Infrastructure.Persistence.Repositories;
using ITFPulse.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITFPulse.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString =
                configuration.GetConnectionString("ITFPulse")
                ?? throw new InvalidOperationException(
                    "Connection string 'ITFPulse' was not found.");

            services.AddDbContext<ITFPulseDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IPostRepository, PostRepository>();

            services.AddSingleton<IClock, SystemClock>();

            return services;
        }
    }
}
