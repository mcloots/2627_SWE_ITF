using FluentAssertions;
using ITFPulse.Application.Posts.CreatePosts;
using ITFPulse.Application.Tests.TestDoubles;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Application.Tests.Posts.CreatePost
{
    public sealed class CreatePostHandlerTests
    {
        [Fact]
        public async Task Handle_WithValidCommand_ShouldCreateAndStorePost()
        {
            // Arrange
            var authorId = Guid.NewGuid();

            var currentTime = new DateTimeOffset(
                2026, 9, 2,
                10, 0, 0,
                TimeSpan.Zero);

            var repository = new FakePostRepository();
            var clock = new FakeClock(currentTime);

            var handler = new CreatePostHandler(
                repository,
                clock);

            var command = new CreatePostCommand(
                authorId,
                "My first post");

            // Act
            var postId = await handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            repository.AddCallCount.Should().Be(1);
            repository.AddedPost.Should().NotBeNull();

            repository.AddedPost!.Id.Should().Be(postId);
            repository.AddedPost.AuthorId.Should().Be(authorId);

            repository.AddedPost.PostContent.Value
                .Should().Be("My first post");

            repository.AddedPost.CreatedAt.Value
                .Should().Be(currentTime);

            repository.AddedPost.Comments
                .Should().BeEmpty();
        }
    }
}
