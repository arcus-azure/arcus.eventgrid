using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Contracts.Interfaces;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.EventGrid.Tests.Core;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.EventGrid.Tests.Integration.Fixture;
using Arcus.Testing.Logging;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait(name: "Category", value: "Integration")]
    public class EventGridEventPublishingTests : IAsyncLifetime
    {
        private readonly TestConfig _config = TestConfig.Create();
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
            _endpoint = await EventGridTopicEndpoint.CreateForEventGridEventAsync(_config, _testOutput);
        }

        [Fact]
        public async Task PublishSingleEventGridEvent_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var eventId = Guid.NewGuid().ToString();
            var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);

            IEventGridPublisher publisher = EventPublisherFactory.CreateEventGridEventPublisher(_config);

            // Act
            await publisher.PublishAsync(@event);
            TracePublishedEvent(eventId, @event);

            // Assert
            var receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            ArcusAssert.ReceivedNewCarRegisteredEvent(eventId, @event.EventType, eventSubject, licensePlate, receivedEvent);
        }

        [Fact]
        public async Task PublishSingleEventGridEvent_WithEventPayload_ReceivesEventByEventPayload()
        {
            // Arrange
            var eventSubject = $"integration-test-{Guid.NewGuid()}";
            var licensePlate = $"1-TOM-{Guid.NewGuid():N}";
            var eventId = Guid.NewGuid().ToString();
            var expected = new NewCarRegistered(eventId, eventSubject, licensePlate); 

            IEventGridPublisher publisher = EventPublisherFactory.CreateEventGridEventPublisher(_config);

            // Act
            await publisher.PublishAsync(expected);
            TracePublishedEvent(eventId, expected);

            // Assert
            Event actual = 
                _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent<CarEventData>(
                    data => data.LicensePlate == licensePlate, 
                    TimeSpan.FromSeconds(30));

            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Subject, actual.Subject);
            ArcusAssert.ReceivedNewCarRegisteredPayload(licensePlate, actual);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PublishSingleEventGridEventWithDependencyTracking_WithBuilder_ValidParameters_Succeeds(bool enableDependencyTracking)
        {
            // Arrange
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var eventId = Guid.NewGuid().ToString();
            var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);
            var spyLogger = new InMemoryLogger();
            
            IEventGridPublisher publisher = EventPublisherFactory.CreateEventGridEventPublisher(_config, spyLogger, options =>
            {
                options.EnableDependencyTracking = enableDependencyTracking;
            });

            // Act
            await publisher.PublishAsync(@event);
            TracePublishedEvent(eventId, @event);

            // Assert
            var receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            ArcusAssert.ReceivedNewCarRegisteredEvent(eventId, @event.EventType, eventSubject, licensePlate, receivedEvent);
            Assert.True(enableDependencyTracking == spyLogger.Messages.Any(message =>
            {
                return message.Contains("Dependency") && message.Contains("Azure Event Grid");
            }));
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

            IEventGridPublisher publisher = EventPublisherFactory.CreateEventGridEventPublisher(_config);

            // Act
            await publisher.PublishRawEventGridEventAsync(@event.Id, @event.EventType, rawEventBody);
            TracePublishedEvent(eventId, @event);

            // Assert
            EventGridEvent actual = 
                _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(
                    (EventGridEvent eventGridEvent) => eventGridEvent.Id == eventId, 
                    TimeSpan.FromSeconds(30));

            Assert.Equal(@event.Id, actual.Id);
            Assert.Equal(@event.EventType, actual.EventType);
            ArcusAssert.ReceivedNewCarRegisteredPayload(licensePlate, actual);
        }

        [Fact]
        public async Task PublishSingleRawEventWithSubject_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var eventId = Guid.NewGuid().ToString();
            var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);
            var rawEventBody = JsonConvert.SerializeObject(@event.Data);

            IEventGridPublisher publisher = EventPublisherFactory.CreateEventGridEventPublisher(_config);

            // Act
            await publisher.PublishRawEventGridEventAsync(@event.Id, @event.EventType, rawEventBody, @event.Subject);
            TracePublishedEvent(eventId, @event);

            // Assert
            var receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            ArcusAssert.ReceivedNewCarRegisteredEvent(eventId, @event.EventType, eventSubject, licensePlate, receivedEvent);
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

            IEventGridPublisher publisher = EventPublisherFactory.CreateEventGridEventPublisher(_config);

            // Act
            await publisher.PublishRawEventGridEventAsync(@event.Id, @event.EventType, rawEventBody, @event.Subject, @event.DataVersion, @event.EventTime);
            TracePublishedEvent(eventId, @event);

            // Assert
            var receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            ArcusAssert.ReceivedNewCarRegisteredEvent(eventId, @event.EventType, eventSubject, licensePlate, receivedEvent);
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

            IEventGridPublisher publisher = EventPublisherFactory.CreateEventGridEventPublisher(_config);

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

            IEventGridPublisher publisher = EventPublisherFactory.CreateEventGridEventPublisher(_config);

            // Act
            await publisher.PublishManyAsync(events);

            // Assert
            Assert.All(events, rawEvent => AssertReceivedEventWithRetryCount(rawEvent, licensePlate));
        }

        private void AssertReceivedEventWithRetryCount(IEvent @event, string licensePlate)
        {
            TracePublishedEvent(@event.Id, @event);
            string receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(@event.Id, retryCount: 5);
            ArcusAssert.ReceivedNewCarRegisteredEvent(@event.Id, @event.EventType, @event.Subject, licensePlate, receivedEvent);
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

            IEventGridPublisher publisher = EventPublisherFactory.CreateEventGridEventPublisher(_config);

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

            IEventGridPublisher publisher = EventPublisherFactory.CreateEventGridEventPublisher(_config);

            // Act
            await publisher.PublishManyAsync(events);

            // Assert
            Assert.All(events, rawEvent => AssertReceivedNewCarRegisteredEventWithTimeout(rawEvent, licensePlate));
        }

        private void AssertReceivedNewCarRegisteredEventWithTimeout(IEvent @event, string licensePlate)
        {
            TracePublishedEvent(@event.Id, @event);
            string receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(@event.Id, timeout: TimeSpan.FromSeconds(30));
            ArcusAssert.ReceivedNewCarRegisteredEvent(@event.Id, @event.EventType, @event.Subject, licensePlate, receivedEvent);
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