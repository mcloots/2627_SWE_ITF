using ITFPulse.Domain.Common;

namespace ITFPulse.Domain.Posts;

public sealed record PostCreated(Guid EventId, Guid PostId, Guid AuthorId,
    DateTimeOffset OccurredAt) : IDomainEvent;
