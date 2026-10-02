using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Domain.Common
{
    public class CreatedAt
    {
        public DateTimeOffset Value { get; }

        public CreatedAt(
            DateTimeOffset value)
        {
            var utcValue = value.ToUniversalTime();

            Value = utcValue;
        }
    }
}
