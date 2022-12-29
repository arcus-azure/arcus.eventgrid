using System;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Xunit;

namespace Arcus.EventGrid.Tests.Core
{
    /// <summary>
    /// Custom additional assertions.
    /// </summary>
    public static class ArcusAssert
    {
        public static void ReceivedNewCarRegisteredEvent(CloudEvent expected, string receivedEvent)
        {
            Assert.NotNull(expected);
            CloudEvent actual = CloudEvent.Parse(BinaryData.FromString(receivedEvent));
            Assert.NotNull(actual);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Subject, actual.Subject);
            Assert.Equal(expected.Type, actual.Type);

            Assert.NotNull(expected.Data);
            var expectedData = expected.Data.ToObjectFromJson<CarEventData>();
            Assert.NotNull(actual.Data);
            var actualData = actual.Data.ToObjectFromJson<CarEventData>();
            Assert.Equal(expectedData.LicensePlate, actualData.LicensePlate);
        }

        public static void ReceivedNewCarRegisteredEvent(EventGridEvent expected, string receivedEvent)
        {
            Assert.NotNull(expected);
            EventGridEvent actual = EventGridEvent.Parse(BinaryData.FromString(receivedEvent));
            Assert.NotNull(actual);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Subject, actual.Subject);
            Assert.Equal(expected.EventType, actual.EventType);

            Assert.NotNull(expected.Data);
            var expectedData = expected.Data.ToObjectFromJson<CarEventData>();
            Assert.NotNull(actual.Data);
            var actualData = actual.Data.ToObjectFromJson<CarEventData>();
            Assert.Equal(expectedData.LicensePlate, actualData.LicensePlate);
        }
    }
}
