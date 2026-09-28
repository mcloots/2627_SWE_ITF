namespace ITFPulse.Domain.Followers;

public sealed class Follow
{
    public long Id { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid FollowerId { get; private set; }
    public DateTimeOffset FollowedAt { get; private set; }

    private Follow() { }

    public static Follow Create(Guid authorId, Guid followerId, DateTimeOffset followedAt)
    {
        if (authorId == Guid.Empty || followerId == Guid.Empty)
            throw new ArgumentException("Author and follower are required.");
        if (authorId == followerId)
            throw new ArgumentException("A user cannot follow themselves.");
        return new Follow { AuthorId = authorId, FollowerId = followerId, FollowedAt = followedAt };
    }
}
