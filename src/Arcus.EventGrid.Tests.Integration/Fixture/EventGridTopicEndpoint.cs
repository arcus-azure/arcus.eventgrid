using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing;
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
        private readonly EventSchema _eventSchema;
        private readonly TestConfig _configuration;

        private EventGridTopicEndpoint(
            EventSchema eventSchema,
            ServiceBusEventConsumerHost serviceBusEventConsumerHost,
            TestConfig config)
        {
            Guard.NotNull(serviceBusEventConsumerHost, nameof(serviceBusEventConsumerHost));
            Guard.NotNull(config, nameof(config));

            _eventSchema = eventSchema;
            _configuration = config;
            ServiceBusEventConsumerHost = serviceBusEventConsumerHost;
        }

        /// <summary>
        /// Gets the consumer host on the current topic endpoint.
        /// </summary>
        public ServiceBusEventConsumerHost ServiceBusEventConsumerHost { get; }

        /// <summary>
        /// Creates a <see cref="EventGridTopicEndpoint"/> implementation that uses CloudEvent's as input event schema.
        /// </summary>
        /// <param name="config">The configuration used to build the endpoint.</param>
        /// <param name="testOutput">The test logger to write diagnostic messages during the availability of the endpoint.</param>
        public static async Task<EventGridTopicEndpoint> CreateForCloudEventAsync(TestConfig config, ITestOutputHelper testOutput)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(testOutput, nameof(testOutput));

            EventGridTopicEndpoint endpoint = await CreateAsync(EventSchema.CloudEvent, config, testOutput);
            return endpoint;
        }

        /// <summary>
        /// Creates a <see cref="EventGridTopicEndpoint"/> implementation that uses EventGridEvent's as input event schema.
        /// </summary>
        /// <param name="config">The configuration the build the endpoint.</param>
        /// <param name="testOutput">The test logger to write diagnostic messages during the availability of the endpoint.</param>
        public static async Task<EventGridTopicEndpoint> CreateForEventGridEventAsync(TestConfig config, ITestOutputHelper testOutput)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(testOutput, nameof(testOutput));

            EventGridTopicEndpoint endpoint = await CreateAsync(EventSchema.EventGrid, config, testOutput);
            return endpoint;
        }

        private static async Task<EventGridTopicEndpoint> CreateAsync(EventSchema type, TestConfig config, ITestOutputHelper testOutput)
        {
            ServiceBusEventConsumerHost serviceBusEventConsumerHost =
                await CreateServiceBusEventConsumerHostAsync(
                    config.GetServiceBusTopicName(type),
                    config.GetServiceBusConnectionString(),
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