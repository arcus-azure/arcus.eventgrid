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
using SendEventGridEventAsync = System.Func<Azure.Messaging.EventGrid.EventGridPublisherClient, Azure.Messaging.EventGrid.EventGridEvent, System.Threading.Tasks.Task<Azure.Response>>;

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
            }) },
        };

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            _eventGridEventEndpoint = await CreateEventConsumerHostWithTrackingAsync();
        }

        [Theory]
        [MemberData(nameof(SendEventGridEventOverloads))]
        public async Task SendEventGridEventAsync_WithoutOptions_Succeeds(SendEventGridEventAsync usePublisherAsync)
        {
            EventGridPublisherClient client = CreateRegisteredClient();
            await TestSendEventGridEventAsync(client, usePublisherAsync);
        }

        [Theory]
        [MemberData(nameof(SendEventGridEventOverloads))]
        public async Task SendEventGridEventAsync_WithOptions_Succeeds(SendEventGridEventAsync usePublisherAsync)
        {
            EventGridPublisherClient client = CreateRegisteredClientWithCustomOptions();
            await TestSendEventGridEventAsync(client, usePublisherAsync);
        }

        [Theory]
        [MemberData(nameof(SendEventGridEventOverloads))]
        public async Task SendEventGridEventAsync_WithCustomImplementation_Succeeds(SendEventGridEventAsync usePublisherAsync)
        {
            EventGridPublisherClient client = CreateRegisteredClientWithCustomImplementation();
            await TestSendEventGridEventAsync(client, usePublisherAsync);
        }

        private async Task TestSendEventGridEventAsync(EventGridPublisherClient client, SendEventGridEventAsync usePublisherAsync)
        {
            // Arrange
            EventGridEvent eventGridEvent = CreateEventGridEvent();

            // Act
            using (Response response = await usePublisherAsync(client, eventGridEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }

            // Assert
            AssertDependencyTracking();
            AssertEventGridEventForData(eventGridEvent);
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

        private void AssertEventGridEventForData(EventGridEvent eventGridEvent)
        {
            Assert.NotNull(eventGridEvent.Data);
            var eventData = eventGridEvent.Data.ToObjectFromJson<CarEventData>();

            string receivedEvent = _eventGridEventEndpoint.ConsumerHost.GetReceivedEventOrFail(eventGridEvent.Id);
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
