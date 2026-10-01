using ITFPulse.Application;
using ITFPulse.Infrastructure;
using ITFPulse.Infrastructure.Messaging;
using ITFPulse.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var initialize = args.Contains("--initialize");
var builder = Host.CreateApplicationBuilder(args.Where(a => a != "--initialize").ToArray());
builder.Configuration.AddEnvironmentVariables("ITFPULSE_");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeedMessaging(builder.Configuration, worker: true, deployOnly: initialize);
using var host = builder.Build();
if (initialize)
{
    // Explicit, one-shot deployment step. Never race migrations in API/worker replicas.
    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    await using var scope = host.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<ITFPulseDbContext>().Database.MigrateAsync(timeout.Token);
    // Provision every queue/binding before accepting posts, so published events have a subscription.
    await host.Services.GetRequiredService<IBusControl>().DeployAsync(timeout.Token);
    return;
}
// Normal mode starts the consumers selected by Messaging:Role and waits for shutdown.
await host.RunAsync();
