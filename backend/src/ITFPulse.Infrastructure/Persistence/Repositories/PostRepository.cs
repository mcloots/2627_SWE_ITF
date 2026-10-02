using ITFPulse.Application.Abstractions.Persistence;
using ITFPulse.Domain.Posts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Infrastructure.Persistence.Repositories
{
    internal sealed class PostRepository : IPostRepository
    {
        private readonly ITFPulseDbContext _dbContext;

        public PostRepository(
            ITFPulseDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            Post post,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.Posts.AddAsync(
                post,
                cancellationToken);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
    }
}

