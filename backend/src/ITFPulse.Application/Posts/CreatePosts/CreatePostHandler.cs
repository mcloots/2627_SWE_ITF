using ITFPulse.Application.Abstractions;
using ITFPulse.Application.Abstractions.Persistence;
using ITFPulse.Domain.Posts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Application.Posts.CreatePosts
{
    public sealed class CreatePostHandler
    {
        private readonly IPostRepository _postRepository;
        private readonly IClock _clock;

        public CreatePostHandler(
            IPostRepository postRepository,
            IClock clock)
        {
            _postRepository = postRepository;
            _clock = clock;
        }

        public async Task<Guid> Handle(
            CreatePostCommand command,
            CancellationToken cancellationToken)
        {
            var post = Post.Create(
                command.AuthorId,
                command.Content,
                _clock.UtcNow);

            await _postRepository.AddAsync(
                post,
                cancellationToken);

            return post.Id;
        }
    }
}
