using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Core;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Xunit;
using Arcus.EventGrid.Tests.Core;
using Xunit.Abstractions;
using Arcus.EventGrid.Tests.Integration.Fixture;
using SendCloudEventAsync = System.Func<Azure.Messaging.EventGrid.EventGridPublisherClient, Azure.Messaging.CloudEvent, System.Threading.Tasks.Task<Azure.Response>>;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class EventGridPublisherClientWithTrackingCloudEventsTests : EventGridPublisherClientWithTrackingTests
    {
        public EventGridPublisherClientWithTrackingCloudEventsTests(ITestOutputHelper testOutput) 
            : base(EventSchema.CloudEvent, testOutput)
        {
        }

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
        [MemberData(nameof(SendCloudEventOverloads))]
        public async Task SendCloudEventAsync_WithoutOptions_Succeeds(SendCloudEventAsync usePublisherAsync)
        {
            await TestWithEventConsumerHostWithTrackingAsync(async (format, endpoint) =>
            {
                EventGridPublisherClient client = CreateRegisteredClient(format);
                await TestSendCloudEventAsync(client, usePublisherAsync, endpoint);
            });
        }

        [Theory]
        [MemberData(nameof(SendCloudEventOverloads))]
        public async Task SendCloudEventAsync_WithOptions_Succeeds(SendCloudEventAsync usePublisherAsync)
        {
            await TestWithEventConsumerHostWithTrackingAsync(async (format, endpoint) =>
            {
                EventGridPublisherClient client = CreateRegisteredClientWithCustomOptions(format);
                await TestSendCloudEventAsync(client, usePublisherAsync, endpoint);
            });
        }

        [Theory]
        [MemberData(nameof(SendCloudEventOverloads))]
        public async Task SendCloudEventAsync_WithCustomImplementation_Succeeds(SendCloudEventAsync usePublisherAsync)
        {
            await TestWithEventConsumerHostWithTrackingAsync(async (format, endpoint) =>
            {
                EventGridPublisherClient client = CreateRegisteredClientWithCustomImplementation(format);
                await TestSendCloudEventAsync(client, usePublisherAsync, endpoint);
            });
        }

        [Theory]
        [MemberData(nameof(SendCloudEventOverloads))]
        public async Task SendCloudEventAsyncUsingManagedIdentity_WithoutOptions_Succeeds(SendCloudEventAsync usePublisherAsync)
        {
            await TestWithEventConsumerHostWithTrackingAsync(async (format, endpoint) =>
            {
                using (TemporaryManagedIdentityConnection.Create(Configuration))
                {
                    EventGridPublisherClient client = CreateRegisteredClientUsingManagedIdentity(format);
                    await TestSendCloudEventAsync(client, usePublisherAsync, endpoint);
                }
            });
        }

        [Theory]
        [MemberData(nameof(SendCloudEventOverloads))]
        public async Task SendCloudEventAsyncUsingManagedIdentity_WithOptions_Succeeds(SendCloudEventAsync usePublisherAsync)
        {
            await TestWithEventConsumerHostWithTrackingAsync(async (format, endpoint) =>
            {
                using (TemporaryManagedIdentityConnection.Create(Configuration))
                {
                    EventGridPublisherClient client = CreateRegisteredClientUsingManagedIdentityWithCustomOptions(format);
                    await TestSendCloudEventAsync(client, usePublisherAsync, endpoint);
                }
            });
        }

        private async Task TestWithEventConsumerHostWithTrackingAsync(Func<EventCorrelationFormat, EventGridTopicEndpoint, Task> testBody)
        {
            foreach (var format in new [] { EventCorrelationFormat.Hierarchical, EventCorrelationFormat.W3C })
            {
                await using (EventGridTopicEndpoint endpoint = await CreateEventConsumerHostWithTrackingAsync(format))
                {
                    await testBody(format, endpoint);
                }
            }
        }

        private async Task TestSendCloudEventAsync(
            EventGridPublisherClient client,
            SendCloudEventAsync usePublisherAsync,
            EventGridTopicEndpoint endpoint)
        {
            CloudEvent cloudEvent = CreateCloudEvent();

            // Act
            using (Response response = await usePublisherAsync(client, cloudEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }

            // Assert
            AssertDependencyTracking();
            AssertCloudEventForData(cloudEvent, endpoint);
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

        private void AssertCloudEventForData(CloudEvent cloudEvent, EventGridTopicEndpoint endpoint)
        {
            Assert.NotNull(cloudEvent.Data);
            var eventData = cloudEvent.Data.ToObjectFromJson<CarEventData>();

            string receivedEvent = endpoint.ConsumerHost.GetReceivedEventOrFail(cloudEvent.Id);
            ArcusAssert.ReceivedNewCarRegisteredEvent(cloudEvent.Id, cloudEvent.Type, cloudEvent.Subject, eventData.LicensePlate, receivedEvent);
        }

        [Theory]
        [InlineData(EventCorrelationFormat.Hierarchical)]
        [InlineData(EventCorrelationFormat.W3C)]
        public async Task SendCloudEventAsync_Single_FailWhenEventDataIsNotJson(EventCorrelationFormat format)
        {
            // Arrange
            var data = BinaryData.FromBytes(BogusGenerator.Random.Bytes(100));
            var cloudEvent = new CloudEvent("source", "type", data, "text/plain");
            EventGridPublisherClient client = CreateRegisteredClient(format);

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
            EventGridPublisherClient client = CreateRegisteredClient(format);

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
            EventGridPublisherClient client = CreateRegisteredClient(format);

            // Act / Assert
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEncodedCloudEvents(memory));
        }
    }
}
