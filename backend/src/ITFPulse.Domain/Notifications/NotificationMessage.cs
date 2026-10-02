using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Notifications
{
    internal class NotificationMessage
    {
        public const int MaximumLength = 500;

        public string Value { get; }

        public NotificationMessage(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "The notification message cannot be empty.",
                    nameof(value));
            }

            var trimmedValue = value.Trim();

            if (trimmedValue.Length > MaximumLength)
            {
                throw new ArgumentException(
                    $"The notification message cannot exceed {MaximumLength} characters.",
                    nameof(value));
            }

            Value = trimmedValue;
        }
    }
}
