using ITFPulse.Domain.Posts;
using ITFPulse.Domain.Followers;

namespace ITFPulse.Domain.Tests.Posts;

public sealed class PostEventTests
{
    [Fact]
    public void CreatingAValidPostRecordsOneFactWithTheSameIdentityAndTime()
    {
        var now = DateTimeOffset.UtcNow;
        var post = Post.Create(Guid.NewGuid(), "Hello followers", now);
        var fact = Assert.IsType<PostCreated>(Assert.Single(post.DomainEvents));
        Assert.Equal(post.Id, fact.PostId);
        Assert.Equal(post.AuthorId, fact.AuthorId);
        Assert.Equal(now, fact.OccurredAt);
        Assert.NotEqual(Guid.Empty, fact.EventId);
        post.ClearDomainEvents();
        Assert.Empty(post.DomainEvents);
    }

    [Fact]
    public void AUserCannotFollowThemselves()
    {
        var user = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => Follow.Create(user, user, DateTimeOffset.UtcNow));
    }
}
