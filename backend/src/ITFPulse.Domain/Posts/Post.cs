using ITFPulse.Domain.Common;
using ITFPulse.Domain.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Posts
{
    public class Post
    {
        private readonly List<IDomainEvent> _domainEvents = [];
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();

        public Guid Id { get; private set; }

        public PostContent PostContent { get; private set; }

        public CreatedAt CreatedAt { get; private set; }

        // User creates a post, but in domain language we talk about the Author of the post
        public Guid AuthorId { get; private set; }

        private readonly List<Comment> _comments = [];

        public IReadOnlyCollection<Comment> Comments =>
            _comments.AsReadOnly();

        // private constructor needed for EF Core
        private Post()
        {
            // Used only by EF Core
            PostContent = null!;
            CreatedAt = null!;
        }

        // A post can only be created through the Create method => Factory method
        private Post(
            Guid id,
            Guid authorId,
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
            AuthorId = authorId;
            PostContent = postContent
                ?? throw new ArgumentNullException(nameof(postContent));

            CreatedAt = createdAt
                ?? throw new ArgumentNullException(nameof(createdAt));
        }

        // Factory method for creating a post
        // This reads like domain language: we are creating a new post
        public static Post Create(
        Guid authorId,
        string content,
        DateTimeOffset createdAt)
        {
            if (authorId == Guid.Empty)
                throw new ArgumentException("An author is required.");

            var post = new Post(
                Guid.NewGuid(),
                authorId,
                new PostContent(content),
                new CreatedAt(createdAt));
            // Record an in-memory business fact only; persistence maps it to a durable integration event.
            post._domainEvents.Add(new PostCreated(Guid.NewGuid(), post.Id, authorId, createdAt));
            return post;
        }

        public void Edit(string newContent)
        {
            PostContent = new PostContent(newContent);
        }

        public void AddComment(
            Guid commentId,
            string content,
            DateTimeOffset createdAt)
        {
            if (_comments.Any(comment => comment.Id == commentId))
            {
                throw new InvalidOperationException(
                    $"Comment with ID {commentId} already exists.");
            }

            var comment = new Comment(
                commentId,
                new CommentContent(content),
                new CreatedAt(createdAt));

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
