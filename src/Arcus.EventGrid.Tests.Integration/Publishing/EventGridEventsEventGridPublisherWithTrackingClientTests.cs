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
    public class EventGridEventsGridPublisherWithTrackingClientTests : EventGridPublisherWithTrackingClientTests, IAsyncLifetime
    {
        private EventGridTopicEndpoint _eventGridEventEndpoint;

        public EventGridEventsGridPublisherWithTrackingClientTests(ITestOutputHelper testOutput) 
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
        public void SendEventGridEventSync_Many_Succeeds()
        {
            TestSendEventGridEvent((client, eventGridEvent) => client.SendEvents(new[] { eventGridEvent }));
        }

        
        [Fact]
        public void SendEventGridEventSync_ManyWithOptions_Succeeds()
        {
            TestSendEventGridEventWithOptions((client, eventGridEvent) => client.SendEvents(new[] { eventGridEvent }));
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
            string key = $"key-{Guid.NewGuid()}", value = $"value-{Guid.NewGuid()}";
            var telemetryContext = new Dictionary<string, object> { [key] = value };
            EventGridEvent cloudEvent = CreateEventGridEventFromData(new CarEventData("1-ARCUS-337"));
            EventGridPublisherClient client = CreateRegisteredClientWithCustomOptions(dependencyId, telemetryContext);

            // Act
            using (Response response = usePublisher(client, cloudEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }

            // Assert
            string logMessage = AssertDependencyTracking(dependencyId);
            Assert.Contains(key, logMessage);
            Assert.Contains(value, logMessage);
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
            string key = $"key-{Guid.NewGuid()}", value = $"value-{Guid.NewGuid()}";
            var telemetryContext = new Dictionary<string, object> { [key] = value };
            EventGridEvent cloudEvent = CreateEventGridEventFromData(new CarEventData("1-ARCUS-337"));
            EventGridPublisherClient client = CreateRegisteredClientWithCustomOptions(dependencyId, telemetryContext);

            // Act
            using (Response response = await usePublisherAsync(client, cloudEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }

            // Assert
            string logMessage = AssertDependencyTracking(dependencyId);
            Assert.Contains(key, logMessage);
            Assert.Contains(value, logMessage);
            AssertEventGridEventForData(cloudEvent);
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
