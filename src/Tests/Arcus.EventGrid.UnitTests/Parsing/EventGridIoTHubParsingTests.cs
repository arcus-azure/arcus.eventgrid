using Arcus.EventGrid.IoTHub;
using Xunit;

namespace Arcus.EventGrid.UnitTests.Parsing
{
    public class EventGridIoTHubParsingTests
    {
        [Fact]
        public void TestDeviceCreateEvent()
        {
            var eventGridMessage = EventGridMessage<IoTDeviceEventData>.Parse(TestArtifacts.IoTDeviceCreateEvent);
            Assert.NotNull(eventGridMessage);
            Assert.True(eventGridMessage.Events.Count > 0);
            Assert.Equal("grid-test-01", eventGridMessage.Events[0].Data.DeviceId);
            Assert.NotNull(eventGridMessage.Events[0].Data.Twin);
            Assert.True(eventGridMessage.Events[0].Data.Twin.Properties.Desired.Metadata.Count > 0);
        }
    }
}