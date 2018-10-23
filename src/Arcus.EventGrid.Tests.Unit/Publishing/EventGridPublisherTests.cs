﻿using System;
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
            const string topicEndpoint = "myTopic";
            const string authenticationKey = "myKey";
            NewCarRegistered @event = null;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridPublisher.Publish(@event));
        }

        [Fact]
        public async Task Publish_NoEventsSpecified_ShouldFailWithArgumentNullException()
        {
            // Arrange
            const string topicEndpoint = "myTopic";
            const string authenticationKey = "myKey";
            List<NewCarRegistered> events = null;

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            await Assert.ThrowsAsync<ArgumentNullException>(() => eventGridPublisher.Publish(events));
        }

        [Fact]
        public async Task Publish_EmptyCollectionOfEventsSpecified_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "myTopic";
            const string authenticationKey = "myKey";
            List<NewCarRegistered> events = new List<NewCarRegistered>();

            // Act
            var eventGridPublisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(authenticationKey)
                    .Build();

            await Assert.ThrowsAsync<ArgumentException>(() => eventGridPublisher.Publish(events));
        }
    }
}
