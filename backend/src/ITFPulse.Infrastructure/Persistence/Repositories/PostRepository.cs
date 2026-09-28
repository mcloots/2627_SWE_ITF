using ITFPulse.Application.Abstractions.Persistence;
using ITFPulse.Domain.Posts;
using ITFPulse.Contracts.Messaging;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Infrastructure.Persistence.Repositories
{
    internal sealed class PostRepository : IPostRepository
    {
        private readonly ITFPulseDbContext _dbContext;
        private readonly IPublishEndpoint _publisher;

        public PostRepository(
            ITFPulseDbContext dbContext, IPublishEndpoint publisher)
        {
            _dbContext = dbContext;
            _publisher = publisher;
        }

        public async Task AddAsync(
            Post post,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.Posts.AddAsync(
                post,
                cancellationToken);

            // Scoped IPublishEndpoint is replaced by the EF bus outbox. No broker call here.
            foreach (var domainEvent in post.DomainEvents)
            {
                if (domainEvent is not PostCreated created)
                    throw new InvalidOperationException($"Unmapped event: {domainEvent.GetType().Name}");
                await _publisher.Publish(new PostCreatedV1(created.EventId, created.PostId,
                    created.AuthorId, created.OccurredAt), context => context.MessageId = created.EventId,
                    cancellationToken);
            }
            await _dbContext.SaveChangesAsync(
                cancellationToken);
            post.ClearDomainEvents();
        }
    }
}

