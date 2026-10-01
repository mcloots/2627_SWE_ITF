using ITFPulse.Application.Feeds;
using ITFPulse.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITFPulse.Infrastructure.Messaging;

public static class MessagingSetup
{
    // Stable queue names let worker replicas compete for the same work instead of each receiving a copy.
    public const string EventQueue = "itfpulse-post-created-v1";
    public const string PlanQueue = "itfpulse-plan-fanout-v1";
    public const string DeliveryQueue = "itfpulse-deliver-feed-v1";

    public static IServiceCollection AddFeedMessaging(this IServiceCollection services,
        IConfiguration configuration, bool worker = false, bool deployOnly = false)
    {
        // The API only publishes; worker roles allow planning and delivery to scale independently.
        var role = configuration["Messaging:Role"] ?? "all";
        if (role is not ("all" or "planner" or "delivery"))
            throw new InvalidOperationException("Messaging:Role must be all, planner or delivery.");
        var concurrency = int.Parse(configuration["Messaging:Concurrency"] ?? "8");
        var prefetch = int.Parse(configuration["Messaging:Prefetch"] ?? "16");
        if (concurrency is < 1 or > 128 || prefetch < concurrency || prefetch > 512)
            throw new InvalidOperationException("Messaging requires concurrency 1–128 and prefetch between concurrency and 512.");

        services.AddScoped<IFeedWorkQueue, FeedWorkQueue>();
        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<ITFPulseDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.QueryDelay = TimeSpan.FromMilliseconds(250);
                // Inbox deduplication is time-limited; the feed's unique key also protects against later replays.
                outbox.DuplicateDetectionWindow = TimeSpan.FromHours(1);
                // API-scoped Publish writes to the post's DbContext. A background service forwards
                // committed outbox messages to RabbitMQ and retries when the broker is unavailable.
                if (!worker) outbox.UseBusOutbox();
            });
            if (worker)
            {
                if (role is "all" or "planner")
                {
                    bus.AddConsumer<PostCreatedConsumer>();
                    bus.AddConsumer<PlanPostFanoutConsumer>();
                }
                if (role is "all" or "delivery") bus.AddConsumer<DeliverPostToFollowersConsumer>();
            }
            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(configuration["Messaging:Host"] ?? "localhost", "/", host =>
                {
                    host.Username(configuration["Messaging:Username"] ?? "pulse");
                    host.Password(configuration["Messaging:Password"]
                        ?? throw new InvalidOperationException("Messaging:Password is required."));
                });
                // Declare subscriptions before the API can publish, without consuming work during initialization.
                rabbit.DeployTopologyOnly = deployOnly;
                if (!worker) return;

                void Configure(IRabbitMqReceiveEndpointConfigurator endpoint)
                {
                    // Three is the requested replica count; the single-node local broker is not highly available.
                    endpoint.SetQuorumQueue(3);
                    // Prefetch bounds in-flight deliveries; concurrency bounds active handlers per endpoint.
                    endpoint.PrefetchCount = (ushort)prefetch;
                    endpoint.ConcurrentMessageLimit = concurrency;
                    // Invalid payloads skip retries; exhausted failures go to the endpoint's _error queue
                    // through MassTransit's default error handling.
                    endpoint.UseMessageRetry(retry =>
                    {
                        retry.Ignore<ArgumentException>();
                        retry.Intervals(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1));
                    });
                    // Commit inbox state, database effects and outgoing commands together.
                    // Deliver outgoing messages before acknowledging successful consumption.
                    endpoint.UseEntityFrameworkOutbox<ITFPulseDbContext>(context);
                }
                if (role is "all" or "planner")
                {
                    rabbit.ReceiveEndpoint(EventQueue, endpoint =>
                    {
                        Configure(endpoint);
                        endpoint.ConfigureConsumer<PostCreatedConsumer>(context);
                    });
                    rabbit.ReceiveEndpoint(PlanQueue, endpoint =>
                    {
                        Configure(endpoint);
                        endpoint.ConfigureConsumer<PlanPostFanoutConsumer>(context);
                    });
                }
                if (role is "all" or "delivery")
                    rabbit.ReceiveEndpoint(DeliveryQueue, endpoint =>
                    {
                        Configure(endpoint);
                        endpoint.ConfigureConsumer<DeliverPostToFollowersConsumer>(context);
                    });
            });
        });
        return services;
    }
}
