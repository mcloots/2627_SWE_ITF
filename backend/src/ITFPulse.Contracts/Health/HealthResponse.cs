using System;
using System.Collections.Generic;
using System.Text;

namespace ITFPulse.Contracts.Health
{
    // API Responses belong in Contracts
    public class HealthResponse
    {
        public string Status { get; init; } = default!;
        public DateTime Timestamp { get; init; }
        public string Service { get; init; } = default!;
    }
}
