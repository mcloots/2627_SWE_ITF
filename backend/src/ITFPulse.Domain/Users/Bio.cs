using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Users
{
    internal class Bio
    {
        public const int MaximumLength = 500;

        public string Value { get; }

        public Bio(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Bio cannot be empty.",
                    nameof(value));
            }

            var trimmedValue = value.Trim();

            if (trimmedValue.Length > MaximumLength)
            {
                throw new ArgumentException(
                    $"Bio cannot exceed {MaximumLength} characters.",
                    nameof(value));
            }

            Value = trimmedValue;
        }
    }
}
