using System;
using Arcus.EventGrid.Testing.Infrastructure.Hosts;
using Arcus.EventGrid.Testing.Logging;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Testing
{
    public class EventConsumerHostTests
    {
        [Theory]
        [InlineData("00:00:00")]
        [InlineData("-00:01:25")]
        [InlineData("-04:21:48")]
        public void GetReceivedEvent_WithNegativeOrZeroTimeRange_FailsWithArgumentOutOfRangeException(string timeout)
        {
            // Arrange
            var consumer = new EventConsumerHost(new ConsoleLogger());

            // Act / Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => consumer.GetReceivedEvent(eventId: Guid.NewGuid().ToString(), timeout: TimeSpan.Parse(timeout)));
        }
    }
}
