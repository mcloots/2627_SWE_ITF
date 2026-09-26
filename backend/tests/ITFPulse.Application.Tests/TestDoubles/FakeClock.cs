
namespace ITFPulse.Application.Tests.TestDoubles
{
    internal sealed class FakeClock : Abstractions.IClock
    {
        public FakeClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow.ToUniversalTime();
        }

        public DateTimeOffset UtcNow { get; }
    }
}
