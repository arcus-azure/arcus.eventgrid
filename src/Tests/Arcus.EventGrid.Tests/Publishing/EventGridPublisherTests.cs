using System;
using Arcus.EventGrid.Publishing;
using Xunit;

namespace Arcus.EventGrid.Tests.Publishing
{
    public class EventGridPublisherTests
    {
        [Fact]
        public void Create_HasEmptyEndpointKey_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "https://savanh-grid-lab.westcentralus-1.eventgrid.azure.net/api/events";
            const string endpointKey = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridPublisher.Create(topicEndpoint, endpointKey));
        }

        [Fact]
        public void Create_HasEmptyTopicEndpoint_ShouldFailWithArgumentException()
        {
            // Arrange
            const string topicEndpoint = "";
            const string endpointKey = "IvjeulNI4OzwQOAA+Ba7gefZr230oqmBQptaz6UOUMc=";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => EventGridPublisher.Create(topicEndpoint, endpointKey));
        }

        [Fact]
        public void Create_HasNoEndpointKey_ShouldFailWithArgumentNullException()
        {
            // Arrange
            const string topicEndpoint = "https://savanh-grid-lab.westcentralus-1.eventgrid.azure.net/api/events";
            const string endpointKey = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EventGridPublisher.Create(topicEndpoint, endpointKey));
        }

        [Fact]
        public void Create_HasNoTopicEndpoint_ShouldFailWithArgumentNullException()
        {
            // Arrange
            const string topicEndpoint = null;
            const string endpointKey = "IvjeulNI4OzwQOAA+Ba7gefZr230oqmBQptaz6UOUMc=";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EventGridPublisher.Create(topicEndpoint, endpointKey));
        }
    }
}
