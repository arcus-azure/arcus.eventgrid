using System;
using System.Runtime.InteropServices;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Unit.Artifacts;
using CloudNative.CloudEvents;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Events
{
    public class SerializationTests
    {
        [Fact]
        public void Serialize_ValidRawEvent_ShouldSucceed()
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

        [Fact]
        public void Serialize_CloudEventAsEvent_Succeeds()
        {
            // Arrange
            string expected = EventSamples.AzureBlobStorageCreatedCloudEvent;
            EventBatch<Event> batch = EventParser.Parse(expected);
            Event @event = Assert.Single(batch.Events);

            // Act
            string actual = JsonConvert.SerializeObject(new[] { @event });

            // Assert
            Assert.Equal(TrimBlanks(expected), TrimBlanks(actual));
        }

        [Fact]
        public void Serialize_EventGridEventAsEvent_Succeeds()
        {
            // Arrange
            string json = EventSamples.IoTDeviceDeleteEvent.Trim('[', ']').Replace("5023869Z", "5023869");
            var ev = JsonConvert.DeserializeObject<EventGridEvent>(json);
            string expected = JsonConvert.SerializeObject(ev);

            EventBatch<Event> batch = EventParser.Parse(json);
            Event @event = Assert.Single(batch.Events);

            // Act
            string actual = JsonConvert.SerializeObject(@event);

            // Assert
            Assert.Equal(TrimBlanks(expected), TrimBlanks(actual));
        }

        private static string TrimBlanks(string value)
        {
            return value.Replace(" ", string.Empty)
                        .Replace(Environment.NewLine, string.Empty);
        }
    }
}