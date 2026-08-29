using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Users
{
    internal class UserName
    {
        public const int MinimumLength = 3;
        public const int MaximumLength = 30;

        public string Value { get; }

        public UserName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Username cannot be empty.",
                    nameof(value));
            }

            var normalizedValue = value.Trim().ToLowerInvariant();

            if (normalizedValue.Length < MinimumLength ||
                normalizedValue.Length > MaximumLength)
            {
                throw new ArgumentException(
                    $"Username must contain between {MinimumLength} and {MaximumLength} characters.",
                    nameof(value));
            }

            if (!normalizedValue.All(IsAllowedCharacter))
            {
                throw new ArgumentException(
                    "Username can only contain letters, numbers, dots, underscores and hyphens.",
                    nameof(value));
            }

            Value = normalizedValue;
        }

        private static bool IsAllowedCharacter(char character)
        {
            return char.IsLetterOrDigit(character) ||
                   character is '.' or '_' or '-';
        }
    }
}
