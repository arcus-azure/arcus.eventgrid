using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Integration.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait(name: "Category", value: "Integration")]
    public class EventPublishingTests : IAsyncLifetime
    {
        private readonly XunitTestLogger _testLogger;

        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        public EventPublishingTests(ITestOutputHelper testOutput)
        {
            _testLogger = new XunitTestLogger(testOutput);

            Configuration = new ConfigurationBuilder()
                .AddJsonFile(path: "appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        }

        protected IConfiguration Configuration { get; }

        public async Task InitializeAsync()
        {
            var connectionString = Configuration.GetValue<string>("Arcus:ServiceBus:ConnectionString");
            var topicName = Configuration.GetValue<string>("Arcus:ServiceBus:TopicName");

            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);
            _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.Start(serviceBusEventConsumerHostOptions, _testLogger);
        }

        public async Task DisposeAsync()
        {
            await _serviceBusEventConsumerHost.Stop();
        }

        [Fact]
        public async Task PublishSingleEvent_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            var topicEndpoint = Configuration.GetValue<string>("Arcus:EventGrid:TopicEndpoint");
            var endpointKey = Configuration.GetValue<string>("Arcus:EventGrid:EndpointKey");
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var eventId = Guid.NewGuid().ToString();
            var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);

            // Act
            await EventGridPublisherBuilder
                .ForTopic(topicEndpoint)
                .UsingAuthenticationKey(endpointKey)
                .Build()
                .Publish(@event);

            TracePublishedEvent(eventId, @event);

            // Assert
            var receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(eventId);
            AssertReceivedEvent(eventId, @event.EventType, eventSubject, licensePlate, receivedEvent);
        }

        [Fact]
        public async Task PublishMultipleEvents_WithBuilder_ValidParameters_SucceedsWithRetryCount()
        {
            // Arrange
            var topicEndpoint = Configuration.GetValue<string>("Arcus:EventGrid:TopicEndpoint");
            var endpointKey = Configuration.GetValue<string>("Arcus:EventGrid:EndpointKey");
            const string eventSubject = "integration-test";
            const string licensePlate = "1-TOM-337";
            var firstEventId = Guid.NewGuid().ToString();
            var firstEvent = new NewCarRegistered(firstEventId, eventSubject, licensePlate);
            var secondEventId = Guid.NewGuid().ToString();
            var secondEvent = new NewCarRegistered(secondEventId, eventSubject, licensePlate);
            var events = new List<NewCarRegistered>
            {
                firstEvent, secondEvent
            };

            // Act
            await EventGridPublisherBuilder
                .ForTopic(topicEndpoint)
                .UsingAuthenticationKey(endpointKey)
                .Build()
                .PublishMany(events);

            TracePublishedEvent(firstEventId, events);
            TracePublishedEvent(secondEventId, events);

            // Assert
            var firstReceivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(firstEventId);
            AssertReceivedEvent(firstEventId, firstEvent.EventType, eventSubject, licensePlate, firstReceivedEvent);
            var secondReceivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(secondEventId);
            AssertReceivedEvent(secondEventId, secondEvent.EventType, eventSubject, licensePlate, secondReceivedEvent);
        }

        [Fact]
        public async Task PublishMultipleEvents_WithBuilder_ValidParameters_SucceedsWithTimeout()
        {
            // Arrange
            var topicEndpoint = Configuration.GetValue<string>("Arcus:EventGrid:TopicEndpoint");
            var endpointKey = Configuration.GetValue<string>("Arcus:EventGrid:EndpointKey");
            var events = 
                Enumerable
                    .Repeat<Func<Guid>>(Guid.NewGuid, 2)
                    .Select(f => new NewCarRegistered(
                        f().ToString(), 
                        subject: "integration-test", 
                        licensePlate: "1-TOM-337"));
            // Act
            await EventGridPublisherBuilder
                  .ForTopic(topicEndpoint)
                  .UsingAuthenticationKey(endpointKey)
                  .Build()
                  .PublishMany(events);

            // Assert
            Assert.All(
                events,
                e =>
                {
                    TracePublishedEvent(e.Id, events);
                    string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(e.Id, TimeSpan.FromMinutes(1));
                    AssertReceivedEvent(e.Id, e.EventType, e.Subject, e.Data.LicensePlate, receivedEvent);
                });
        }

        private static void AssertReceivedEvent(string eventId, string eventType, string eventSubject, string licensePlate, string receivedEvent)
        {
            Assert.NotEqual(string.Empty, receivedEvent);

            EventGridMessage<NewCarRegistered> deserializedEventGridMessage = EventGridParser.Parse<NewCarRegistered>(receivedEvent);
            Assert.NotNull(deserializedEventGridMessage);
            Assert.NotEmpty(deserializedEventGridMessage.SessionId);
            Assert.NotNull(deserializedEventGridMessage.Events);

            NewCarRegistered deserializedEvent = Assert.Single(deserializedEventGridMessage.Events);
            Assert.NotNull(deserializedEvent);
            Assert.Equal(eventId, deserializedEvent.Id);
            Assert.Equal(eventSubject, deserializedEvent.Subject);
            Assert.Equal(eventType, deserializedEvent.EventType);

            Assert.NotNull(deserializedEvent.Data);
            Assert.Equal(licensePlate, deserializedEvent.Data.LicensePlate);
        }

        private void TracePublishedEvent(string eventId, object events)
        {
            _testLogger.LogInformation($"Event '{eventId}' published - {JsonConvert.SerializeObject(events)}");
        }
    }
}