using System.ComponentModel.DataAnnotations;

namespace ITFPulse.Api.Posts
{
    public sealed class CreatePostRequest
    {
        public Guid AuthorId { get; init; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; init; } = string.Empty;
    }
}
