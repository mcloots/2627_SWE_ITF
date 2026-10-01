namespace ITFPulse.Contracts.Messaging;

// Public integration contract: no domain entities or transport dependencies.
// V1 identifies the wire contract version. EventId is also used as the transport MessageId.
public sealed record PostCreatedV1(Guid EventId, Guid PostId, Guid AuthorId, DateTimeOffset CreatedAt);

// Commands target one logical owner; multiple instances compete for its queue.
// AfterFollowerId is the numeric follow relationship ID, not the follower's user GUID.
public sealed record PlanPostFanoutV1(Guid PostId, Guid AuthorId, DateTimeOffset CreatedAt, long AfterFollowerId);
// Carry one bounded batch of recipients and a post reference, never the entire post aggregate.
public sealed record DeliverPostToFollowersV1(Guid PostId, DateTimeOffset CreatedAt, Guid[] FollowerIds);
