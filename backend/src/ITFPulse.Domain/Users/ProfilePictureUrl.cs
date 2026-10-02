using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Users
{
    internal class ProfilePictureUrl
    {
        public string Value { get; }

        public ProfilePictureUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Profile picture URL cannot be empty.",
                    nameof(value));
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException(
                    "Profile picture URL must be a valid absolute URL.",
                    nameof(value));
            }

            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException(
                    "Profile picture URL must use HTTPS.",
                    nameof(value));
            }

            Value = uri.AbsoluteUri;
        }
    }
}
