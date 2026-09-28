using ITFPulse.Application.Feeds;
using ITFPulse.Domain.Followers;
using Microsoft.EntityFrameworkCore;

namespace ITFPulse.Infrastructure.Persistence.Repositories;

internal sealed class FeedStore(ITFPulseDbContext db) : IFeedStore
{
    public async Task FollowAsync(Follow follow, CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO itfpulse.follows (author_id, follower_id, followed_at)
            VALUES ({follow.AuthorId}, {follow.FollowerId}, {follow.FollowedAt})
            ON CONFLICT (author_id, follower_id) DO NOTHING
            """, cancellationToken);

    public async Task<IReadOnlyList<FollowerPageItem>> GetFollowersAsync(Guid authorId,
        DateTimeOffset cutoff, long afterId, int limit, CancellationToken cancellationToken) =>
        await db.Follows.AsNoTracking()
            .Where(x => x.AuthorId == authorId && x.Id > afterId && x.FollowedAt <= cutoff)
            .OrderBy(x => x.Id).Take(limit)
            .Select(x => new FollowerPageItem(x.Id, x.FollowerId)).ToListAsync(cancellationToken);

    public async Task DeliverAsync(Guid postId, DateTimeOffset createdAt, Guid[] followerIds,
        CancellationToken cancellationToken) =>
        // One parameterized, set-based statement per batch; no 500 tracked EF entities.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO itfpulse.feed_entries (follower_id, post_id, created_at)
            SELECT follower_id, {postId}, {createdAt} FROM unnest({followerIds}) AS follower_id
            ON CONFLICT (follower_id, post_id) DO NOTHING
            """, cancellationToken);

    public async Task<IReadOnlyList<FeedPost>> ReadAsync(Guid followerId, long? beforeSequence,
        int limit, CancellationToken cancellationToken) =>
        await (from entry in db.FeedEntries.AsNoTracking()
               join post in db.Posts.AsNoTracking() on entry.PostId equals post.Id
               where entry.FollowerId == followerId && (!beforeSequence.HasValue || entry.Sequence < beforeSequence.Value)
               orderby entry.Sequence descending
               select new FeedPost(entry.Sequence, post.Id, post.AuthorId,
                   post.PostContent.Value, post.CreatedAt.Value))
            .Take(limit).ToListAsync(cancellationToken);

    public async Task<FanoutProgress> GetProgressAsync(Guid authorId, CancellationToken cancellationToken)
    {
        var posts = await db.Posts.LongCountAsync(x => x.AuthorId == authorId, cancellationToken);
        var followers = await db.Follows.LongCountAsync(x => x.AuthorId == authorId, cancellationToken);
        var entries = await (from entry in db.FeedEntries
                             join post in db.Posts on entry.PostId equals post.Id
                             where post.AuthorId == authorId select entry).LongCountAsync(cancellationToken);
        return new(posts, followers, entries);
    }
}
