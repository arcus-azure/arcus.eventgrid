using System;
using System.Linq;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Security.Contracts.Events.v1;
using Arcus.EventGrid.Tests.Unit.Artifacts;
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
            var eventGridMessage = EventGridParser.Parse<SubscriptionValidation>(rawEvent);

            // Assert
            Assert.NotNull(eventGridMessage);
            Assert.NotNull(eventGridMessage.Events);
            Assert.Single(eventGridMessage.Events);
            var eventPayload = eventGridMessage.Events.Single();
            Assert.Equal(eventId, eventPayload.Id);
            Assert.Equal(topic, eventPayload.Topic);
            Assert.Equal(subject, eventPayload.Subject);
            Assert.Equal(eventType, eventPayload.EventType);
            Assert.Equal(eventTime, eventPayload.EventTime);
            Assert.NotNull(eventPayload.Data);
            Assert.Equal(validationCode, eventPayload.Data.ValidationCode);
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
            var eventGridMessage = EventGridParser.Parse<SubscriptionValidation>(rawEvent, sessionId);

            // Assert
            Assert.NotNull(eventGridMessage);
            Assert.Equal(sessionId, eventGridMessage.SessionId);
            Assert.NotNull(eventGridMessage.Events);
            Assert.Single(eventGridMessage.Events);
            var eventPayload = eventGridMessage.Events.Single();
            Assert.Equal(eventId, eventPayload.Id);
            Assert.Equal(topic, eventPayload.Topic);
            Assert.Equal(subject, eventPayload.Subject);
            Assert.Equal(eventType, eventPayload.EventType);
            Assert.Equal(eventTime, eventPayload.EventTime);
            Assert.NotNull(eventPayload.Data);
            Assert.Equal(validationCode, eventPayload.Data.ValidationCode);
        }

        [Fact]
        public void Parse_EmptyEventSpecified_ThrowsException()
        {
            // Arrange
            string rawEvent = string.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse<SubscriptionValidation>(rawEvent));
        }

        [Fact]
        public void Parse_NoEventSpecified_ThrowsException()
        {
            // Arrange
            string rawEvent = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse<SubscriptionValidation>(rawEvent));
        }

        [Fact]
        public void Parse_EmptyEventWithValidSessionIdSpecified_ThrowsException()
        {
            // Arrange
            string rawEvent = string.Empty;
            string sessionId = Guid.NewGuid().ToString();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse<SubscriptionValidation>(rawEvent, sessionId));
        }

        [Fact]
        public void Parse_NoEventWithValidSessionIdSpecified_ThrowsException()
        {
            // Arrange
            string rawEvent = null;
            string sessionId = Guid.NewGuid().ToString();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse<SubscriptionValidation>(rawEvent, sessionId));
        }

        [Fact]
        public void Parse_ValidEventWithEmptySessionIdSpecified_ThrowsException()
        {
            // Arrange
            string rawEvent = EventSamples.SubscriptionValidationEvent;
            string sessionId = string.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse<SubscriptionValidation>(rawEvent, sessionId));
        }

        [Fact]
        public void Parse_ValidEventWithoutSessionIdSpecified_ThrowsException()
        {
            // Arrange
            string rawEvent = EventSamples.SubscriptionValidationEvent;
            string sessionId = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse<SubscriptionValidation>(rawEvent, sessionId));
        }
    }
}
