using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Users
{
    internal class DisplayName
    {
        public const int MaximumLength = 100;

        public string Value { get; }

        public DisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Display name cannot be empty.",
                    nameof(value));
            }

            var trimmedValue = value.Trim();

            if (trimmedValue.Length > MaximumLength)
            {
                throw new ArgumentException(
                    $"Display name cannot exceed {MaximumLength} characters.",
                    nameof(value));
            }

            Value = trimmedValue;
        }
    }
}
