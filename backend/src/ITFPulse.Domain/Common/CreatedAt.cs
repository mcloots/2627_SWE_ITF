using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Common
{
    internal class CreatedAt
    {
        public DateTimeOffset Value { get; }

        public CreatedAt(
            DateTimeOffset value,
            DateTimeOffset currentTime)
        {
            var utcValue = value.ToUniversalTime();
            var utcCurrentTime = currentTime.ToUniversalTime();

            if (utcValue > utcCurrentTime)
            {
                throw new ArgumentException(
                    "Creation time cannot be in the future.",
                    nameof(value));
            }

            Value = utcValue;
        }
    }
}
