using ITFPulse.Application.Feeds;

namespace ITFPulse.Api.Feeds;

public static class DemoEndpoints
{
    public static void MapFanoutDemo(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;
        app.MapGet("/api/demo/fanout/{authorId:guid}", async (Guid authorId, IFeedStore store, CancellationToken ct) =>
            Results.Ok(await store.GetProgressAsync(authorId, ct)));
    }
}
