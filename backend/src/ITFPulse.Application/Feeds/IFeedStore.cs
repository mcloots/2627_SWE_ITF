using ITFPulse.Domain.Followers;

namespace ITFPulse.Application.Feeds;

public sealed record FollowerPageItem(long Id, Guid FollowerId);
public sealed record FeedPost(long Sequence, Guid PostId, Guid AuthorId, string Content, DateTimeOffset CreatedAt);
public sealed record FanoutProgress(long Posts, long Followers, long FeedEntries);

public interface IFeedStore
{
    Task FollowAsync(Follow follow, CancellationToken cancellationToken);
    Task<IReadOnlyList<FollowerPageItem>> GetFollowersAsync(Guid authorId, DateTimeOffset cutoff,
        long afterId, int limit, CancellationToken cancellationToken);
    Task DeliverAsync(Guid postId, DateTimeOffset createdAt, Guid[] followerIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<FeedPost>> ReadAsync(Guid followerId, long? beforeSequence, int limit, CancellationToken cancellationToken);
    Task<FanoutProgress> GetProgressAsync(Guid authorId, CancellationToken cancellationToken);
}
