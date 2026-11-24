namespace ITFPulse.Application.Posts.GetPosts;

public interface IPostReader
{
    Task<PostDetails?> GetByIdAsync(
        Guid postId,
        CancellationToken cancellationToken = default);
}
