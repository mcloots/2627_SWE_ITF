using ITFPulse.Contracts.Messaging;

namespace ITFPulse.Application.Feeds;

public interface IFeedWorkQueue
{
    Task PlanAsync(PlanPostFanoutV1 command, CancellationToken cancellationToken);
    Task DeliverAsync(DeliverPostToFollowersV1 command, CancellationToken cancellationToken);
}
