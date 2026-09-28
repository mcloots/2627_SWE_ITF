using ITFPulse.Contracts.Messaging;

namespace ITFPulse.Application.Feeds;

public sealed class PlanPostFanoutHandler(IFeedStore store, IFeedWorkQueue queue)
{
    public const int BatchSize = 500;

    public async Task Handle(PlanPostFanoutV1 command, CancellationToken cancellationToken)
    {
        if (command.PostId == Guid.Empty || command.AuthorId == Guid.Empty || command.AfterFollowerId < 0)
            throw new ArgumentException("Invalid fan-out command.");
        var followers = await store.GetFollowersAsync(command.AuthorId, command.CreatedAt,
            command.AfterFollowerId, BatchSize, cancellationToken);
        if (followers.Count == 0) return;
        await queue.DeliverAsync(new(command.PostId, command.CreatedAt,
            followers.Select(f => f.FollowerId).ToArray()), cancellationToken);
        if (followers.Count == BatchSize)
            await queue.PlanAsync(command with { AfterFollowerId = followers[^1].Id }, cancellationToken);
        // Consumer outbox commits both commands atomically before acknowledging this page.
    }
}
