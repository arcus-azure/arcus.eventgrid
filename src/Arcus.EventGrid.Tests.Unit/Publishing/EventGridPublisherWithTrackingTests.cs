using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Core;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.EventGrid.Tests.Unit.Publishing.Fixtures;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Bogus;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Publishing
{
    public class EventGridPublisherWithTrackingTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Theory]
        [InlineData(EventCorrelationFormat.Hierarchical)]
        [InlineData(EventCorrelationFormat.W3C)]
        public async Task SendEventGridEventAsync_Many_FailsWhenEventDataIsNotJson(EventCorrelationFormat format)
        {
            // Arrange
            var data = BinaryData.FromBytes(BogusGenerator.Random.Bytes(100));
            var eventGridEvent = new EventGridEvent("subject", "type", "version", data);
            EventGridPublisherClient client = CreateClient(format);

            // Act / Assert
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEventsAsync(new[] { eventGridEvent }));
        }

        [Theory]
        [InlineData(EventCorrelationFormat.Hierarchical)]
        [InlineData(EventCorrelationFormat.W3C)]
        public async Task SendCustomEventAsync_Single_FailsWhenEventIsNotJson(EventCorrelationFormat format)
        {
            // Arrange
            byte[] data = BogusGenerator.Random.Bytes(100);
            EventGridPublisherClient client = CreateClient(format);

            // Act / Assert
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEventAsync(new BinaryData(data)));
        }

        [Theory]
        [InlineData(EventCorrelationFormat.Hierarchical)]
        [InlineData(EventCorrelationFormat.W3C)]
        public void SendCustomEvent_Single_FailsWhenEventHasNoDataProperty(EventCorrelationFormat format)
        {
            // Arrange
            var eventData = new CarEventData("1-ARCUS-337");
            var data = BinaryData.FromObjectAsJson(eventData);

            EventGridPublisherClient client = CreateClient(format);

            // Act / Assert
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEvent(data));
        }

        [Theory]
        [InlineData(EventCorrelationFormat.Hierarchical)]
        [InlineData(EventCorrelationFormat.W3C)]
        public async Task SendCloudEventAsync_Single_FailWhenEventDataIsNotJson(EventCorrelationFormat format)
        {
            // Arrange
            var data = BinaryData.FromBytes(BogusGenerator.Random.Bytes(100));
            var cloudEvent = new CloudEvent("source", "type", data, "text/plain");
            EventGridPublisherClient client = CreateClient(format);

            // Act / Assert
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEventAsync(cloudEvent));
        }

        [Theory]
        [InlineData(EventCorrelationFormat.Hierarchical)]
        [InlineData(EventCorrelationFormat.W3C)]
        public async Task SendCloudEventAsync_ManyEncoded_FailWhenEventsAreNoCloudEvents(EventCorrelationFormat format)
        {
            // Arrange
            var data = BogusGenerator.Random.Bytes(100);
            var memory = new ReadOnlyMemory<byte>(data);
            EventGridPublisherClient client = CreateClient(format);

            // Act / Assert
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEncodedCloudEventsAsync(memory));
        }

        [Theory]
        [InlineData(EventCorrelationFormat.Hierarchical)]
        [InlineData(EventCorrelationFormat.W3C)]
        public void SendCloudEventSync_ManyEncoded_FailWhenEventsAreNoCloudEvents(EventCorrelationFormat format)
        {
            // Arrange
            var data = BogusGenerator.Random.Bytes(100);
            var memory = new ReadOnlyMemory<byte>(data);
            EventGridPublisherClient client = CreateClient(format);

            // Act / Assert
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEncodedCloudEvents(memory));
        }

        private static EventGridPublisherClient CreateClient(EventCorrelationFormat format)
        {
            var options = new EventGridPublisherClientWithTrackingOptions
            {
                Format = format
            };

            return new StubEventGridPublisherClientWithTracking(options);
        }
    }
}
