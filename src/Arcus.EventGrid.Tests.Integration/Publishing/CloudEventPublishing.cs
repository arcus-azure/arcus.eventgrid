using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.EventGrid.Tests.Integration.Fixture;
using CloudNative.CloudEvents;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait(name: "Category", value: "Integration")]
    public class CloudEventPublishing : IAsyncLifetime
    {
        private readonly ITestOutputHelper _testOutput;
        private EventGridTopicEndpoint _endpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventPublishing"/> class.
        /// </summary>
        public CloudEventPublishing(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            _endpoint = await EventGridTopicEndpoint.CreateForCloudEventAsync(_testOutput);
        }

        [Fact]
        public async Task PublishSingleCloudEvent_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var eventId = Guid.NewGuid().ToString();
            var @event = new CloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), eventSubject, eventId)
            {
                Data = new CarEventData(licensePlate),
                DataContentType = new ContentType("application/json")
            };

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishAsync(@event);
            TracePublishedEvent(eventId, @event);

            // Assert
            string receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            AssertReceivedNewCarRegisteredEvent(eventId, @event.Type, eventSubject, licensePlate, receivedEvent);
        }

        [Fact]
        public async Task PublishMultipleCloudEvent_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var firstEventId = Guid.NewGuid().ToString();
            var firstEvent = new CloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), eventSubject, firstEventId)
            {
                Data = new CarEventData(licensePlate),
                DataContentType = new ContentType("application/json")
            };
            var secondEventId = Guid.NewGuid().ToString();
            var secondEvent = new CloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), eventSubject, secondEventId)
            {
                Data = new CarEventData(licensePlate),
                DataContentType = new ContentType("application/json")
            };
            CloudEvent[] cloudEvents = { firstEvent, secondEvent };

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishManyAsync(cloudEvents);

            // Assert
            Assert.All(cloudEvents, cloudEvent => AssertReceivedNewCarRegisteredEventWithTimeout(cloudEvent, licensePlate));
        }

        private void AssertReceivedNewCarRegisteredEventWithTimeout(CloudEvent @event, string licensePlate)
        {
            TracePublishedEvent(@event.Id, @event);
            string receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(@event.Id, timeout: TimeSpan.FromSeconds(30));
            AssertReceivedNewCarRegisteredEvent(@event.Id, @event.Type, @event.Subject, licensePlate, receivedEvent);
        }

        [Fact]
        public async Task PublishSingleRawEvent_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string licensePlate = "1-TOM-337";
            const string expectedSubject = "/";
            var eventId = Guid.NewGuid().ToString();
            var data = new CarEventData(licensePlate);
            var rawEventBody = JsonConvert.SerializeObject(data);
            var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), subject: expectedSubject, id: eventId)
            {
                Data = data,
                DataContentType = new ContentType("application/json")
            };

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishRawCloudEventAsync(cloudEvent.SpecVersion, cloudEvent.Id, cloudEvent.Type, cloudEvent.Source, rawEventBody);
            TracePublishedEvent(eventId, cloudEvent);

            // Assert
            var receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            AssertReceivedNewCarRegisteredEvent(eventId, cloudEvent.Type, expectedSubject, licensePlate, receivedEvent);
        }

        [Fact]
        public async Task PublishSingleRawEventWithDetailedInfo_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string licensePlate = "1-TOM-337";
            const string expectedSubject = "/";
            var eventId = Guid.NewGuid().ToString();
            var data = new CarEventData(licensePlate);
            var rawEventBody = JsonConvert.SerializeObject(data);
            var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), subject: expectedSubject, id: eventId, DateTime.UtcNow)
            {
                Data = data,
                DataContentType = new ContentType("application/json")
            };

            IEventGridPublisher publisher = _endpoint.BuildPublisher();

            // Act
            await publisher.PublishRawCloudEventAsync(
                cloudEvent.SpecVersion,
                cloudEvent.Id,
                cloudEvent.Type,
                cloudEvent.Source,
                cloudEvent.Subject,
                rawEventBody,
                cloudEvent.Time ?? default(DateTimeOffset));
            TracePublishedEvent(eventId, cloudEvent);

            // Assert
            var receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            AssertReceivedNewCarRegisteredEvent(eventId, cloudEvent.Type, cloudEvent.Subject, licensePlate, receivedEvent);
        }

        private void TracePublishedEvent(string eventId, object events)
        {
            _testOutput.WriteLine($"Event '{eventId}' published - {JsonConvert.SerializeObject(events)}");
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
