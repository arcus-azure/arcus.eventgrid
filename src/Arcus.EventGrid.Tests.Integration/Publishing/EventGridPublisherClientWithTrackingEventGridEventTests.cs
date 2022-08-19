using System;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Azure;
using Azure.Messaging.EventGrid;
using Xunit;
using Arcus.EventGrid.Tests.Core;
using Xunit.Abstractions;
using Arcus.EventGrid.Tests.Integration.Fixture;
using System.Collections.Generic;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class EventGridPublisherClientWithTrackingEventGridEventTests : EventGridPublisherClientWithTrackingTests, IAsyncLifetime
    {
        private EventGridTopicEndpoint _eventGridEventEndpoint;

        public EventGridPublisherClientWithTrackingEventGridEventTests(ITestOutputHelper testOutput) 
            : base(EventSchema.EventGrid, testOutput)
        {
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            _eventGridEventEndpoint = await CreateEventConsumerHostWithTrackingAsync();
        }

        [Fact]
        public async Task SendEventGridEventAsync_Single_Succeeds()
        {
            await TestSendEventGridEventAsync((client, eventGridEvent) => client.SendEventAsync(eventGridEvent));
        }

        [Fact]
        public async Task SendEventGridEventAsync_SingleWithOptions_Succeeds()
        {
            await TestSendEventGridEventWithOptionsAsync((client, eventGridEvent) => client.SendEventAsync(eventGridEvent));
        }

        [Fact]
        public async Task SendEventGridEventAsync_SingleWithImplementation_Succeeds()
        {
            await TestSendEventGridEventWithImplementationAsync((client, eventGridEvent) => client.SendEventAsync(eventGridEvent));
        }

        [Fact]
        public async Task SendCustomEventAsync_Single_Succeeds()
        {
            await TestSendEventGridEventAsync(async (client, eventGridEvent) =>
            {
                BinaryData data = BinaryData.FromObjectAsJson(eventGridEvent);

                Response response = await client.SendEventAsync(data);
                return response;
            });
        }

        [Fact]
        public async Task SendEventGridEventAsync_Many_Succeeds()
        {
            await TestSendEventGridEventAsync((client, eventGridEvent) => client.SendEventsAsync(new[] { eventGridEvent }));
        }

        [Fact]
        public async Task SendEventGridEventAsync_ManyWithOptions_Succeeds()
        {
            await TestSendEventGridEventWithOptionsAsync((client, eventGridEvent) => client.SendEventsAsync(new[] { eventGridEvent }));
        }

        [Fact]
        public async Task SendCustomEventAsync_Many_Succeeds()
        {
            await TestSendEventGridEventAsync(async (client, eventGridEvent) =>
            {
                BinaryData data = BinaryData.FromObjectAsJson(eventGridEvent);

                Response response = await client.SendEventsAsync(new [] { data });
                return response;
            });
        }

        [Fact]
        public void SendEventGridEventSync_Single_Succeeds()
        {
            TestSendEventGridEvent((client, eventGridEvent) => client.SendEvent(eventGridEvent));
        }

        [Fact]
        public void SendEventGridEventSync_SingleWithOptions_Succeeds()
        {
            TestSendEventGridEventWithOptions((client, eventGridEvent) => client.SendEvent(eventGridEvent));
        }

        [Fact]
        public void SendCustomEventSync_Single_Succeeds()
        {
            TestSendEventGridEvent((client, eventGridEvent) =>
            {
                BinaryData data = BinaryData.FromObjectAsJson(eventGridEvent);

                Response response = client.SendEvent(data);
                return response;
            });
        }

        [Fact]
        public void SendEventGridEventSync_Many_Succeeds()
        {
            TestSendEventGridEvent((client, eventGridEvent) => client.SendEvents(new[] { eventGridEvent }));
        }

        [Fact]
        public void SendEventGridEventSync_ManyWithOptions_Succeeds()
        {
            TestSendEventGridEventWithOptions((client, eventGridEvent) => client.SendEvents(new[] { eventGridEvent }));
        }

        [Fact]
        public void SendCustomEventSync_Many_Succeeds()
        {
            TestSendEventGridEvent((client, eventGridEvent) =>
            {
                BinaryData data = BinaryData.FromObjectAsJson(eventGridEvent);

                Response response = client.SendEvents(new [] { data });
                return response;
            });
        }

        private void TestSendEventGridEvent(Func<EventGridPublisherClient, EventGridEvent, Response> usePublisher)
        {
            // Arrange
            EventGridEvent eventGridEvent = CreateEventGridEventFromData(new CarEventData("1-ARCUS-337"));
            EventGridPublisherClient client = CreateRegisteredClient();

            // Act
            using (Response response = usePublisher(client, eventGridEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }
            
            // Assert
            AssertDependencyTracking();
            AssertEventGridEventForData(eventGridEvent);
        }

        private void TestSendEventGridEventWithOptions(Func<EventGridPublisherClient, EventGridEvent, Response> usePublisher)
        {
            // Arrange
            string dependencyId = $"parent-{Guid.NewGuid()}";
            string key1 = $"key-{Guid.NewGuid()}", value1 = $"value-{Guid.NewGuid()}";
            string key2 = $"key-{Guid.NewGuid()}", value2 = $"value-{Guid.NewGuid()}";
            EventGridEvent cloudEvent = CreateEventGridEventFromData(new CarEventData("1-ARCUS-337"));
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
            AssertEventGridEventForData(cloudEvent);
        }

        private async Task TestSendEventGridEventAsync(Func<EventGridPublisherClient, EventGridEvent, Task<Response>> usePublisherAsync)
        {
            // Arrange
            EventGridEvent eventGridEvent = CreateEventGridEventFromData(new CarEventData("1-ARCUS-337"));
            EventGridPublisherClient client = CreateRegisteredClient();

            // Act
            using (Response response = await usePublisherAsync(client, eventGridEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }
            
            // Assert
            AssertDependencyTracking();
            AssertEventGridEventForData(eventGridEvent);
        }

        private async Task TestSendEventGridEventWithOptionsAsync(Func<EventGridPublisherClient, EventGridEvent, Task<Response>> usePublisherAsync)
        {
            // Arrange
            string dependencyId = $"parent-{Guid.NewGuid()}";
            string key1 = $"key-{Guid.NewGuid()}", value1 = $"value-{Guid.NewGuid()}";
            string key2 = $"key-{Guid.NewGuid()}", value2 = $"value-{Guid.NewGuid()}";
            EventGridEvent cloudEvent = CreateEventGridEventFromData(new CarEventData("1-ARCUS-337"));
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
            AssertEventGridEventForData(cloudEvent);
        }

        private async Task TestSendEventGridEventWithImplementationAsync(Func<EventGridPublisherClient, EventGridEvent, Task<Response>> usePublisherAsync)
        {
            // Arrange
            EventGridEvent eventGridEvent = CreateEventGridEventFromData(new CarEventData("1-ARCUS-337"));
            EventGridPublisherClient client = CreateRegisteredClientWithCustomImplementation();

             // Act
            using (Response response = await usePublisherAsync(client, eventGridEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }
            
            // Assert
            AssertDependencyTracking();
            AssertEventGridEventForData(eventGridEvent);
        }

        private static EventGridEvent CreateEventGridEventFromData(CarEventData eventData)
        {
            var eventGridEvent = new EventGridEvent(
                subject: BogusGenerator.Commerce.ProductName(),
                eventType: BogusGenerator.Commerce.Product(),
                dataVersion: BogusGenerator.System.Version().ToString(),
                data: eventData)
            {
                Id = $"event-{Guid.NewGuid()}",
            };

            return eventGridEvent;
        }

        private void AssertEventGridEventForData(EventGridEvent eventGridEvent)
        {
            Assert.NotNull(eventGridEvent.Data);
            var eventData = eventGridEvent.Data.ToObjectFromJson<CarEventData>();

            string receivedEvent = _eventGridEventEndpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventGridEvent.Id);
            ArcusAssert.ReceivedNewCarRegisteredEvent(eventGridEvent.Id, eventGridEvent.EventType, eventGridEvent.Subject, eventData.LicensePlate, receivedEvent);
        }

        [Fact]
        public async Task SendEventGridEventAsync_Many_FailsWhenEventDataIsNotJson()
        {
            // Arrange
            var data = BinaryData.FromBytes(BogusGenerator.Random.Bytes(100));
            var eventGridEvent = new EventGridEvent("subject", "type", "version", data);
            EventGridPublisherClient client = CreateRegisteredClient();

            // Act / Assert
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEventsAsync(new[] { eventGridEvent }));
        }

        [Fact]
        public async Task SendCustomEventAsync_Single_FailsWhenEventIsNotJson()
        {
            // Arrange
            byte[] data = BogusGenerator.Random.Bytes(100);
            EventGridPublisherClient client = CreateRegisteredClient();

            // Act / Assert
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.SendEventAsync(new BinaryData(data)));
        }

        [Fact]
        public void SendCustomEvent_Single_FailsWhenEventHasNoDataProperty()
        {
            // Arrange
            var eventData = new CarEventData("1-ARCUS-337");
            var data = BinaryData.FromObjectAsJson(eventData);

            EventGridPublisherClient client = CreateRegisteredClient();

            // Act / Assert
            Assert.ThrowsAny<InvalidOperationException>(() => client.SendEvent(data));
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (_eventGridEventEndpoint != null)
            {
                await _eventGridEventEndpoint.StopAsync();
            }
        }
    }
}
