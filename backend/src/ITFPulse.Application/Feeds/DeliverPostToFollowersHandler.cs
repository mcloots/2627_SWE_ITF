using ITFPulse.Contracts.Messaging;

namespace ITFPulse.Application.Feeds;

public sealed class DeliverPostToFollowersHandler(IFeedStore store)
{
    public Task Handle(DeliverPostToFollowersV1 command, CancellationToken cancellationToken)
    {
        if (command.PostId == Guid.Empty || command.FollowerIds is not { Length: > 0 and <= PlanPostFanoutHandler.BatchSize }
            || command.FollowerIds.Any(id => id == Guid.Empty))
            throw new ArgumentException("A delivery requires a post and 1–500 valid followers.");
        return store.DeliverAsync(command.PostId, command.CreatedAt, command.FollowerIds, cancellationToken);
    }
}
