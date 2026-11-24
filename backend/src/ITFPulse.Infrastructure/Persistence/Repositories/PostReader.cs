using ITFPulse.Application.Posts.GetPosts;
using Microsoft.EntityFrameworkCore;

namespace ITFPulse.Infrastructure.Persistence.Repositories;

internal sealed class PostReader(ITFPulseDbContext dbContext) : IPostReader
{
    public Task<PostDetails?> GetByIdAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
        => dbContext.Posts
            .AsNoTracking()
            .Where(post => post.Id == postId)
            .Select(post => new PostDetails(
                post.Id,
                post.AuthorId,
                post.PostContent.Value,
                post.CreatedAt.Value))
            .SingleOrDefaultAsync(cancellationToken);
}
