using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.EventGrid.Tests.Integration.Logging;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Fixture 
{
    /// <summary>
    /// Represents an EventGrid topic endpoint that can be interacted with by publishing events.
    /// </summary>
    public class EventGridTopicEndpoint
    {
        private readonly EventGridEndpointType _endpointType;
        private readonly TestConfig _configuration;

        private EventGridTopicEndpoint(
            EventGridEndpointType endpointType,
            ServiceBusEventConsumerHost serviceBusEventConsumerHost,
            TestConfig config)
        {
            Guard.NotNull(serviceBusEventConsumerHost, nameof(serviceBusEventConsumerHost));
            Guard.NotNull(config, nameof(config));

            _endpointType = endpointType;
            _configuration = config;
            ServiceBusEventConsumerHost = serviceBusEventConsumerHost;
        }

        /// <summary>
        /// Gets the consumer host on the current topic endpoint.
        /// </summary>
        public ServiceBusEventConsumerHost ServiceBusEventConsumerHost { get; }

        /// <summary>
        /// Builds a <see cref="IEventGridPublisher"/> implementation that interacts with this endpoint.
        /// </summary>
        public IEventGridPublisher BuildPublisher()
        {
            string topicEndpoint = _configuration.GetEventGridTopicEndpoint(_endpointType);
            string endpointKey = _configuration.GetEventGridEndpointKey(_endpointType);
            
            IEventGridPublisher publisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(endpointKey)
                    .Build();

            return publisher;
        }

        /// <summary>
        /// Creates a <see cref="EventGridTopicEndpoint"/> implementation that uses CloudEvent's as input event schema.
        /// </summary>
        /// <param name="testOutput">The test logger to write diagnostic messages during the availability of the endpoint.</param>
        public static async Task<EventGridTopicEndpoint> CreateForCloudEventAsync(ITestOutputHelper testOutput)
        {
            Guard.NotNull(testOutput, nameof(testOutput));

            EventGridTopicEndpoint endpoint = await CreateAsync(EventGridEndpointType.CloudEvent, testOutput);
            return endpoint;
        }

        /// <summary>
        /// Creates a <see cref="EventGridTopicEndpoint"/> implementation that uses EventGridEvent's as input event schema.
        /// </summary>
        /// <param name="testOutput">The test logger to write diagnostic messages during the availability of the endpoint.</param>
        public static async Task<EventGridTopicEndpoint> CreateForEventGridEventAsync(ITestOutputHelper testOutput)
        {
            Guard.NotNull(testOutput, nameof(testOutput));

            EventGridTopicEndpoint endpoint = await CreateAsync(EventGridEndpointType.EventGridEvent, testOutput);
            return endpoint;
        }

        private static async Task<EventGridTopicEndpoint> CreateAsync(EventGridEndpointType type, ITestOutputHelper testOutput)
        {
            var config = TestConfig.Create();
            ServiceBusEventConsumerHost serviceBusEventConsumerHost =
                await CreateServiceBusEventConsumerHostAsync(
                    config.GetServiceBusTopicName(type),
                    config.GetServiceBusConnectionString(type),
                    testOutput);

            return new EventGridTopicEndpoint(type, serviceBusEventConsumerHost, config);
        }

        private static async Task<ServiceBusEventConsumerHost> CreateServiceBusEventConsumerHostAsync(string topicName, string connectionString, ITestOutputHelper testOutput)
        {
            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);
            var testLogger = new XunitTestLogger(testOutput);
            
            var serviceBusEventGridEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, testLogger);
            return serviceBusEventGridEventConsumerHost;
        }

        /// <summary>
        /// Releases all unmanaged resources asynchronously.
        /// </summary>
        public async ValueTask StopAsync()
        {
            await ServiceBusEventConsumerHost.StopAsync();
        }
    }
}