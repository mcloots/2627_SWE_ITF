using ITFPulse.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Infrastructure.Time
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
