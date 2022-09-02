using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Xunit;
using Arcus.EventGrid.Tests.Core;
using Xunit.Abstractions;
using Arcus.EventGrid.Tests.Integration.Fixture;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class EventGridPublisherClientWithTrackingCloudEventsTests : EventGridPublisherClientWithTrackingTests, IAsyncLifetime
    {
        private EventGridTopicEndpoint _cloudEventEndpoint;

        public EventGridPublisherClientWithTrackingCloudEventsTests(ITestOutputHelper testOutput) 
            : base(EventSchema.CloudEvent, testOutput)
        {
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            _cloudEventEndpoint = await CreateEventConsumerHostWithTrackingAsync();
        }

        [Fact]
        public async Task SendCloudEventAsync_Single_Succeeds()
        {
            await TestSendCloudEventAsync((client, cloudEvent) => client.SendEventAsync(cloudEvent));
        }

        [Fact]
        public async Task SendCloudEventAsync_SingleWithOptions_Succeeds()
        {
            await TestSendCloudEventWithOptionsAsync((client, cloudEvent) => client.SendEventAsync(cloudEvent));
        }

        [Fact]
        public async Task SendCloudEventAsync_SingleWithImplementation_Succeeds()
        {
            await TestSendCloudEventWithImplementationAsync((client, cloudEvent) => client.SendEventAsync(cloudEvent));
        }

        [Fact]
        public async Task SendCloudEventAsync_Many_Succeeds()
        {
            await TestSendCloudEventAsync((client, cloudEvent) => client.SendEventsAsync(new[] { cloudEvent }));
        }

        [Fact]
        public async Task SendCloudEventAsync_ManyWithOptions_Succeeds()
        {
            await TestSendCloudEventWithOptionsAsync((client, cloudEvent) => client.SendEventsAsync(new[] { cloudEvent }));
        }

        [Fact]
        public async Task SendCloudEventAsync_SingleOnChannel_Succeeds()
        {
            await TestSendCloudEventAsync((client, cloudEvent) => client.SendEventAsync(cloudEvent, "some-channel"));
        }

        [Fact]
        public async Task SendCloudEventAsync_SingleOnChannelWithOptions_Succeeds()
        {
            await TestSendCloudEventWithOptionsAsync((client, cloudEvent) => client.SendEventAsync(cloudEvent, "some-channel"));
        }

        [Fact]
        public async Task SendCloudEventAsync_ManyOnChannel_Succeeds()
        {
           await TestSendCloudEventAsync((client, cloudEvent) => client.SendEventsAsync(new[] { cloudEvent }, "some-channel"));
        }

        [Fact]
        public async Task SendCloudEventAsync_ManyOnChannelWithOptions_Succeeds()
        {
            await TestSendCloudEventWithOptionsAsync((client, cloudEvent) => client.SendEventsAsync(new[] { cloudEvent }, "some-channel"));
        }

        [Fact]
        public async Task SendCloudEventAsync_ManyEncoded_Succeeds()
        {
            await TestSendCloudEventAsync(async (client, cloudEvent) =>
            {
                ReadOnlyMemory<byte> memory = EncodeCloudEvent(cloudEvent);

                Response response = await client.SendEncodedCloudEventsAsync(memory);
                return response;
            });
        }

        [Fact]
        public async Task SendCloudEventAsync_ManyEncodedWithOptions_Succeeds()
        {
            await TestSendCloudEventAsync(async (client, cloudEvent) =>
            {
                ReadOnlyMemory<byte> memory = EncodeCloudEvent(cloudEvent);

                Response response = await client.SendEncodedCloudEventsAsync(memory);
                return response;
            });
        }

        [Fact]
        public void SendCloudEventSync_Single_Succeeds()
        {
            TestSendCloudEvent((client, cloudEvent) => client.SendEvent(cloudEvent));
        }

        [Fact]
        public void SendCloudEventSync_SingleWithOptions_Succeeds()
        {
            TestSendCloudEventWithOptions((client, cloudEvent) => client.SendEvent(cloudEvent));
        }

        [Fact]
        public void SendCloudEventSync_Many_Succeeds()
        {
            TestSendCloudEvent((client, cloudEvent) => client.SendEvents(new[] { cloudEvent }));
        }

        [Fact]
        public void SendCloudEventSync_ManyWithOptions_Succeeds()
        {
            TestSendCloudEventWithOptions((client, cloudEvent) => client.SendEvents(new[] { cloudEvent }));
        }

        [Fact]
        public void SendCloudEventSync_SingleOnChannel_Succeeds()
        {
            TestSendCloudEvent((client, cloudEvent) => client.SendEvent(cloudEvent, "some-channel"));
        }

        [Fact]
        public void SendCloudEventSync_SingleOnChannelWithOptions_Succeeds()
        {
            TestSendCloudEventWithOptions((client, cloudEvent) => client.SendEvent(cloudEvent, "some-channel"));
        }

        [Fact]
        public void SendCloudEventSync_ManyOnChannel_Succeeds()
        {
            TestSendCloudEvent((client, cloudEvent) => client.SendEvents(new[] { cloudEvent }, "some-channel"));
        }

        [Fact]
        public void SendCloudEventSync_ManyOnChannelWithOptions_Succeeds()
        {
            TestSendCloudEventWithOptions((client, cloudEvent) => client.SendEvents(new[] { cloudEvent }, "some-channel"));
        }

        [Fact]
        public void SendCloudEventSync_ManyEncoded_Succeeds()
        {
            TestSendCloudEvent((client, cloudEvent) =>
            {
                ReadOnlyMemory<byte> memory = EncodeCloudEvent(cloudEvent);

                Response response = client.SendEncodedCloudEvents(memory);
                return response;
            });
        }

        [Fact]
        public void SendCloudEventSync_ManyEncodedWithOptions_Succeeds()
        {
            TestSendCloudEventWithOptions((client, cloudEvent) =>
            {
                ReadOnlyMemory<byte> memory = EncodeCloudEvent(cloudEvent);

                Response response = client.SendEncodedCloudEvents(memory);
                return response;
            });
        }

        private static ReadOnlyMemory<byte> EncodeCloudEvent(CloudEvent cloudEvent)
        {
            BinaryData binary = BinaryData.FromObjectAsJson(new[] { cloudEvent });
            var memory = new ReadOnlyMemory<byte>(binary.ToArray());
            
            return memory;
        }

        private void TestSendCloudEvent(Func<EventGridPublisherClient, CloudEvent, Response> usePublisher)
        {
            CloudEvent cloudEvent = CreateCloudEventFromData(new CarEventData("1-ARCUS-337"));
            EventGridPublisherClient client = CreateRegisteredClient();

            // Act
            using (Response response = usePublisher(client, cloudEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }

            // Assert
            AssertDependencyTracking();
            AssertCloudEventForData(cloudEvent);
        }

        private void TestSendCloudEventWithOptions(Func<EventGridPublisherClient, CloudEvent, Response> usePublisher)
        {
            // Arrange
            string dependencyId = $"parent-{Guid.NewGuid()}";
            string key1 = $"key-{Guid.NewGuid()}", value1 = $"value-{Guid.NewGuid()}";
            string key2 = $"key-{Guid.NewGuid()}", value2 = $"value-{Guid.NewGuid()}";
            CloudEvent cloudEvent = CreateCloudEventFromData(new CarEventData("1-ARCUS-337"));
            EventGridPublisherClient client = CreateRegisteredClientWithCustomOptions(dependencyId, options =>
            {
                options.AddTelemetryContext(new Dictionary<string, object> { [key1] = value1 });
                options.AddTelemetryContext(new Dictionary<string, object> { [key2] = value2, [key1] = value2 });
            });

            // Act
            using (Response response = usePublisher(client, cloudEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }

            // Assert
            string logMessage = AssertDependencyTracking(dependencyId);
            Assert.Contains(key1, logMessage);
            Assert.DoesNotContain(value1, logMessage);
            Assert.Contains(key2, logMessage);
            Assert.Contains(value2, logMessage);
            AssertCloudEventForData(cloudEvent);
        }

        private async Task TestSendCloudEventAsync(Func<EventGridPublisherClient, CloudEvent, Task<Response>> usePublisherAsync)
        {
            CloudEvent cloudEvent = CreateCloudEventFromData(new CarEventData("1-ARCUS-337"));
            EventGridPublisherClient client = CreateRegisteredClient();

            // Act
            using (Response response = await usePublisherAsync(client, cloudEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }

            // Assert
            AssertDependencyTracking();
            AssertCloudEventForData(cloudEvent);
        }

        private async Task TestSendCloudEventWithOptionsAsync(Func<EventGridPublisherClient, CloudEvent, Task<Response>> usePublisherAsync)
        {
            // Arrange
            string dependencyId = $"parent-{Guid.NewGuid()}";
            string key1 = $"key-{Guid.NewGuid()}", value1 = $"value-{Guid.NewGuid()}";
            string key2 = $"key-{Guid.NewGuid()}", value2 = $"value-{Guid.NewGuid()}";
            CloudEvent cloudEvent = CreateCloudEventFromData(new CarEventData("1-ARCUS-337"));
            EventGridPublisherClient client = CreateRegisteredClientWithCustomOptions(dependencyId, options =>
            {
                options.AddTelemetryContext(new Dictionary<string, object> { [key1] = value1 });
                options.AddTelemetryContext(new Dictionary<string, object> { [key2] = value2, [key1] = value2 });
            });

            // Act
            using (Response response = await usePublisherAsync(client, cloudEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }

            // Assert
            string logMessage = AssertDependencyTracking(dependencyId);
            Assert.Contains(key1, logMessage);
            Assert.DoesNotContain(value1, logMessage);
            Assert.Contains(key2, logMessage);
            Assert.Contains(value2, logMessage);
            AssertCloudEventForData(cloudEvent);
        }

        private async Task TestSendCloudEventWithImplementationAsync(Func<EventGridPublisherClient, CloudEvent, Task<Response>> usePublisherAsync)
        {
            // Arrange
            CloudEvent cloudEvent = CreateCloudEventFromData(new CarEventData("1-ARCUS-337"));
            EventGridPublisherClient client = CreateRegisteredClientWithCustomImplementation();

            // ACt
            using (Response response = await usePublisherAsync(client, cloudEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }

            // Assert
            AssertDependencyTracking();
            AssertCloudEventForData(cloudEvent);
        }

        private static CloudEvent CreateCloudEventFromData(CarEventData eventData)
        {
            var cloudEvent = new CloudEvent(
                source: BogusGenerator.Internet.UrlWithPath(),
                type: BogusGenerator.Commerce.Product(),
                jsonSerializableData: eventData)
            {
                Id = $"event-{Guid.NewGuid()}",
                Subject = BogusGenerator.Commerce.ProductName()
            };

            return cloudEvent;
        }

        private void AssertCloudEventForData(CloudEvent cloudEvent)
        {
            Assert.NotNull(cloudEvent.Data);
            var eventData = cloudEvent.Data.ToObjectFromJson<CarEventData>();

            string receivedEvent = _cloudEventEndpoint.ServiceBusEventConsumerHost.GetReceivedEvent(cloudEvent.Id);
            ArcusAssert.ReceivedNewCarRegisteredEvent(cloudEvent.Id, cloudEvent.Type, cloudEvent.Subject, eventData.LicensePlate, receivedEvent);
        }

        [Fact]
        public async Task SendCloudEventAsync_Single_FailWhenEventDataIsNotJson()
        {
            // Arrange
            var data = BinaryData.FromBytes(BogusGenerator.Random.Bytes(100));
            var cloudEvent = new CloudEvent("source", "type", data, "text/plain");
            EventGridPublisherClient client = CreateRegisteredClient();

            // Act / Assert
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEventAsync(cloudEvent));
        }

        [Fact]
        public async Task SendCloudEventAsync_ManyEncoded_FailWhenEventsAreNoCloudEvents()
        {
            // Arrange
            var data = BogusGenerator.Random.Bytes(100);
            var memory = new ReadOnlyMemory<byte>(data);
            EventGridPublisherClient client = CreateRegisteredClient();

            // Act / Assert
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEncodedCloudEventsAsync(memory));
        }

        [Fact]
        public void SendCloudEventSync_ManyEncoded_FailWhenEventsAreNoCloudEvents()
        {
            // Arrange
            var data = BogusGenerator.Random.Bytes(100);
            var memory = new ReadOnlyMemory<byte>(data);
            EventGridPublisherClient client = CreateRegisteredClient();

            // Act / Assert
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEncodedCloudEvents(memory));
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (_cloudEventEndpoint != null)
            {
                await _cloudEventEndpoint.StopAsync();
            }
        }
    }
}
