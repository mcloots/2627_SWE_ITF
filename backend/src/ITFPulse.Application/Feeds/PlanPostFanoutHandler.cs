using ITFPulse.Contracts.Messaging;

namespace ITFPulse.Application.Feeds;

public sealed class PlanPostFanoutHandler(IFeedStore store, IFeedWorkQueue queue)
{
    // Bound query size, message payloads and retry cost even for authors with many followers.
    public const int BatchSize = 500;

    public async Task Handle(PlanPostFanoutV1 command, CancellationToken cancellationToken)
    {
        if (command.PostId == Guid.Empty || command.AuthorId == Guid.Empty || command.AfterFollowerId < 0)
            throw new ArgumentException("Invalid fan-out command.");
        // Exclude later follows. Each page reads current relationships, not one historical snapshot.
        var followers = await store.GetFollowersAsync(command.AuthorId, command.CreatedAt,
            command.AfterFollowerId, BatchSize, cancellationToken);
        if (followers.Count == 0) return;
        await queue.DeliverAsync(new(command.PostId, command.CreatedAt,
            followers.Select(f => f.FollowerId).ToArray()), cancellationToken);
        // Continue by key rather than OFFSET. An exact multiple of BatchSize adds one harmless empty page.
        if (followers.Count == BatchSize)
            await queue.PlanAsync(command with { AfterFollowerId = followers[^1].Id }, cancellationToken);
        // Consumer outbox commits both commands atomically before acknowledging this page.
    }
}
