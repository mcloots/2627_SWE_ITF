using ITFPulse.Application.Abstractions;
using ITFPulse.Application.Feeds;
using Microsoft.AspNetCore.Mvc;

namespace ITFPulse.Api.Feeds;

[ApiController]
[Route("api")]
public sealed class FeedsController(IFeedStore store, IClock clock) : ControllerBase
{
    // As with CreatePost, IDs are demo identities until authentication is introduced.
    [HttpPut("authors/{authorId:guid}/followers/{followerId:guid}")]
    public async Task<IActionResult> Follow(Guid authorId, Guid followerId, CancellationToken cancellationToken)
    {
        if (authorId == Guid.Empty || followerId == Guid.Empty || authorId == followerId)
            return BadRequest(new { error = "Different, non-empty author and follower IDs are required." });
        await store.FollowAsync(Domain.Followers.Follow.Create(authorId, followerId, clock.UtcNow), cancellationToken);
        return NoContent();
    }

    [HttpGet("feeds/{followerId:guid}")]
    public async Task<IActionResult> Read(Guid followerId, CancellationToken cancellationToken,
        [FromQuery] long? before = null, [FromQuery] int limit = 20)
    {
        if (followerId == Guid.Empty || before is <= 0 || limit is < 1 or > 100)
            return BadRequest(new { error = "Use a follower ID, positive cursor and limit 1–100." });
        var items = await store.ReadAsync(followerId, before, limit, cancellationToken);
        return Ok(new { items, nextCursor = items.Count == limit ? (long?)items[^1].Sequence : null });
    }
}
