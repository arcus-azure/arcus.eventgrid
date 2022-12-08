using System;
using System.Text;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        /// <param name="eventId">The expected unique event identifier of the event.</param>
        /// <param name="eventType">The expected event type of the event.</param>
        /// <param name="eventSubject">The expected subject of the event.</param>
        /// <param name="licensePlate">The expected license plate number of the event's payload.</param>
        /// <param name="receivedEvent">The actual raw event to be asserted.</param>
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

            ReceivedNewCarRegisteredPayload(licensePlate, deserializedEvent);
        }

        /// <summary>
        /// Asserts the <see cref="CarEventData"/> event payload model of an <see cref="NewCarRegistered"/> event.
        /// </summary>
        /// <param name="licensePlate">The expected license plate number of the event's payload.</param>
        /// <param name="deserializedEvent">The actual deserialized event to be asserted.</param>
        public static void ReceivedNewCarRegisteredPayload(string licensePlate, Event deserializedEvent)
        {
            Assert.NotNull(deserializedEvent.Data);
            var eventData = deserializedEvent.GetPayload<CarEventData>();
            Assert.NotNull(eventData);
            Assert.Equal(JsonConvert.DeserializeObject<CarEventData>(deserializedEvent.Data.ToString()), eventData);
            Assert.Equal(licensePlate, eventData.LicensePlate);
        }

        
        /// <summary>
        /// Asserts the <see cref="CarEventData"/> event payload model of an <see cref="NewCarRegistered"/> event.
        /// </summary>
        /// <param name="licensePlate">The expected license plate number of the event's payload.</param>
        /// <param name="deserializedEvent">The actual deserialized event to be asserted.</param>
        public static void ReceivedNewCarRegisteredPayload(string licensePlate, CloudEvent deserializedEvent)
        {
            Assert.NotNull(deserializedEvent.Data);
            var eventData = deserializedEvent.Data.ToObjectFromJson<CarEventData>();
            Assert.NotNull(eventData);
            Assert.Equal(licensePlate, eventData.LicensePlate);
        }

        /// <summary>
        /// Asserts the <see cref="CarEventData"/> event payload model of an <see cref="NewCarRegistered"/> event.
        /// </summary>
        /// <param name="licensePlate">The expected license plate number of the event's payload.</param>
        /// <param name="deserializedEvent">The actual deserialized event to be asserted.</param>
        public static void ReceivedNewCarRegisteredPayload(string licensePlate, EventGridEvent deserializedEvent)
        {
            Assert.NotNull(deserializedEvent.Data);
            var eventData = deserializedEvent.Data.ToObjectFromJson<CarEventData>();
            Assert.NotNull(eventData);
            Assert.Equal(JsonConvert.DeserializeObject<CarEventData>(deserializedEvent.Data.ToString()), eventData);
            Assert.Equal(licensePlate, eventData.LicensePlate);
        }
    }
}
