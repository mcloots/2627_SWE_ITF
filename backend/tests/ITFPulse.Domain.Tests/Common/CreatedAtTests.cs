using ITFPulse.Domain.Common;
using FluentAssertions;

namespace ITFPulse.Domain.Tests.Common
{
    public class CreatedAtTests
    {
        [Fact]
        public void Constructor_WithValidDate_ShouldStoreDateAsUtc()
        {
            // Arrange
            var date = new DateTimeOffset(
                2026, 9, 1, 10, 0, 0,
                TimeSpan.FromHours(2));

            // Act
            var createdAt = new CreatedAt(date);

            // Assert
            createdAt.Value.Offset.Should().Be(TimeSpan.Zero);
            createdAt.Value.Should().Be(date.ToUniversalTime());
        }
    }
}
