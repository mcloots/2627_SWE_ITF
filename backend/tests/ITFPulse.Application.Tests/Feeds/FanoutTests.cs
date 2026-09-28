using ITFPulse.Application.Feeds;
using ITFPulse.Contracts.Messaging;
using ITFPulse.Domain.Followers;

namespace ITFPulse.Application.Tests.Feeds;

public sealed class FanoutTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 0)]
    [InlineData(499, 1, 0)]
    [InlineData(500, 1, 1)]
    public async Task PlannerBoundsWorkAndContinuesOnlyFullPages(int count, int deliveries, int continuations)
    {
        var store = new Store { Followers = Enumerable.Range(11, count).Select(i => new FollowerPageItem(i, Guid.NewGuid())).ToArray() };
        var queue = new Queue();
        var command = new PlanPostFanoutV1(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, 10);
        await new PlanPostFanoutHandler(store, queue).Handle(command, CancellationToken.None);
        Assert.Equal((command.AuthorId, command.CreatedAt, 10L, 500), store.Query);
        Assert.Equal(deliveries, queue.Deliveries.Count);
        Assert.Equal(continuations, queue.Plans.Count);
        if (count > 0) Assert.Equal(store.Followers.Select(f => f.FollowerId), queue.Deliveries[0].FollowerIds);
        if (continuations > 0) Assert.Equal(command with { AfterFollowerId = 510 }, queue.Plans[0]);
    }

    [Fact]
    public async Task DeliveryFailureMustPropagateSoTheMessageIsNotAcknowledged()
    {
        var store = new Store { FailDelivery = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new DeliverPostToFollowersHandler(store)
            .Handle(new(Guid.NewGuid(), DateTimeOffset.UtcNow, [Guid.NewGuid()]), CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task DeliveryRejectsUnboundedOrEmptyBatches(int count)
    {
        var store = new Store();
        await Assert.ThrowsAsync<ArgumentException>(() => new DeliverPostToFollowersHandler(store)
            .Handle(new(Guid.NewGuid(), DateTimeOffset.UtcNow,
                Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray()), CancellationToken.None));
    }

    private sealed class Queue : IFeedWorkQueue
    {
        public List<PlanPostFanoutV1> Plans { get; } = [];
        public List<DeliverPostToFollowersV1> Deliveries { get; } = [];
        public Task PlanAsync(PlanPostFanoutV1 command, CancellationToken ct) { Plans.Add(command); return Task.CompletedTask; }
        public Task DeliverAsync(DeliverPostToFollowersV1 command, CancellationToken ct) { Deliveries.Add(command); return Task.CompletedTask; }
    }

    private sealed class Store : IFeedStore
    {
        public IReadOnlyList<FollowerPageItem> Followers { get; init; } = [];
        public (Guid, DateTimeOffset, long, int) Query { get; private set; }
        public bool FailDelivery { get; init; }
        public Task<IReadOnlyList<FollowerPageItem>> GetFollowersAsync(Guid author, DateTimeOffset cutoff, long after, int limit, CancellationToken ct)
        { Query = (author, cutoff, after, limit); return Task.FromResult(Followers); }
        public Task DeliverAsync(Guid post, DateTimeOffset created, Guid[] followers, CancellationToken ct) =>
            FailDelivery ? Task.FromException(new InvalidOperationException("Database unavailable")) : Task.CompletedTask;
        public Task FollowAsync(Follow follow, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<FeedPost>> ReadAsync(Guid follower, long? before, int limit, CancellationToken ct) => throw new NotImplementedException();
        public Task<FanoutProgress> GetProgressAsync(Guid author, CancellationToken ct) => throw new NotImplementedException();
    }
}
