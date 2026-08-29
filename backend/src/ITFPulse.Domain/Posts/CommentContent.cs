using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Posts
{
    internal class CommentContent
    {
        public const int MaximumLength = 500;

        public string Value { get; }

        public CommentContent(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "The content of a comment cannot be empty.",
                    nameof(value));
            }

            var trimmedValue = value.Trim();

            if (trimmedValue.Length > MaximumLength)
            {
                throw new ArgumentException(
                    $"The content of a comment cannot exceed {MaximumLength} characters.",
                    nameof(value));
            }

            Value = trimmedValue;
        }
    }
}
