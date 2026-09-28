using ITFPulse.Application.Posts.CreatePosts;
using ITFPulse.Contracts.Posts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ITFPulse.Api.Posts
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class PostsController : ControllerBase
    {
        private readonly CreatePostHandler _handler;

        public PostsController(CreatePostHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        [ProducesResponseType<CreatePostResponse>(
            StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(
            StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreatePostResponse>> Create(
            CreatePostRequest request,
            CancellationToken cancellationToken)
        {
            if (request.AuthorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["post"] = ["A non-empty author ID and post content are required."]
                }));
            var command = new CreatePostCommand(
                request.AuthorId,
                request.Content);

            var postId = await _handler.Handle(
                command,
                cancellationToken);

            var response = new CreatePostResponse(postId);

            return Created(
                $"/api/posts/{postId}",
                response);
        }
    }
}
