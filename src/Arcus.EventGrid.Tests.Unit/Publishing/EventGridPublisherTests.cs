using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Tests.Unit.Publishing.Events;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Publishing
{
    public class EventGridPublisherTests
    {
        [Fact]
        public void Create_HasEmptyEndpointKey_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "https://savanh-grid-lab.westcentralus-1.eventgrid.azure.net/api/events";
            const string authenticationKey = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridPublisher.Create(topicEndpoint, authenticationKey));
        }

        [Fact]
        public void Create_HasEmptyTopicEndpoint_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "";
            const string authenticationKey = "IvjeulNI4OzwQOAA+Ba7gefZr230oqmBQptaz6UOUMc=";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridPublisher.Create(topicEndpoint, authenticationKey));
        }

        [Fact]
        public void Create_HasNoEndpointKey_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "https://savanh-grid-lab.westcentralus-1.eventgrid.azure.net/api/events";
            const string authenticationKey = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridPublisher.Create(topicEndpoint, authenticationKey));
        }

        [Fact]
        public void Create_HasNoTopicEndpoint_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = null;
            const string authenticationKey = "IvjeulNI4OzwQOAA+Ba7gefZr230oqmBQptaz6UOUMc=";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridPublisher.Create(topicEndpoint, authenticationKey));
        }

        [Fact]
        public async Task Publish_HasNoSubject_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "myTopic";
            const string authenticationKey = "myKey";
            const string subject = null;
            const string eventType = "eventType";
            List<NewCarRegisteredEvent> eventData = new List<NewCarRegisteredEvent>
            {
                new NewCarRegisteredEvent("1-TOM-337")
            };

            // Act
            var eventGridPublisher = 
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.Publish(subject, eventType, eventData));
        }

        [Fact]
        public async Task Publish_HasNoEventType_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "myTopic";
            const string authenticationKey = "myKey";
            const string subject = "subject";
            const string eventType = null;
            List<NewCarRegisteredEvent> eventData = new List<NewCarRegisteredEvent>
            {
                new NewCarRegisteredEvent("1-TOM-337")
            };

            // Act
            var eventGridPublisher = 
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.Publish(subject, eventType, eventData));
        }

        [Fact]
        public async Task Publish_HasNoEventData_ShouldFailWithArgumentNullException()
        {
            // Arrange
            const string topicEndpoint = "myTopic";
            const string authenticationKey = "myKey";
            const string subject = "subject";
            const string eventType = "eventType";
            List<NewCarRegisteredEvent> eventData = null;

            // Act
            var eventGridPublisher = 
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridPublisher.Publish(subject, eventType, eventData));
        }

        [Fact]
        public async Task Publish_HasEmptyEventData_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "myTopic";
            const string authenticationKey = "myKey";
            const string subject = "subject";
            const string eventType = "eventType";
            List<NewCarRegisteredEvent> eventData = new List<NewCarRegisteredEvent>();

            // Act
            var eventGridPublisher = 
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.Publish(subject, eventType, eventData));
        }
    }
}
