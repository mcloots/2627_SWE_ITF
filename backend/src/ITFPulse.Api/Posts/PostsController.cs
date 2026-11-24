using ITFPulse.Application.Posts.CreatePosts;
using ITFPulse.Application.Posts.GetPosts;
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

        [HttpGet("{postId:guid}")]
        [ProducesResponseType<GetPostResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GetPostResponse>> GetById(
            Guid postId,
            [FromServices] GetPostHandler handler,
            CancellationToken cancellationToken)
        {
            var post = await handler.Handle(postId, cancellationToken);
            if (post is null)
            {
                return NotFound();
            }

            return Ok(new GetPostResponse(
                post.PostId,
                post.AuthorId,
                post.Content,
                post.CreatedAt));
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
