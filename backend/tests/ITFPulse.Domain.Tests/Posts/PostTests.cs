using FluentAssertions;
using ITFPulse.Domain.Posts;

namespace ITFPulse.Domain.Tests.Posts
{
    public class PostTests
    {
        [Fact]
        public void Create_WithValidInput_ShouldCreatePost()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            const string content = "My first post";
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);

            // Act
            var post = Post.Create(
                authorId,
                content,
                createdAt);

            // Assert
            post.Id.Should().NotBe(Guid.Empty);
            post.AuthorId.Should().Be(authorId);
            post.PostContent.Value.Should().Be(content);
            post.CreatedAt.Value.Should().Be(createdAt.ToUniversalTime());
            post.Comments.Should().BeEmpty();
        }

        [Fact]
        public void Create_WithEmptyAuthorId_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => Post.Create(
                Guid.Empty,
                "My first post",
                DateTimeOffset.UtcNow.AddMinutes(-1));

            // Assert
            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("*author is required*");
        }

        [Fact]
        public void Create_WithEmptyContent_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => Post.Create(
                Guid.NewGuid(),
                string.Empty,
                DateTimeOffset.UtcNow.AddMinutes(-1));

            // Assert
            act.Should()
                .Throw<ArgumentException>();
        }
    }
}
