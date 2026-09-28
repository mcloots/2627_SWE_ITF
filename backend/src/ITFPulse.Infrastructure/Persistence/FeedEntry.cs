namespace ITFPulse.Infrastructure.Persistence;

// Read model, not an aggregate. A reference to the source post, not a content copy.
public sealed class FeedEntry
{
    public long Sequence { get; set; }
    public Guid FollowerId { get; set; }
    public Guid PostId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
