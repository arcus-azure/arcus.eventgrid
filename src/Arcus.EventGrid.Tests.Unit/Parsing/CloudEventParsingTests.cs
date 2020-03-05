using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using Arcus.EventGrid.Tests.Core.Events;
using CloudNative.CloudEvents;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Parsing
{
    public class CloudEventParsingTests
    {
        [Theory]
        [InlineData(CloudEventsSpecVersion.V0_1)]
        [InlineData(CloudEventsSpecVersion.V0_2)]
        [InlineData(CloudEventsSpecVersion.V0_3)]
        [InlineData(CloudEventsSpecVersion.V1_0)]
        public void SerializedCloudEvent_AsJObject_IsCloudEvent(CloudEventsSpecVersion specVersion)
        {
            // Arrange
            var cloudEvent = new CloudEvent(
                specVersion,
                $"event-type-{Guid.NewGuid()}",
                new Uri("http://test-host"),
                id: $"event-id-{Guid.NewGuid()}")
            {
                Data = $"event-data-{Guid.NewGuid()}",
                DataContentType = new ContentType("text/plain")
            };
            var jsonFormatter = new JsonEventFormatter();
            byte[] serialized = jsonFormatter.EncodeStructuredEvent(cloudEvent, out ContentType contentType);
            JObject jObject = JObject.Parse(Encoding.UTF8.GetString(serialized));

            // Act
            bool isCloudEvent = jObject.IsCloudEvent();

            // Assert
            Assert.True(isCloudEvent, "Serialized CloudEvent object should be evaluated as CloudEvent schema");
        }

        [Fact]
        public void SerializedCustomEventGridEvent_AsJObject_IsNotCloudEvent()
        {
            // Arrange
            var newCarRegisteredEvent =
                new NewCarRegistered($"event-id-{Guid.NewGuid()}", $"license-plate-{Guid.NewGuid()}");

            JObject jObject = JObject.FromObject(newCarRegisteredEvent);

            // Act
            bool isCloudEvent = jObject.IsCloudEvent();

            // Assert
            Assert.False(isCloudEvent, "Serialized custom EventGridEvent should not be evaluated as CloudEvent schema");
        }

        [Fact]
        public void SerializedEventGridEvent_AsJObject_IsNotCloudEvent()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent(
                $"event-id-{Guid.NewGuid()}",
                $"subject-{Guid.NewGuid()}",
                $"data-{Guid.NewGuid()}",
                $"event-type-{Guid.NewGuid()}",
                DateTime.UtcNow, 
                $"data-version-{Guid.NewGuid()}");

            string serialized = JsonConvert.SerializeObject(eventGridEvent);
            JObject jObject = JObject.Parse(serialized);

            // Act
            bool isCloudEvent = jObject.IsCloudEvent();

            // Assert
            Assert.False(isCloudEvent, "Serialized EventGridEvent should not be evaluated as CloudEvent schema");
        }
    }
}
