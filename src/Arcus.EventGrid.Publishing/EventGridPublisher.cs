using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts.Interfaces;
using Arcus.EventGrid.Publishing.Interfaces;
using Flurl.Http;
using GuardNet;
using Polly;

namespace Arcus.EventGrid.Publishing
{
    /// <summary>
    ///     Event Grid publisher can be used to publish events to a custom Event Grid topic
    /// </summary>
    public class EventGridPublisher : IEventGridPublisher
    {
        private readonly Policy _resilientPolicy;
        private readonly string _authenticationKey;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="topicEndpoint">Url of the custom Event Grid topic</param>
        /// <param name="authenticationKey">Authentication key for the custom Event Grid topic</param>
        internal EventGridPublisher(string topicEndpoint, string authenticationKey)
            : this(topicEndpoint, authenticationKey, Policy.NoOpAsync())
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="topicEndpoint">Url of the custom Event Grid topic</param>
        /// <param name="authenticationKey">Authentication key for the custom Event Grid topic</param>
        /// <param name="resilientPolicy">The policy to use making the publishing resilient.</param>
        internal EventGridPublisher(string topicEndpoint, string authenticationKey, Policy resilientPolicy)
        {
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "The topic endpoint must not be empty and is required");
            Guard.NotNullOrWhitespace(authenticationKey, nameof(authenticationKey), "The authentication key must not be empty and is required");
            Guard.NotNull(resilientPolicy, nameof(resilientPolicy), "The resilient policy is required with this construction, otherwise use other constructor");

            TopicEndpoint = topicEndpoint;

            _authenticationKey = authenticationKey;
            _resilientPolicy = resilientPolicy;
        }

        /// <summary>
        ///     Url of the custom Event Grid topic
        /// </summary>
        public string TopicEndpoint { get; }

        /// <summary>
        ///     Publish an event grid message to the configured Event Grid topic
        /// </summary>
        /// <typeparam name="TEvent">Type of the specific EventData</typeparam>
        /// <param name="event">Event to publish</param>
        public async Task Publish<TEvent>(TEvent @event)
            where TEvent : class, IEvent, new()
        {
            Guard.NotNull(@event, nameof(@event), "No event was specified");

            List<TEvent> eventList = new List<TEvent>
            {
                @event
            };

            await PublishEventToTopic(eventList);
        }

        /// <summary>
        ///     Publish an event grid message to the configured Event Grid topic
        /// </summary>
        /// <typeparam name="TEvent">Type of the specific EventData</typeparam>
        /// <param name="events">Events to publish</param>
        public async Task Publish<TEvent>(List<TEvent> events)
            where TEvent : class, IEvent, new()
        {
            Guard.NotNull(events, nameof(events), "No events was specified");
            Guard.For(() => events.Any() == false, new ArgumentException("No events were specified", nameof(events)));

            await PublishEventToTopic(events);
        }

        private async Task PublishEventToTopic<TEvent>(List<TEvent> eventList) where TEvent : class, IEvent, new()
        {
            // Calling HTTP endpoint
            var response = await TopicEndpoint
                .WithHeader(name: "aeg-sas-key", value: AuthenticationKey)
                .PostJsonAsync(eventList);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowApplicationExceptionAsync(response);
            }
        }

        private async Task ThrowApplicationExceptionAsync(HttpResponseMessage response)
        {
            var rawResponse = string.Empty;

            try
            {
                rawResponse = await response.Content.ReadAsStringAsync();
            }
            finally
            {
                // Throw custom exception in case of failure
                throw new ApplicationException($"Event grid publishing failed with status {response.StatusCode} and content {rawResponse}");
            }
        }
    }
}