using ITFPulse.Domain.Common;
using ITFPulse.Domain.Users;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace ITFPulse.Domain.Posts
{
    internal class Comment
    {
        public Guid Id { get; private set; }
        public CommentContent CommentContent { get; private set; }
        public CreatedAt CreatedAt { get; private set; }

        internal Comment(
        Guid id,
        CommentContent commentContent,
        CreatedAt createdAt)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "Comment ID cannot be empty.",
                    nameof(id));
            }

            Id = id;
            CommentContent = commentContent
                ?? throw new ArgumentNullException(nameof(commentContent));

            CreatedAt = createdAt
                ?? throw new ArgumentNullException(nameof(createdAt));
        }

        internal void Edit(string newContent)
        {
            CommentContent = new CommentContent(newContent);
        }
    }
}
