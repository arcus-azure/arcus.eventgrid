using System;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Fixture
{
    /// <summary>
    /// Factory to create <see cref="IEventGridPublisher"/> implementations.
    /// </summary>
    public class EventPublisherFactory
    {
        /// <summary>
        /// Creates an <see cref="IEventGridPublisher"/> implementation that can publish events to an CloudEvents endpoint.
        /// </summary>
        /// <param name="configuration">The instance to retrieve the required values to configure the publisher.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static IEventGridPublisher CreateCloudEventPublisher(TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the necessary Azure Event Grid topic authentication");

            IEventGridPublisher publisher = CreateEventPublisher(EventSchema.CloudEvent, configuration);
            return publisher;
        }
        
        /// <summary>
        /// Creates an <see cref="IEventGridPublisher"/> implementation that can publish events to an CloudEvents endpoint.
        /// </summary>
        /// <param name="configuration">The instance to retrieve the required values to configure the publisher.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid resource.</param>
        /// <param name="configureOptions">The additional function to configure optional settings on the <see cref="IEventGridPublisher"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static IEventGridPublisher CreateCloudEventPublisher(TestConfig configuration, ILogger logger, Action<EventGridPublisherOptions> configureOptions)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the necessary Azure Event Grid topic authentication");

            IEventGridPublisher publisher = CreateEventPublisher(EventSchema.CloudEvent, configuration, logger, configureOptions);
            return publisher;
        }

        /// <summary>
        /// Creates an <see cref="IEventGridPublisher"/> implementation that can publish events to an EventGrid event endpoint.
        /// </summary>
        /// <param name="configuration">The instance to retrieve the required values to configure the publisher.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static IEventGridPublisher CreateEventGridEventPublisher(TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the necessary Azure Event Grid topic authentication");

            IEventGridPublisher publisher = CreateEventPublisher(EventSchema.EventGrid, configuration);
            return publisher;
        }
        
        /// <summary>
        /// Creates an <see cref="IEventGridPublisher"/> implementation that can publish events to an EventGrid event endpoint.
        /// </summary>
        /// <param name="configuration">The instance to retrieve the required values to configure the publisher.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid resource.</param>
        /// <param name="configureOptions">The additional function to configure optional settings on the <see cref="IEventGridPublisher"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static IEventGridPublisher CreateEventGridEventPublisher(TestConfig configuration, ILogger logger, Action<EventGridPublisherOptions> configureOptions)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the necessary Azure Event Grid topic authentication");

            IEventGridPublisher publisher = CreateEventPublisher(EventSchema.EventGrid, configuration, logger, configureOptions);
            return publisher;
        }

        /// <summary>
        /// Creates an <see cref="IEventGridPublisher"/> implementation that can publish events to an endpoint corresponding with the given <paramref name="eventSchema"/>.
        /// </summary>
        /// <param name="eventSchema">The schema that corresponds to the target endpoint to which the publisher will publish events.</param>
        /// <param name="configuration">The instance to retrieve the required values to configure the publisher.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static IEventGridPublisher CreateEventPublisher(EventSchema eventSchema, TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the necessary Azure Event Grid topic authentication");

            IEventGridPublisher publisher = CreateEventPublisher(eventSchema, configuration, NullLogger.Instance, configureOptions: null);
            return publisher;
        }

        /// <summary>
        /// Creates an <see cref="IEventGridPublisher"/> implementation that can publish events to an endpoint corresponding with the given <paramref name="eventSchema"/>.
        /// </summary>
        /// <param name="eventSchema">The schema that corresponds to the target endpoint to which the publisher will publish events.</param>
        /// <param name="configuration">The instance to retrieve the required values to configure the publisher.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid resource.</param>
        /// <param name="configureOptions">The additional function to configure optional settings on the <see cref="IEventGridPublisher"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static IEventGridPublisher CreateEventPublisher(
            EventSchema eventSchema, 
            TestConfig configuration, 
            ILogger logger, 
            Action<EventGridPublisherOptions> configureOptions)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the necessary Azure Event Grid topic authentication");
            
            string topicEndpoint = configuration.GetEventGridTopicEndpoint(eventSchema);
            string endpointKey = configuration.GetEventGridEndpointKey(eventSchema);
            
            IEventGridPublisher publisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint, logger, configureOptions)
                    .UsingAuthenticationKey(endpointKey)
                    .Build();

            return publisher;
        }
    }
}
