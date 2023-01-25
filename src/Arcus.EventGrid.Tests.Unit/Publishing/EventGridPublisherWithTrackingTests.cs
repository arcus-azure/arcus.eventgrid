using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Core;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.EventGrid.Tests.Unit.Publishing.Fixtures;
using Arcus.Observability.Correlation;
using Arcus.Testing.Logging;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;
using SendEventGridEventAsync = System.Func<Azure.Messaging.EventGrid.EventGridPublisherClient, Azure.Messaging.EventGrid.EventGridEvent, System.Threading.Tasks.Task<Azure.Response>>;
using SendCloudEventAsync = System.Func<Azure.Messaging.EventGrid.EventGridPublisherClient, Azure.Messaging.CloudEvent, System.Threading.Tasks.Task<Azure.Response>>;

namespace Arcus.EventGrid.Tests.Unit.Publishing
{
    public class EventGridPublisherWithTrackingTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        public static IEnumerable<object[]> SendEventGridEventOverloads = new[]
        {
            new object[] { new SendEventGridEventAsync((client, eventGridEvent) => client.SendEventAsync(eventGridEvent)) },
            new object[] { new SendEventGridEventAsync((client, eventGridEvent) => Task.FromResult(client.SendEvent(eventGridEvent))) },
            new object[] { new SendEventGridEventAsync((client, eventGridEvent) => client.SendEventsAsync(new [] { eventGridEvent })) },
            new object[] { new SendEventGridEventAsync((client, eventGridEvent) => Task.FromResult(client.SendEvents(new [] { eventGridEvent }))) },
            new object[] { new SendEventGridEventAsync(async (client, eventGridEvent) =>
            {
                BinaryData data = BinaryData.FromObjectAsJson(eventGridEvent);
                return await client.SendEventsAsync(new[] { data });
            }) }, 
            new object[] { new SendEventGridEventAsync((client, eventGridEvent) =>
            {
                BinaryData data = BinaryData.FromObjectAsJson(eventGridEvent);
                return Task.FromResult(client.SendEvents(new[] { data }));
            }) }
        };

        public static IEnumerable<object[]> SendCloudEventOverloads = new[]
        {
            new object[] { new SendCloudEventAsync((client, cloudEvent) => client.SendEventAsync(cloudEvent)) },
            new object[] { new SendCloudEventAsync((client, cloudEvent) => Task.FromResult(client.SendEvent(cloudEvent))) },
            new object[] { new SendCloudEventAsync((client, cloudEvent) => client.SendEventAsync(cloudEvent, "some-channel")) },
            new object[] { new SendCloudEventAsync((client, cloudEvent) => Task.FromResult(client.SendEvent(cloudEvent, "some-channel"))) },
            new object[] { new SendCloudEventAsync((client, cloudEvent) => client.SendEventsAsync(new [] { cloudEvent })) },
            new object[] { new SendCloudEventAsync((client, cloudEvent) => Task.FromResult(client.SendEvents(new [] { cloudEvent }))) },
            new object[] { new SendCloudEventAsync((client, cloudEvent) => client.SendEventsAsync(new [] { cloudEvent }, "some-channel")) },
            new object[] { new SendCloudEventAsync((client, cloudEvent) => Task.FromResult(client.SendEvents(new [] { cloudEvent }, "some-channel"))) },
            new object[] { new SendCloudEventAsync(async (client, cloudEvent) =>
            {
                ReadOnlyMemory<byte> memory = EncodeCloudEvent(cloudEvent);
                return await client.SendEncodedCloudEventsAsync(memory);
            })},
            new object[] { new SendCloudEventAsync((client, cloudEvent) =>
            {
                ReadOnlyMemory<byte> memory = EncodeCloudEvent(cloudEvent);
                return Task.FromResult(client.SendEncodedCloudEvents(memory));
            })}
        };

        private static ReadOnlyMemory<byte> EncodeCloudEvent(CloudEvent cloudEvent)
        {
            BinaryData binary = BinaryData.FromObjectAsJson(new[] { cloudEvent });
            var memory = new ReadOnlyMemory<byte>(binary.ToArray());
            
            return memory;
        }

        [Theory]
        [MemberData(nameof(SendEventGridEventOverloads))]
        public async Task SendEventGridEvent_WithEvent_TracksPublishing(SendEventGridEventAsync sendEventAsync)
        {
            // Arrange
            var logger = new InMemoryLogger<EventGridPublisherClient>();
            var client = new EventGridPublisherClientWithTracking(
                "/topic-endpoint",
                new InMemoryEventGridPublisherClient(),
                new DefaultCorrelationInfoAccessor(),
                new EventGridPublisherClientWithTrackingOptions { Format = EventCorrelationFormat.Hierarchical },
                logger);
            
            EventGridEvent eventGridEvent = CreateEventGridEvent();
            
            // Act
            await sendEventAsync(client, eventGridEvent);

            // Assert
            Assert.Single(logger.Entries, entry =>
            {
                return entry.Level == LogLevel.Warning 
                       && entry.Message.StartsWith("Azure Event Grid")
                       && entry.Message.Contains("EventGridEvent") || entry.Message.Contains("Custom");
            });
        }

        private static EventGridEvent CreateEventGridEvent()
        {
            var eventGridEvent = new EventGridEvent(
                subject: BogusGenerator.Commerce.ProductName(),
                eventType: BogusGenerator.Commerce.Product(),
                dataVersion: BogusGenerator.System.Version().ToString(),
                data: new CarEventData("1-ARCUS-337"))
            {
                Id = $"event-{Guid.NewGuid()}",
            };

            return eventGridEvent;
        }

        [Theory]
        [MemberData(nameof(SendCloudEventOverloads))]
        public async Task SendCloudEvent_WithEvent_TracksPublishing(SendCloudEventAsync sendEventAsync)
        {
            // Arrange
            var logger = new InMemoryLogger<EventGridPublisherClient>();
            var client = new EventGridPublisherClientWithTracking(
                "/topic-endpoint",
                new InMemoryEventGridPublisherClient(),
                new DefaultCorrelationInfoAccessor(),
                new EventGridPublisherClientWithTrackingOptions { Format = EventCorrelationFormat.Hierarchical },
                logger);
            CloudEvent cloudEvent = CreateCloudEvent();
            
            // Act
            await sendEventAsync(client, cloudEvent);

            // Assert
            Assert.Single(logger.Entries, entry =>
            {
                return entry.Level == LogLevel.Warning 
                       && entry.Message.StartsWith("Azure Event Grid")
                       && entry.Message.Contains("CloudEvent");
            });
        }

        private static CloudEvent CreateCloudEvent()
        {
            var cloudEvent = new CloudEvent(
                source: BogusGenerator.Internet.UrlWithPath(),
                type: BogusGenerator.Commerce.Product(),
                jsonSerializableData: new CarEventData("1-ARCUS-337"))
            {
                Id = $"event-{Guid.NewGuid()}",
                Subject = BogusGenerator.Commerce.ProductName()
            };

            return cloudEvent;
        }

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
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEvent(eventGridEvent));
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEventAsync(eventGridEvent));
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEvents(new[] { eventGridEvent }));
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
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEventsAsync(new [] { new BinaryData(data) }));
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
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEvents(new [] { data }));
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
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEvent(cloudEvent));
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEventAsync(cloudEvent));
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEvents(new [] { cloudEvent }));
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEventsAsync(new [] { cloudEvent }));
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
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEncodedCloudEvents(memory));
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEncodedCloudEventsAsync(memory));
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
