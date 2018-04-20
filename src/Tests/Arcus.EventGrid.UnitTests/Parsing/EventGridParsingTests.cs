using Xunit;

namespace Arcus.EventGrid.UnitTests.Parsing
{
    public class EventGridParsingTests
    {
        [Fact]
        public void TestSubscriptionEvent()
        {
            var eventGridMessage = EventGridMessage<dynamic>.Parse(TestArtifacts.SubscriptionValidationEvent);
            Assert.NotNull(eventGridMessage);
            Assert.True(eventGridMessage.Events.Count > 0);
        }

        [Fact]
        public void TestBlobCreateEvent()
        {
            var eventGridMessage = EventGridMessage<dynamic>.Parse(TestArtifacts.BlobCreateEvent);
            Assert.NotNull(eventGridMessage);
            Assert.True(eventGridMessage.Events.Count > 0);
        }
    }
}
