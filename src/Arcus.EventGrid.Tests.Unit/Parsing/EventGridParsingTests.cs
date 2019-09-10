using System;
using System.Linq;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Unit.Artifacts;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Parsing
{
    public class EventGridParsingTests
    {
        [Fact]
        public void Parse_ValidSubscriptionValidationEvent_ShouldSucceed()
        {
            // Arrange
            string rawEvent = EventSamples.SubscriptionValidationEvent;
            const string eventId = "2d1781af-3a4c-4d7c-bd0c-e34b19da4e66";
            const string topic = "/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
            const string subject = "Sample.Subject";
            const string eventType = "Microsoft.EventGrid.SubscriptionValidationEvent";
            const string validationCode = "512d38b6-c7b8-40c8-89fe-f46f9e9622b6";
            var eventTime = DateTimeOffset.Parse("2017-08-06T22:09:30.740323Z");

            // Act
            var eventGridBatch = EventGridParser.ParseFromData<SubscriptionValidationEventData>(rawEvent);

            // Assert
            Assert.NotNull(eventGridBatch);
            Assert.NotNull(eventGridBatch.Events);
            var eventPayload = Assert.Single(eventGridBatch.Events);
            Assert.Equal(eventId, eventPayload.Id);
            Assert.Equal(topic, eventPayload.Topic);
            Assert.Equal(subject, eventPayload.Subject);
            Assert.Equal(eventType, eventPayload.EventType);
            Assert.Equal(eventTime, eventPayload.EventTime);
            Assert.NotNull(eventPayload.Data);
            Assert.Equal(validationCode, eventPayload.GetPayload()?.ValidationCode);
        }

        [Fact]
        public void Parse_ValidNewCarRegisteredEvent_ShouldSucceed()
        {
            // Arrange
            string eventId = Guid.NewGuid().ToString();
            const string licensePlate = "1-TOM-337";
            const string subject = licensePlate;
            
            var @event = new NewCarRegistered(eventId, subject, licensePlate);
            var rawEvent = JsonConvert.SerializeObject(new[] {@event});

            // Act
            var eventGridBatch = EventGridParser.Parse<NewCarRegistered>(rawEvent);

            // Assert
            Assert.NotNull(eventGridBatch);
            Assert.NotNull(eventGridBatch.Events);
            var eventPayload = Assert.Single(eventGridBatch.Events);
            Assert.Equal(eventId, eventPayload.Id);
            Assert.Equal(subject, eventPayload.Subject);
            Assert.Equal(@event.EventType, eventPayload.EventType);
            Assert.Equal(@event.EventTime, eventPayload.EventTime);
            Assert.NotNull(eventPayload.Data);
            Assert.Equal(licensePlate, eventPayload.GetPayload()?.LicensePlate);
        }

        [Fact]
        public void Parse_ValidSubscriptionValidationEventWithSessionId_ShouldSucceed()
        {
            // Arrange
            string rawEvent = EventSamples.SubscriptionValidationEvent;
            const string eventId = "2d1781af-3a4c-4d7c-bd0c-e34b19da4e66";
            const string topic = "/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
            const string subject = "Sample.Subject";
            const string eventType = "Microsoft.EventGrid.SubscriptionValidationEvent";
            const string validationCode = "512d38b6-c7b8-40c8-89fe-f46f9e9622b6";
            var eventTime = DateTimeOffset.Parse("2017-08-06T22:09:30.740323Z");
            string sessionId = Guid.NewGuid().ToString();

            // Act
            var eventGridBatch = EventGridParser.ParseFromData<SubscriptionValidationEventData>(rawEvent, sessionId);

            // Assert
            Assert.NotNull(eventGridBatch);
            Assert.Equal(sessionId, eventGridBatch.SessionId);
            Assert.NotNull(eventGridBatch.Events);
            Assert.Single(eventGridBatch.Events);
            var eventPayload = eventGridBatch.Events.Single();
            Assert.Equal(eventId, eventPayload.Id);
            Assert.Equal(topic, eventPayload.Topic);
            Assert.Equal(subject, eventPayload.Subject);
            Assert.Equal(eventType, eventPayload.EventType);
            Assert.Equal(eventTime, eventPayload.EventTime);
            Assert.NotNull(eventPayload.Data);
            Assert.Equal(validationCode, eventPayload.GetPayload()?.ValidationCode);
        }

        [Fact]
        public void Parse_ValidNewCarRegisteredEventWithSessionId_ShouldSucceed()
        {
            // Arrange
            string eventId = Guid.NewGuid().ToString();
            const string licensePlate = "1-TOM-337";
            const string subject = licensePlate;
            
            var @event = new NewCarRegistered(eventId, subject, licensePlate);
            var rawEvent = JsonConvert.SerializeObject(new[] {@event});
            string sessionId = Guid.NewGuid().ToString();

            // Act
            var eventGridBatch = EventGridParser.Parse<NewCarRegistered>(rawEvent, sessionId);

            // Assert
            Assert.NotNull(eventGridBatch);
            Assert.Equal(sessionId, eventGridBatch.SessionId);
            Assert.NotNull(eventGridBatch.Events);
            var eventPayload = Assert.Single(eventGridBatch.Events);
            Assert.Equal(eventId, eventPayload.Id);
            Assert.Equal(subject, eventPayload.Subject);
            Assert.Equal(@event.EventType, eventPayload.EventType);
            Assert.Equal(@event.EventTime, eventPayload.EventTime);
            Assert.NotNull(eventPayload.Data);
            Assert.Equal(licensePlate, eventPayload.GetPayload()?.LicensePlate);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Parse_BlankEventDataSpecified_ThrowsException(string rawEvent)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.ParseFromData<SubscriptionValidationEventData>(rawEvent));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Parse_BlankEventSpecified_ThrowsException(string rawEvent)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse<NewCarRegistered>(rawEvent));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Parse_BlankEventDataWithValidSessionIdSpecified_ThrowsException(string rawEvent)
        {
            // Arrange
            string sessionId = Guid.NewGuid().ToString();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.ParseFromData<SubscriptionValidationEventData>(rawEvent, sessionId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Parse_BlankEventWithValidSessionIdSpecified_ThrowsException(string rawEvent)
        {
            // Arrange
            string sessionId = Guid.NewGuid().ToString();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse<NewCarRegistered>(rawEvent, sessionId));
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Parse_ValidEventDataWithBlankSessionIdSpecified_ThrowsException(string sessionId)
        {
            // Arrange
            string rawEvent = EventSamples.SubscriptionValidationEvent;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.ParseFromData<SubscriptionValidationEventData>(rawEvent, sessionId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Parse_ValidEventWithBlankSessionIdSpecified_ThrowsException(string sessionId)
        {
            // Arrange
            string rawEvent = EventSamples.IoTDeviceCreateEvent;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse<EventGridEvent<IotHubDeviceCreatedEventData>>(rawEvent, sessionId));
        }
    }
}
