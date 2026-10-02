using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Users
{
    internal class Email
    {
        public string Value { get; }

        public Email(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty.");

            if (!value.Contains('@'))
                throw new ArgumentException("Email must contain '@'.");

            Value = value.Trim().ToLowerInvariant();
        }
    }
}
