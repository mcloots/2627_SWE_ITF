using ITFPulse.Contracts.Messaging;

namespace ITFPulse.Application.Feeds;

// Application port for scheduling work; infrastructure supplies routing and outbox semantics.
public interface IFeedWorkQueue
{
    Task PlanAsync(PlanPostFanoutV1 command, CancellationToken cancellationToken);
    Task DeliverAsync(DeliverPostToFollowersV1 command, CancellationToken cancellationToken);
}
