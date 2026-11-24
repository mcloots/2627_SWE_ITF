namespace ITFPulse.Contracts.Posts;

public sealed record GetPostResponse(
    Guid PostId,
    Guid AuthorId,
    string Content,
    DateTimeOffset CreatedAt);
