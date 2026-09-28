using ITFPulse.Application.Feeds;
using ITFPulse.Contracts.Messaging;
using MassTransit;

namespace ITFPulse.Infrastructure.Messaging;

public sealed class PostCreatedConsumer(IFeedWorkQueue queue) : IConsumer<PostCreatedV1>
{
    public Task Consume(ConsumeContext<PostCreatedV1> context) => queue.PlanAsync(
        new(context.Message.PostId, context.Message.AuthorId, context.Message.CreatedAt, 0), context.CancellationToken);
}

public sealed class PlanPostFanoutConsumer(PlanPostFanoutHandler handler) : IConsumer<PlanPostFanoutV1>
{
    public Task Consume(ConsumeContext<PlanPostFanoutV1> context) => handler.Handle(context.Message, context.CancellationToken);
}

public sealed class DeliverPostToFollowersConsumer(DeliverPostToFollowersHandler handler) : IConsumer<DeliverPostToFollowersV1>
{
    public Task Consume(ConsumeContext<DeliverPostToFollowersV1> context) => handler.Handle(context.Message, context.CancellationToken);
}
