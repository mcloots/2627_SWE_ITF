using ITFPulse.Application.Feeds;
using ITFPulse.Contracts.Messaging;
using MassTransit;

namespace ITFPulse.Infrastructure.Messaging;

internal sealed class FeedWorkQueue(ISendEndpointProvider endpoints) : IFeedWorkQueue
{
    public async Task PlanAsync(PlanPostFanoutV1 command, CancellationToken cancellationToken)
    {
        // Topology is provisioned by --initialize. Sending to its exchange avoids
        // accidentally redeclaring the existing quorum queue as a classic queue.
        var endpoint = await endpoints.GetSendEndpoint(new Uri($"exchange:{MessagingSetup.PlanQueue}"));
        await endpoint.Send(command, cancellationToken);
    }

    public async Task DeliverAsync(DeliverPostToFollowersV1 command, CancellationToken cancellationToken)
    {
        var endpoint = await endpoints.GetSendEndpoint(new Uri($"exchange:{MessagingSetup.DeliveryQueue}"));
        await endpoint.Send(command, cancellationToken);
    }
}
