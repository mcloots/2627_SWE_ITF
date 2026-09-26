using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Application.Posts.CreatePosts
{
    public sealed record CreatePostCommand(
    Guid AuthorId,
    string Content);
}
