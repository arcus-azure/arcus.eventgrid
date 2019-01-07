using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Tests.Core.Events;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Publishing
{
    public class EventGridPublisherTests
    {
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
            await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridPublisher.Publish(@event));
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
            await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridPublisher.PublishMany(events));
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
            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.PublishMany(events));
        }
    }
}
