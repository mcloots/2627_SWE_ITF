namespace ITFPulse.Application.Posts.GetPosts;

public sealed class GetPostHandler(IPostReader postReader)
{
    public Task<PostDetails?> Handle(Guid postId, CancellationToken cancellationToken)
        => postReader.GetByIdAsync(postId, cancellationToken);
}
