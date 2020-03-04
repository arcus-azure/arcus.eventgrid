using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Contracts.Interfaces;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.EventGrid.Tests.Integration.Fixture;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait(name: "Category", value: "Integration")]
    public class EventGridEventPublishingTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _testOutput;

        private EventGridTopicEndpoint _endpoint;

        public EventGridEventPublishingTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            _endpoint = await EventGridTopicEndpoint.CreateForEventGridEventAsync(_testOutput);
        }

        [Fact]
        public async Task PublishSingleEventGridEvent_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var eventId = Guid.NewGuid().ToString();
            var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishAsync(@event);
            TracePublishedEvent(eventId, @event);

            // Assert
            var receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            AssertReceivedNewCarRegisteredEvent(eventId, @event.EventType, eventSubject, licensePlate, receivedEvent);
        }

        [Fact]
        public async Task PublishSingleRawEvent_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string licensePlate = "1-TOM-337";
            const string expectedSubject = "/";
            var eventId = Guid.NewGuid().ToString();
            var @event = new NewCarRegistered(eventId, licensePlate);
            var rawEventBody = JsonConvert.SerializeObject(@event.Data);

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishRawEventGridEventAsync(@event.Id, @event.EventType, rawEventBody);
            TracePublishedEvent(eventId, @event);

            // Assert
            var receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            AssertReceivedNewCarRegisteredEvent(eventId, @event.EventType, expectedSubject, licensePlate, receivedEvent);
        }

        [Fact]
        public async Task PublishSingleRawEventWithDetailedInfo_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var eventId = Guid.NewGuid().ToString();
            var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);
            var rawEventBody = JsonConvert.SerializeObject(@event.Data);

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishRawEventGridEventAsync(@event.Id, @event.EventType, rawEventBody, @event.Subject, @event.DataVersion, @event.EventTime);
            TracePublishedEvent(eventId, @event);

            // Assert
            var receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            AssertReceivedNewCarRegisteredEvent(eventId, @event.EventType, eventSubject, licensePlate, receivedEvent);
        }

        [Fact]
        public async Task PublishMultipleEvents_WithBuilder_ValidParameters_SucceedsWithRetryCount()
        {
            // Arrange
            var events =
                Enumerable
                    .Repeat<Func<Guid>>(Guid.NewGuid, 2)
                    .Select(newGuid => new NewCarRegistered(
                        newGuid().ToString(),
                        subject: "integration-test",
                        licensePlate: "1-TOM-337"))
                    .ToArray();

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishManyAsync(events);

            // Assert
            Assert.All(events, @event => AssertReceivedEventWithRetryCount(@event, @event.GetPayload()?.LicensePlate));
        }

        [Fact]
        public async Task PublishMultipleRawEvents_WithBuilder_ValidParameters_SucceedsWithRetryCount()
        {
            // Arrange
            const string licensePlate = "1-TOM-1337";
            var events =
                Enumerable
                    .Repeat<Func<Guid>>(Guid.NewGuid, 2)
                    .Select(newGuid => new RawEvent(
                                newGuid().ToString(),
                                eventSubject: "integration-test",
                                eventData: $"{{\"licensePlate\": \"{licensePlate}\"}}",
                                eventType: "Arcus.Samples.Cars.NewCarRegistered",
                                eventVersion:"1.0",
                                eventTime: DateTimeOffset.Now))
                    .ToArray();

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishManyAsync(events);

            // Assert
            Assert.All(events, rawEvent => AssertReceivedEventWithRetryCount(rawEvent, licensePlate));
        }

        private void AssertReceivedEventWithRetryCount(IEvent @event, string licensePlate)
        {
            TracePublishedEvent(@event.Id, @event);
            string receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(@event.Id, retryCount: 5);
            AssertReceivedNewCarRegisteredEvent(@event.Id, @event.EventType, @event.Subject, licensePlate, receivedEvent);
        }

        [Fact]
        public async Task PublishMultipleEvents_WithBuilder_ValidParameters_SucceedsWithTimeout()
        {
            // Arrange
            var events =
                Enumerable
                    .Repeat<Func<Guid>>(Guid.NewGuid, 2)
                    .Select(newGuid => new NewCarRegistered(
                                newGuid().ToString(),
                                subject: "integration-test",
                                licensePlate: "1-TOM-337"))
                    .ToArray();

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishManyAsync(events);

            // Assert
            Assert.All(events, @event => AssertReceivedNewCarRegisteredEventWithTimeout(@event, @event.GetPayload()?.LicensePlate));
        }

        [Fact]
        public async Task PublishMultipleRawEvents_WithBuilder_ValidParameters_SucceedsWithTimeout()
        {
            // Arrange
            const string licensePlate = "1-TOM-1337";
            var events =
                Enumerable
                    .Repeat<Func<Guid>>(Guid.NewGuid, 2)
                    .Select(newGuid => new RawEvent(
                                newGuid().ToString(),
                                eventSubject: "integration-test",
                                eventData: $"{{\"licensePlate\": \"{licensePlate}\"}}",
                                eventType: "Arcus.Samples.Cars.NewCarRegistered",
                                eventVersion:"1.0",
                                eventTime: DateTimeOffset.Now))
                    .ToArray();

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishManyAsync(events);

            // Assert
            Assert.All(events, rawEvent => AssertReceivedNewCarRegisteredEventWithTimeout(rawEvent, licensePlate));
        }

        private void AssertReceivedNewCarRegisteredEventWithTimeout(IEvent @event, string licensePlate)
        {
            TracePublishedEvent(@event.Id, @event);
            string receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(@event.Id, timeout: TimeSpan.FromSeconds(30));
            AssertReceivedNewCarRegisteredEvent(@event.Id, @event.EventType, @event.Subject, licensePlate, receivedEvent);
        }

        private static void AssertReceivedNewCarRegisteredEvent(string eventId, string eventType, string eventSubject, string licensePlate, string receivedEvent)
        {
            Assert.NotEqual(String.Empty, receivedEvent);

            EventBatch<Event> deserializedEventGridMessage = EventParser.Parse(receivedEvent);
            Assert.NotNull(deserializedEventGridMessage);
            Assert.NotEmpty(deserializedEventGridMessage.SessionId);
            Assert.NotNull(deserializedEventGridMessage.Events);

            Event deserializedEvent = Assert.Single(deserializedEventGridMessage.Events);
            Assert.NotNull(deserializedEvent);
            Assert.Equal(eventId, deserializedEvent.Id);
            Assert.Equal(eventSubject, deserializedEvent.Subject);
            Assert.Equal(eventType, deserializedEvent.EventType);

            Assert.NotNull(deserializedEvent.Data);
            var eventData = deserializedEvent.GetPayload<CarEventData>();
            Assert.NotNull(eventData);
            Assert.Equal(JsonConvert.DeserializeObject<CarEventData>(deserializedEvent.Data.ToString()), eventData);
            Assert.Equal(licensePlate, eventData.LicensePlate);
        }

        private void TracePublishedEvent(string eventId, object events)
        {
            _testOutput.WriteLine($"Event '{eventId}' published - {JsonConvert.SerializeObject(events)}");
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (_endpoint != null)
            {
                await _endpoint.StopAsync();
            }
        }
    }
}