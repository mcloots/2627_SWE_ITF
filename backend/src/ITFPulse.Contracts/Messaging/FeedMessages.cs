namespace ITFPulse.Contracts.Messaging;

// Public integration contract: no domain entities or transport dependencies.
public sealed record PostCreatedV1(Guid EventId, Guid PostId, Guid AuthorId, DateTimeOffset CreatedAt);

// Commands target one logical owner; multiple instances compete for its queue.
public sealed record PlanPostFanoutV1(Guid PostId, Guid AuthorId, DateTimeOffset CreatedAt, long AfterFollowerId);
public sealed record DeliverPostToFollowersV1(Guid PostId, DateTimeOffset CreatedAt, Guid[] FollowerIds);
