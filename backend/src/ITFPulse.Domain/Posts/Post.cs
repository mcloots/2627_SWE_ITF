using ITFPulse.Domain.Common;
using ITFPulse.Domain.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Posts
{
    internal class Post
    {
        private readonly List<Comment> _comments = [];

        public Guid Id { get; private set; }

        public PostContent PostContent { get; private set; }

        public CreatedAt CreatedAt { get; private set; }

        public IReadOnlyCollection<Comment> Comments =>
            _comments.AsReadOnly();

        private Post()
        {
            // Required by EF Core
            PostContent = null!;
            CreatedAt = null!;
        }

        public Post(
            Guid id,
            PostContent postContent,
            CreatedAt createdAt)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "Post ID cannot be empty.",
                    nameof(id));
            }

            Id = id;
            PostContent = postContent
                ?? throw new ArgumentNullException(nameof(postContent));

            CreatedAt = createdAt
                ?? throw new ArgumentNullException(nameof(createdAt));
        }

        public void Edit(string newContent)
        {
            PostContent = new PostContent(newContent);
        }

        public void AddComment(
            Guid commentId,
            string content,
            DateTimeOffset createdAt,
            DateTimeOffset currentTime)
        {
            if (_comments.Any(comment => comment.Id == commentId))
            {
                throw new InvalidOperationException(
                    $"Comment with ID {commentId} already exists.");
            }

            var comment = new Comment(
                commentId,
                new CommentContent(content),
                new CreatedAt(createdAt, currentTime));

            _comments.Add(comment);
        }

        public void EditComment(Guid commentId, string newContent)
        {
            var comment = FindComment(commentId);
            comment.Edit(newContent);
        }

        public void RemoveComment(Guid commentId)
        {
            var comment = FindComment(commentId);
            _comments.Remove(comment);
        }

        private Comment FindComment(Guid commentId)
        {
            return _comments.SingleOrDefault(comment => comment.Id == commentId)
                ?? throw new KeyNotFoundException(
                    $"Comment with ID {commentId} was not found.");
        }
    }
}
