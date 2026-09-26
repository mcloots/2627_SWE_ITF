using ITFPulse.Application.Abstractions.Persistence;
using ITFPulse.Domain.Posts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Application.Tests.TestDoubles
{
    internal sealed class FakePostRepository : IPostRepository
    {
        public Post? AddedPost { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public int AddCallCount { get; private set; }

        public Task AddAsync(
            Post post,
            CancellationToken cancellationToken = default)
        {
            AddedPost = post;
            ReceivedCancellationToken = cancellationToken;
            AddCallCount++;

            return Task.CompletedTask;
        }
    }
}
