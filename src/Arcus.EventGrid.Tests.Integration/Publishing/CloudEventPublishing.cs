using System;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.EventGrid.Tests.Core;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.EventGrid.Tests.Integration.Fixture;
using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
#if NET6_0
using NewCloudEvent = Azure.Messaging.CloudEvent;
#endif
using OldCloudEvent = CloudNative.CloudEvents.CloudEvent;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait(name: "Category", value: "Integration")]
    [Collection(TestCollections.Integration)]
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
        public async Task PublishSingleCloudEvent_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var eventId = Guid.NewGuid().ToString();
            var @event = new OldCloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), eventSubject, eventId)
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

            NewCloudEvent receivedEventAsNew = 
                _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(
                    (NewCloudEvent ev) =>
                    {
                        _testOutput.WriteLine("Filter on event ID: {0} = {1}", eventId, ev.Id);
                        return ev.Id == eventId;
                    }, 
                    TimeSpan.FromSeconds(40));
            
            Assert.Equal(eventId, receivedEventAsNew.Id);
            Assert.Equal(@event.Subject, receivedEventAsNew.Subject);
            Assert.Equal(@event.Type, receivedEventAsNew.Type);
            ArcusAssert.ReceivedNewCarRegisteredPayload(licensePlate, receivedEventAsNew);
        }

        [Fact]
        public async Task PublishMultipleCloudEvent_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var firstEventId = Guid.NewGuid().ToString();
            var firstEvent = new OldCloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), eventSubject, firstEventId)
            {
                Data = new CarEventData(licensePlate),
                DataContentType = new ContentType("application/json")
            };
            var secondEventId = Guid.NewGuid().ToString();
            var secondEvent = new OldCloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), eventSubject, secondEventId)
            {
                Data = new CarEventData(licensePlate),
                DataContentType = new ContentType("application/json")
            };
            OldCloudEvent[] cloudEvents = { firstEvent, secondEvent };

            IEventGridPublisher publisher = EventPublisherFactory.CreateCloudEventPublisher(_config);

            // Act
            await publisher.PublishManyAsync(cloudEvents);

            // Assert
            Assert.All(cloudEvents, cloudEvent => AssertReceivedNewCarRegisteredEventWithTimeout(cloudEvent, licensePlate));
        }

        private void AssertReceivedNewCarRegisteredEventWithTimeout(OldCloudEvent @event, string licensePlate)
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
            var cloudEvent = new OldCloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), subject: expectedSubject, id: eventId)
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
            var rawEventBody = JsonConvert.SerializeObject(data);
            var cloudEvent = new OldCloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), subject: expectedSubject, id: eventId, DateTime.UtcNow)
            {
                Data = data,
                DataContentType = new ContentType("application/json")
            };

            IEventGridPublisher publisher = EventPublisherFactory.CreateCloudEventPublisher(_config);

            // Act
            _testOutput.WriteLine("Publish CloudEvent (Id='{0}') to Azure Event Grid", eventId);
            await publisher.PublishRawCloudEventAsync(
                cloudEvent.SpecVersion,
                cloudEvent.Id,
                cloudEvent.Type,
                cloudEvent.Source,
                rawEventBody,
                cloudEvent.Subject);

            TracePublishedEvent(eventId, cloudEvent);

            // Assert
            OldCloudEvent receivedEventAsOld = 
                _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(
                    (OldCloudEvent ev) =>
                    {
                        _testOutput.WriteLine("Filter on event ID: {0} = {1}", eventId, ev.Id);
                        return ev.Id == eventId;
                    }, 
                    TimeSpan.FromSeconds(40));
            
            Assert.Equal(eventId, receivedEventAsOld.Id);
            Assert.Equal(cloudEvent.Subject, receivedEventAsOld.Subject);
            Assert.Equal(cloudEvent.Type, receivedEventAsOld.Type);
            ArcusAssert.ReceivedNewCarRegisteredPayload(licensePlate, receivedEventAsOld);
        }

        [Fact]
        public async Task PublishSingleRawOldEventWithDetailedInfo_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            const string licensePlate = "1-TOM-337";
            const string expectedSubject = "/";
            var eventId = Guid.NewGuid().ToString();
            var data = new CarEventData(licensePlate);
            var rawEventBody = JsonConvert.SerializeObject(data);
            var cloudEvent = new OldCloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), subject: expectedSubject, id: eventId, DateTime.UtcNow)
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

        [Fact]
        public void PublishSingleOldCloudEvent_WithInvalidRetrieval_TimesOut()
        {
            Assert.Throws<TimeoutException>(
                () => _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(
                    (OldCloudEvent cloudEvent) => cloudEvent.Id == "not existing ID", 
                    timeout: TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void PublishSingleNewCloudEvent_WithInvalidRetrieval_TimesOut()
        {
            Assert.Throws<TimeoutException>(
                () => _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(
                    (NewCloudEvent cloudEvent) => cloudEvent.Id == "not existing ID", 
                    timeout: TimeSpan.FromSeconds(5)));
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
