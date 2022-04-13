using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
#pragma warning disable CS0618 // Ignore deprecated types as we are testing them.

namespace Arcus.EventGrid.Tests.Unit.Parsing
{
    public class RawEventParsingTests
    {
        [Fact]
        public void ParseToNewCarRegistered_ValidRawEvent_ShouldSucceed()
        {
            // Arrange
            const string eventId = "2d1781af-3a4c-4d7c-bd0c-e34b19da4e66";
            const string licensePlate = "1-TOM-337";
            var originalEvent = new NewCarRegistered(eventId, licensePlate);
            var rawEventBody = JsonConvert.SerializeObject(originalEvent.GetPayload());
            var rawEvent = new RawEvent(eventId, originalEvent.EventType, rawEventBody, originalEvent.Subject, originalEvent.DataVersion, originalEvent.EventTime);
            IEnumerable<RawEvent> events = new List<RawEvent>
            {
                rawEvent
            };

            var serializedRawEvents = JsonConvert.SerializeObject(events);

            // Act
            var eventGridMessage = EventGridParser.Parse<NewCarRegistered>(serializedRawEvents);

            // Assert
            Assert.NotNull(eventGridMessage);
            Assert.NotNull(eventGridMessage.Events);
            Assert.Single(eventGridMessage.Events);
            var eventPayload = eventGridMessage.Events.Single();
            Assert.Equal(eventId, eventPayload.Id);
            Assert.Equal(originalEvent.Subject, eventPayload.Subject);
            Assert.Equal(originalEvent.EventType, eventPayload.EventType);
            Assert.Equal(originalEvent.EventTime, eventPayload.EventTime);
            Assert.Equal(originalEvent.DataVersion, eventPayload.DataVersion);
            Assert.NotNull(eventPayload.Data);

            CarEventData expectedEventData = originalEvent.GetPayload();
            CarEventData actualEventData = eventPayload.GetPayload();
            
            Assert.NotNull(expectedEventData);
            Assert.NotNull(actualEventData);
            Assert.Equal(expectedEventData, actualEventData);
        }

        [Fact]
        public void ParseToRaw_ValidRawEvent_ShouldSucceed()
        {
            // Arrange
            const string eventId = "2d1781af-3a4c-4d7c-bd0c-e34b19da4e66";
            const string licensePlate = "1-TOM-337";
            var originalEvent = new NewCarRegistered(eventId, licensePlate);
            var rawEventBody = JsonConvert.SerializeObject(originalEvent.Data, Formatting.Indented);
            var rawEvent = new RawEvent(eventId, originalEvent.EventType, rawEventBody, originalEvent.Subject, originalEvent.DataVersion, originalEvent.EventTime);
            IEnumerable<RawEvent> events = new List<RawEvent>
            {
                rawEvent
            };

            var serializedRawEvents = JsonConvert.SerializeObject(events);

            // Act
            var eventGridMessage = EventGridParser.Parse<RawEvent>(serializedRawEvents);

            // Assert
            Assert.NotNull(eventGridMessage);
            Assert.NotNull(eventGridMessage.Events);
            Assert.Single(eventGridMessage.Events);
            var eventPayload = eventGridMessage.Events.Single();
            Assert.Equal(eventId, eventPayload.Id);
            Assert.Equal(originalEvent.Subject, eventPayload.Subject);
            Assert.Equal(originalEvent.EventType, eventPayload.EventType);
            Assert.Equal(originalEvent.EventTime, eventPayload.EventTime);
            Assert.Equal(originalEvent.DataVersion, eventPayload.DataVersion);
            Assert.NotNull(eventPayload.Data);
            Assert.Equal(rawEventBody, eventPayload.Data.ToString());
        }
    }
}
