using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.EventGrid.Tests.Core;
using Arcus.EventGrid.Tests.Core.Events;
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
        private readonly TestConfig _config = TestConfig.Create();
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
            _endpoint = await EventGridTopicEndpoint.CreateForCloudEventAsync(_config, _testOutput);
        }

        [Fact]
        public async Task PublishSingleCloudEvent_WithInvalidRetrieval_TimesOut()
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

            IEventGridPublisher publisher = EventPublisherFactory.CreateCloudEventPublisher(_config);

            // Act
            await publisher.PublishAsync(@event);
            TracePublishedEvent(eventId, @event);

            // Assert
            Assert.Throws<TimeoutException>(
                () => _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(
                    (CloudEvent cloudEvent) => cloudEvent.Id == "not existing ID", 
                    timeout: TimeSpan.FromSeconds(5)));
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

            IEventGridPublisher publisher = EventPublisherFactory.CreateCloudEventPublisher(_config);

            // Act
            await publisher.PublishAsync(@event);
            TracePublishedEvent(eventId, @event);

            // Assert
            string receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            ArcusAssert.ReceivedNewCarRegisteredEvent(eventId, @event.Type, eventSubject, licensePlate, receivedEvent);
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

            IEventGridPublisher publisher = EventPublisherFactory.CreateCloudEventPublisher(_config);

            // Act
            await publisher.PublishManyAsync(cloudEvents);

            // Assert
            Assert.All(cloudEvents, cloudEvent => AssertReceivedNewCarRegisteredEventWithTimeout(cloudEvent, licensePlate));
        }

        private void AssertReceivedNewCarRegisteredEventWithTimeout(CloudEvent @event, string licensePlate)
        {
            TracePublishedEvent(@event.Id, @event);
            string receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(@event.Id, timeout: TimeSpan.FromSeconds(30));
            ArcusAssert.ReceivedNewCarRegisteredEvent(@event.Id, @event.Type, @event.Subject, licensePlate, receivedEvent);
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

            IEventGridPublisher publisher = EventPublisherFactory.CreateCloudEventPublisher(_config);

            // Act
            await publisher.PublishRawCloudEventAsync(cloudEvent.SpecVersion, cloudEvent.Id, cloudEvent.Type, cloudEvent.Source, rawEventBody);
            TracePublishedEvent(eventId, cloudEvent);

            // Assert
            var receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            ArcusAssert.ReceivedNewCarRegisteredEvent(eventId, cloudEvent.Type, expectedSubject, licensePlate, receivedEvent);
        }
        
        [Fact]
        public async Task PublishSingleRawEventWithSubject_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string licensePlate = "1-TOM-337";
            const string expectedSubject = "/";
            var eventId = Guid.NewGuid().ToString();
            var data = new CarEventData(licensePlate);
            var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), subject: expectedSubject, id: eventId, DateTime.UtcNow)
            {
                Data = data,
                DataContentType = new ContentType("application/json")
            };

            IEventGridPublisher publisher = EventPublisherFactory.CreateCloudEventPublisher(_config);

            // Act
            _testOutput.WriteLine("Publish CloudEvent (Id='{0}') to Azure Event Grid", eventId);
            await publisher.PublishAsync(cloudEvent);
              
            TracePublishedEvent(eventId, cloudEvent);

            // Assert
            CloudEvent receivedEvent = 
                _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(
                    (CloudEvent ev) =>
                    {
                        _testOutput.WriteLine("Filter on event ID: {0} = {1}", eventId, ev.Id);
                        return ev.Id == eventId;
                    }, 
                    TimeSpan.FromSeconds(5));
            
            Assert.Equal(eventId, receivedEvent.Id);
            Assert.Equal(cloudEvent.Subject, receivedEvent.Subject);
            Assert.Equal(cloudEvent.Type, receivedEvent.Type);
            ArcusAssert.ReceivedNewCarRegisteredPayload(licensePlate, receivedEvent);
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

            IEventGridPublisher publisher = EventPublisherFactory.CreateCloudEventPublisher(_config);

            // Act
            await publisher.PublishRawCloudEventAsync(
                cloudEvent.SpecVersion,
                cloudEvent.Id,
                cloudEvent.Type,
                cloudEvent.Source,
                rawEventBody,
                cloudEvent.Subject,
                cloudEvent.Time ?? default(DateTimeOffset));
            TracePublishedEvent(eventId, cloudEvent);

            // Assert
            string receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            ArcusAssert.ReceivedNewCarRegisteredEvent(eventId, cloudEvent.Type, cloudEvent.Subject, licensePlate, receivedEvent);
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
