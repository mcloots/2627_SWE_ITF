namespace ITFPulse.Application.Posts.GetPosts;

public sealed record PostDetails(
    Guid PostId,
    Guid AuthorId,
    string Content,
    DateTimeOffset CreatedAt);
