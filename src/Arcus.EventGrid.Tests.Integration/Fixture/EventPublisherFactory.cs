using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;

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
        public static IEventGridPublisher CreateCloudEventPublisher(TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration));

            IEventGridPublisher publisher = CreateEventPublisher(EventSchema.CloudEvent, configuration);
            return publisher;
        }

        /// <summary>
        /// Creates an <see cref="IEventGridPublisher"/> implementation that can publish events to an EventGrid event endpoint.
        /// </summary>
        /// <param name="configuration">The instance to retrieve the required values to configure the publisher.</param>
        public static IEventGridPublisher CreateEventGridEventPublisher(TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration));

            IEventGridPublisher publisher = CreateEventPublisher(EventSchema.EventGrid, configuration);
            return publisher;
        }

        /// <summary>
        /// Creates an <see cref="IEventGridPublisher"/> implementation that can publish events to an endpoint corresponding with the given <paramref name="eventSchema"/>.
        /// </summary>
        /// <param name="eventSchema">The schema that corresponds to the target endpoint to which the publisher will publish events.</param>
        /// <param name="configuration">The instance to retrieve the required values to configure the publisher.</param>
        public static IEventGridPublisher CreateEventPublisher(EventSchema eventSchema, TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration));

            string topicEndpoint = configuration.GetEventGridTopicEndpoint(eventSchema);
            string endpointKey = configuration.GetEventGridEndpointKey(eventSchema);
            
            IEventGridPublisher publisher =
                EventGridPublisherBuilder
                    .ForTopic(topicEndpoint)
                    .UsingAuthenticationKey(endpointKey)
                    .Build();

            return publisher;
        }
    }
}
