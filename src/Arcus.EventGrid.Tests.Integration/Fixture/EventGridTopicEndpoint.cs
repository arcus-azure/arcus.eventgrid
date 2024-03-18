using System;
using System.Threading.Tasks;
using Arcus.EventGrid.Testing.Infrastructure.Hosts;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Integration.Publishing.Fixture;
using Arcus.Testing.Logging;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Fixture 
{
    /// <summary>
    /// Represents an EventGrid topic endpoint that can be interacted with by publishing events.
    /// </summary>
    public class EventGridTopicEndpoint : IAsyncDisposable
    {
        private readonly EventSchema _eventSchema;
        private readonly TestConfig _configuration;

        private EventGridTopicEndpoint(
            EventSchema eventSchema,
            EventConsumerHost serviceBusEventConsumerHost,
            TestConfig config)
        {
            Guard.NotNull(serviceBusEventConsumerHost, nameof(serviceBusEventConsumerHost));
            Guard.NotNull(config, nameof(config));

            _eventSchema = eventSchema;
            _configuration = config;

            ServiceBusEventConsumerHost = serviceBusEventConsumerHost;
            if (serviceBusEventConsumerHost is MockServiceBusEventConsumerHost consumerHost)
            {
                ConsumerHost = consumerHost;
            }
        }

        /// <summary>
        /// Gets the consumer host on the current topic endpoint.
        /// </summary>
        [Obsolete("Use the " + nameof(ConsumerHost) + " instead")]
        public EventConsumerHost ServiceBusEventConsumerHost { get; }

        /// <summary>
        /// Gets the consumer host on the current topic endpoint.
        /// </summary>
        public MockServiceBusEventConsumerHost ConsumerHost { get; }

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
        /// Creates a <see cref="EventGridTopicEndpoint"/> implementation that uses CloudEvent's as input event schema.
        /// </summary>
        /// <param name="config">The configuration used to build the endpoint.</param>
        /// <param name="testOutput">The test logger to write diagnostic messages during the availability of the endpoint.</param>
        public static async Task<EventGridTopicEndpoint> CreateForCloudEventAsync(
            TestConfig config, 
            ITestOutputHelper testOutput, 
            Action<MockServiceBusEventConsumerHostOptions> configureOptions)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(testOutput, nameof(testOutput));

            EventGridTopicEndpoint endpoint = await CreateAsync(EventSchema.CloudEvent, config, testOutput, configureOptions);
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

        /// <summary>
        /// Creates a <see cref="EventGridTopicEndpoint"/> implementation that uses EventGridEvent's as input event schema.
        /// </summary>
        /// <param name="config">The configuration the build the endpoint.</param>
        /// <param name="testOutput">The test logger to write diagnostic messages during the availability of the endpoint.</param>
        public static async Task<EventGridTopicEndpoint> CreateForEventGridEventAsync(
            TestConfig config, 
            ITestOutputHelper testOutput, 
            Action<MockServiceBusEventConsumerHostOptions> configureOptions)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(testOutput, nameof(testOutput));

            EventGridTopicEndpoint endpoint = await CreateAsync(EventSchema.EventGrid, config, testOutput, configureOptions);
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
            
            var serviceBusEventGridEventConsumerHost = 
                await Testing.Infrastructure.Hosts.ServiceBus.ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, testLogger);
            
            return serviceBusEventGridEventConsumerHost;
        }

        public static async Task<EventGridTopicEndpoint> CreateAsync(
            EventSchema type, 
            TestConfig config, 
            ITestOutputHelper testOutput,
            Action<MockServiceBusEventConsumerHostOptions> configureOptions)
        {
            MockServiceBusEventConsumerHost serviceBusEventConsumerHost =
                await CreateMockServiceBusEventConsumerHostAsync(
                    config.GetServiceBusTopicName(type),
                    config.GetServiceBusConnectionString(),
                    testOutput,
                    configureOptions);

            return new EventGridTopicEndpoint(type, serviceBusEventConsumerHost, config);
        }

        private static async Task<MockServiceBusEventConsumerHost> CreateMockServiceBusEventConsumerHostAsync(
            string topicName,
            string connectionString,
            ITestOutputHelper testOutput,
            Action<MockServiceBusEventConsumerHostOptions> configureOptions)
        {
            var serviceBusEventConsumerHostOptions = new MockServiceBusEventConsumerHostOptions(topicName, connectionString);
            configureOptions?.Invoke(serviceBusEventConsumerHostOptions);

            var testLogger = new XunitTestLogger(testOutput);
            
            var serviceBusEventGridEventConsumerHost = await MockServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, testLogger);
            return serviceBusEventGridEventConsumerHost;
        }

        /// <summary>
        /// Releases all unmanaged resources asynchronously.
        /// </summary>
        public async ValueTask StopAsync()
        {
            await ServiceBusEventConsumerHost.StopAsync();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await ServiceBusEventConsumerHost.StopAsync();
        }
    }
}