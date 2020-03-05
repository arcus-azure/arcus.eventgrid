using System;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.EventGrid.Tests.Core
{
    /// <summary>
    /// Custom additional assertions.
    /// </summary>
    public static class ArcusAssert
    {
        /// <summary>
        /// Asserts the <see cref="NewCarRegistered"/> event.
        /// </summary>
        public static void ReceivedNewCarRegisteredEvent(string eventId, string eventType, string eventSubject, string licensePlate, string receivedEvent)
        {
            Assert.NotEqual(String.Empty, receivedEvent);

            EventBatch<Event> deserializedEventGridMessage = EventParser.Parse(receivedEvent);
            Assert.NotNull(deserializedEventGridMessage);
            Assert.NotEmpty(deserializedEventGridMessage.SessionId);
            Assert.NotNull(deserializedEventGridMessage.Events);

            Event deserializedEvent = Assert.Single(deserializedEventGridMessage.Events);
            Assert.NotNull(deserializedEvent);
            Assert.Equal(eventId, deserializedEvent.Id);
            Assert.Equal(eventSubject, deserializedEvent.Subject);
            Assert.Equal(eventType, deserializedEvent.EventType);

            Assert.NotNull(deserializedEvent.Data);
            var eventData = deserializedEvent.GetPayload<CarEventData>();
            Assert.NotNull(eventData);
            Assert.Equal(JsonConvert.DeserializeObject<CarEventData>(deserializedEvent.Data.ToString()), eventData);
            Assert.Equal(licensePlate, eventData.LicensePlate);
        }
    }
}
