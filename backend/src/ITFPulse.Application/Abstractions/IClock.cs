using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Application.Abstractions
{
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
