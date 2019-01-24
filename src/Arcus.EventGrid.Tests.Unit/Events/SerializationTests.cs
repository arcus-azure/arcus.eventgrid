using System;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Tests.Core.Events;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Events
{
    public class SerializationTests
    {
        [Fact]
        public void Parse_ValidRawEvent_ShouldSucceed()
        {
            // Arrange
            const string eventId = "2d1781af-3a4c-4d7c-bd0c-e34b19da4e66";
            const string licensePlate = "1-TOM-337";
            var originalEvent = new NewCarRegistered(eventId, licensePlate);
            var rawEventBody = JsonConvert.SerializeObject(originalEvent.Data);
            var rawEvent = new RawEvent(eventId, originalEvent.EventType, rawEventBody, originalEvent.Subject, originalEvent.DataVersion, originalEvent.EventTime);
            
            // Act
            var serializedOriginalEvent = JsonConvert.SerializeObject(originalEvent);
            var serializedRawEvent = JsonConvert.SerializeObject(rawEvent);

            // Assert
            Assert.Equal(serializedRawEvent, serializedOriginalEvent);
        }
    }
}