using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Integration.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait(name: "Category", value: "Integration")]
    public class EventPublishingTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _testOutput;

        private HybridConnectionHost _hybridConnectionHost;

        public EventPublishingTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;

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

            _hybridConnectionHost = await HybridConnectionHost.Start(relayNamespace, hybridConnectionName, accessPolicyName, accessPolicyKey);
        }

        [Fact]
        public async Task Publish_ValidParameters_Succeeds()
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
            var eventGridPublisher = EventGridPublisher.Create(topicEndpoint, endpointKey);
            await eventGridPublisher.Publish(eventSubject, eventType, events, eventId);
            _testOutput.WriteLine($"Event '{eventId}' published");

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