using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Testing.Infrastructure.Hosts;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Integration.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait(name: "Category", value: "Integration")]
    public class EventPublishingTests : IAsyncLifetime
    {
        private readonly XunitTestLogger _testLogger;

        private HybridConnectionHost _hybridConnectionHost;

        public EventPublishingTests(ITestOutputHelper testOutput)
        {
            _testLogger = new XunitTestLogger(testOutput);

            Configuration = new ConfigurationBuilder()
                .AddJsonFile(path: "appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        }

        protected IConfiguration Configuration { get; }

        public async Task DisposeAsync()
        {
            await _hybridConnectionHost.Stop();
        }

        public async Task InitializeAsync()
        {
            var relayNamespace = Configuration.GetValue<string>("Arcus:HybridConnections:RelayNamespace");
            var hybridConnectionName = Configuration.GetValue<string>("Arcus:HybridConnections:Name");
            var accessPolicyName = Configuration.GetValue<string>("Arcus:HybridConnections:AccessPolicyName");
            var accessPolicyKey = Configuration.GetValue<string>("Arcus:HybridConnections:AccessPolicyKey");

            _hybridConnectionHost = await HybridConnectionHost.Start(relayNamespace, hybridConnectionName, accessPolicyName, accessPolicyKey, _testLogger);
        }

        // TODO: remove the raw-duplicate of the Integration Test after the obsolete creation of the 'EventGridPublisher' is removed
        [Fact]
        public async Task Publish_WithFactoryMethod_ValidParameters_Succeeds()
        {
            // Arrange
            var topicEndpoint = Configuration.GetValue<string>("Arcus:EventGrid:TopicEndpoint");
            var endpointKey = Configuration.GetValue<string>("Arcus:EventGrid:EndpointKey");
            const string eventSubject = "integration-test";
            const string eventType = "integration-test-event";
            const string licensePlate = "1-TOM-337";
            var eventId = Guid.NewGuid().ToString();
            var events = new List<NewCarRegisteredEvent>
            {
                new NewCarRegisteredEvent(licensePlate)
            };

            // Act
#pragma warning disable CS0618 // Member is obsolete
            await EventGridPublisher
                .Create(topicEndpoint, endpointKey)
#pragma warning restore CS0618 // Member is obsolete
                .Publish(eventSubject, eventType, events, eventId);

            _testLogger.LogInformation($"Event '{eventId}' published");

            // Assert
            var receivedEvent = _hybridConnectionHost.GetReceivedEvent(eventId);
            Assert.NotEmpty(receivedEvent);

            EventGridMessage<NewCarRegisteredEvent> deserializedEventGridMessage = EventGridMessage<NewCarRegisteredEvent>.Parse(receivedEvent);
            Assert.NotNull(deserializedEventGridMessage);
            Assert.NotEmpty(deserializedEventGridMessage.SessionId);
            Assert.NotNull(deserializedEventGridMessage.Events);
            Assert.Single(deserializedEventGridMessage.Events);
            Event<NewCarRegisteredEvent> deserializedEvent = deserializedEventGridMessage.Events.First();
            Assert.Equal(deserializedEvent.Id, eventId);
            Assert.Equal(deserializedEvent.Subject, eventSubject);
            Assert.Equal(deserializedEvent.EventType, eventType);
            Assert.NotNull(deserializedEvent.Data);
            Assert.Equal(deserializedEvent.Data.LicensePlate, licensePlate);
        }

        [Fact]
        public async Task Publish_WithBuilder_ValidParameters_Succeeds()
        {
            // Arrange
            var topicEndpoint = Configuration.GetValue<string>("Arcus:EventGrid:TopicEndpoint");
            var endpointKey = Configuration.GetValue<string>("Arcus:EventGrid:EndpointKey");
            const string eventSubject = "integration-test";
            const string eventType = "integration-test-event";
            const string licensePlate = "1-TOM-337";
            var eventId = Guid.NewGuid().ToString();
            var events = new List<NewCarRegisteredEvent>
            {
                new NewCarRegisteredEvent(licensePlate)
            };

            // Act
            await EventGridPublisherBuilder
                .ForTopic(topicEndpoint)
                .UsingAuthenticationKey(endpointKey)
                .Build()
                .Publish(eventSubject, eventType, events, eventId);

            _testLogger.LogInformation($"Event '{eventId}' published");

            // Assert
            var receivedEvent = _hybridConnectionHost.GetReceivedEvent(eventId);
            Assert.NotEmpty(receivedEvent);

            EventGridMessage<NewCarRegisteredEvent> deserializedEventGridMessage = EventGridMessage<NewCarRegisteredEvent>.Parse(receivedEvent);
            Assert.NotNull(deserializedEventGridMessage);
            Assert.NotEmpty(deserializedEventGridMessage.SessionId);
            Assert.NotNull(deserializedEventGridMessage.Events);
            Assert.Single(deserializedEventGridMessage.Events);
            Event<NewCarRegisteredEvent> deserializedEvent = deserializedEventGridMessage.Events.First();
            Assert.Equal(deserializedEvent.Id, eventId);
            Assert.Equal(deserializedEvent.Subject, eventSubject);
            Assert.Equal(deserializedEvent.EventType, eventType);
            Assert.NotNull(deserializedEvent.Data);
            Assert.Equal(deserializedEvent.Data.LicensePlate, licensePlate);
        }
    }
}