using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Tests.Core.Events;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Publishing
{
    public class EventGridPublisherTests
    {
        [Fact]
        public async Task PublishRaw_NoRawEventWasSpecified_ShouldFailWithArgumentNullException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";

            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridPublisher.PublishAsync(null));
        }

        [Fact]
        public async Task PublishRaw_NoEventIdWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = null;
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";
            string eventSubject = "/cars/volvo";
            string dataVersion = "1.0";
            DateTimeOffset eventTime = DateTimeOffset.UtcNow;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime));
        }

        [Fact]
        public async Task PublishRawWithoutDetailedEventInfo_NoEventIdWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = null;
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";
            
            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody));
        }

        [Fact]
        public async Task PublishRaw_EmptyEventIdWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = string.Empty;
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";
            string eventSubject = "/cars/volvo";
            string dataVersion = "1.0";
            DateTimeOffset eventTime = DateTimeOffset.UtcNow;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime));
        }

        [Fact]
        public async Task PublishRaw_NoEventTypeWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = null;
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";
            string eventSubject = "/cars/volvo";
            string dataVersion = "1.0";
            DateTimeOffset eventTime = DateTimeOffset.UtcNow;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime));
        }

        [Fact]
        public async Task PublishRaw_EmptyEventTypeWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = string.Empty;
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";
            string eventSubject = "/cars/volvo";
            string dataVersion = "1.0";
            DateTimeOffset eventTime = DateTimeOffset.UtcNow;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime));
        }

        [Fact]
        public async Task PublishRaw_NoEventBodyWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = null;
            string eventSubject = "/cars/volvo";
            string dataVersion = "1.0";
            DateTimeOffset eventTime = DateTimeOffset.UtcNow;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime));
        }

        [Fact]
        public async Task PublishRaw_EventBodyIsNoJson_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = "Invalid-Body";
            string eventSubject = "/cars/volvo";
            string dataVersion = "1.0";
            DateTimeOffset eventTime = DateTimeOffset.UtcNow;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime));
        }

        [Fact]
        public async Task PublishRawWithoutDetailedEventInfo_EmptyEventIdWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = string.Empty;
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody));
        }

        [Fact]
        public async Task PublishRawWithoutDetailedEventInfo_NoEventTypeWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = null;
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody));
        }

        [Fact]
        public async Task PublishRawWithoutDetailedEventInfo_EmptyEventTypeWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = string.Empty;
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody));
        }

        [Fact]
        public async Task PublishRawWithoutDetailedEventInfo_NoEventBodyWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = null;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody));
        }

        [Fact]
        public async Task PublishRawWithoutDetailedEventInfo_EventBodyIsNoJson_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = "Invalid-Body";

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody));
        }

        [Fact]
        public async Task PublishRaw_NoEventDataVersionWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";
            string eventSubject = "/cars/volvo";
            string dataVersion = null;
            DateTimeOffset eventTime = DateTimeOffset.UtcNow;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime));
        }

        [Fact]
        public async Task PublishRaw_EmptyEventDataVersionWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";
            string eventSubject = "/cars/volvo";
            string dataVersion = string.Empty;
            DateTimeOffset eventTime = DateTimeOffset.UtcNow;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime));
        }

        [Fact]
        public async Task Publish_NoEventSpecified_ShouldFailWithArgumentNullException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            NewCarRegistered @event = null;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridPublisher.PublishAsync(@event));
        }

        [Fact]
        public async Task PublishManyRaw_NoRawEventsWasSpecified_ShouldFailWithArgumentNullException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";

            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridPublisher.PublishManyAsync(null));
        }

        [Fact]
        public async Task PublishManyRaw_AnyNullRawEventWasSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            string eventId = Guid.NewGuid().ToString();
            string eventType = "Arcus.Samples.Cars.NewCarRegistered";
            string eventBody = "{\"licensePlate\": \"1-TOM-1337\"}";
            string eventSubject = "/cars/volvo";
            string dataVersion = "1.0";
            DateTimeOffset eventTime = DateTimeOffset.UtcNow;

            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            IEnumerable<RawEvent> eventList = new[]
            {
                new RawEvent(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime),
                null
            };

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishManyAsync(eventList));
        }


        [Fact]
        public async Task Publish_NoEventsSpecified_ShouldFailWithArgumentNullException()
        {
            // Arrange
            const string topicEndpoint = "https://myTopic";
            const string authenticationKey = "myKey";
            List<NewCarRegistered> events = null;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridPublisher.PublishManyAsync(events));
        }

        [Fact]
        public async Task Publish_EmptyCollectionOfEventsSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "http://myTopic";
            const string authenticationKey = "myKey";
            List<NewCarRegistered> events = new List<NewCarRegistered>();

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishManyAsync(events));
        }
    }
}