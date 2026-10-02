using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Posts
{
    public class PostContent
    {
        public const int MaximumLength = 2000;

        public string Value { get; }

        public PostContent(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "The content of a post cannot be empty.",
                    nameof(value));
            }

            var trimmedValue = value.Trim();

            if (trimmedValue.Length > MaximumLength)
            {
                throw new ArgumentException(
                    $"The content of a post cannot exceed {MaximumLength} characters.",
                    nameof(value));
            }

            Value = trimmedValue;
        }
    }
}
