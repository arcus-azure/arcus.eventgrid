using System;
using System.Linq;
using Arcus.EventGrid.Security.Contracts;
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
            var eventGridMessage = EventGridMessage<SubscriptionEventData>.Parse(rawEvent);

            // Assert
            Assert.Null(eventGridMessage);
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
    }
}
