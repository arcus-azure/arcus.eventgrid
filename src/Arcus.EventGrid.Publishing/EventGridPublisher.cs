using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Contracts.Interfaces;
using Arcus.EventGrid.Publishing.Interfaces;
using CloudNative.CloudEvents;
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
        /// <exception cref="UriFormatException">The topic endpoint must be a HTTP endpoint.</exception>
        internal EventGridPublisher(Uri topicEndpoint, string authenticationKey)
            : this(topicEndpoint, authenticationKey, Policy.NoOpAsync())
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="topicEndpoint">Url of the custom Event Grid topic</param>
        /// <param name="authenticationKey">Authentication key for the custom Event Grid topic</param>
        /// <param name="resilientPolicy">The policy to use making the publishing resilient.</param>
        /// <exception cref="UriFormatException">The topic endpoint must be a HTTP endpoint.</exception>
        internal EventGridPublisher(Uri topicEndpoint, string authenticationKey, Policy resilientPolicy)
        {
            Guard.NotNull(topicEndpoint, nameof(topicEndpoint), "The topic endpoint must be specified");
            Guard.NotNullOrWhitespace(authenticationKey, nameof(authenticationKey), "The authentication key must not be empty and is required");
            Guard.NotNull(resilientPolicy, nameof(resilientPolicy), "The resilient policy is required with this construction, otherwise use other constructor");
            Guard.For<UriFormatException>(
                () => topicEndpoint.Scheme != Uri.UriSchemeHttp
                      && topicEndpoint.Scheme != Uri.UriSchemeHttps,
                $"The topic endpoint must be and HTTP or HTTPS endpoint but is: {topicEndpoint.Scheme}");

            TopicEndpoint = topicEndpoint.OriginalString;

            _authenticationKey = authenticationKey;
            _resilientPolicy = resilientPolicy;
        }

        /// <summary>
        ///     Url of the custom Event Grid topic
        /// </summary>
        public string TopicEndpoint { get; }

        /// <summary>
        ///     Publish a raw JSON payload as event
        /// </summary>
        /// <param name="eventId">Id of the event</param>
        /// <param name="eventType">Type of the event</param>
        /// <param name="eventBody">Body of the event</param>
        /// <param name="eventSchema">The schema in which the event should be published.</param>
        public async Task PublishRawAsync(string eventId, string eventType, string eventBody, EventSchema eventSchema = EventSchema.EventGrid)
        {
            await PublishRawAsync(eventId, eventType, eventBody, eventSubject: "/", dataVersion: "1.0", eventTime: DateTimeOffset.UtcNow, eventSchema);
        }

        /// <summary>
        ///     Publish a raw JSON payload as event
        /// </summary>
        /// <param name="eventId">Id of the event</param>
        /// <param name="eventType">Type of the event</param>
        /// <param name="eventBody">Body of the event</param>
        /// <param name="eventSubject">Subject of the event</param>
        /// <param name="dataVersion">Data version of the event body</param>
        /// <param name="eventTime">Time when the event occured</param>
        /// <param name="eventSchema">The schema in which the event should be published.</param>
        public async Task PublishRawAsync(string eventId, string eventType, string eventBody, string eventSubject, string dataVersion, DateTimeOffset eventTime, EventSchema eventSchema = EventSchema.EventGrid)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId), "No event id was specified");
            Guard.NotNullOrWhitespace(eventType, nameof(eventType), "No event type was specified");
            Guard.NotNullOrWhitespace(eventBody, nameof(eventBody), "No event body was specified");
            Guard.NotNullOrWhitespace(dataVersion, nameof(dataVersion), "No data version body was specified");
            Guard.For<ArgumentException>(() => eventBody.IsValidJson() == false, "The event body is not a valid JSON payload");

            var rawEvent = new RawEvent(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime);

            await PublishRawAsync(rawEvent, eventSchema);
        }

        /// <summary>
        ///     Publish a raw JSON payload as event
        /// </summary>
        /// <param name="rawEvent">The event to publish</param>
        /// <param name="eventSchema">The schema in which the <paramref name="rawEvent"/> should be published.</param>
        public async Task PublishRawAsync(RawEvent rawEvent, EventSchema eventSchema = EventSchema.EventGrid)
        {
            Guard.NotNull(rawEvent, nameof(rawEvent), "No event was specified");

            IEnumerable<RawEvent> eventList = new[] { rawEvent };

            await PublishEventToTopicAsync(eventList, eventSchema);
        }

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <param name="cloudEvent">Event to publish</param>
        public async Task PublishAsync(CloudEvent cloudEvent)
        {
            Guard.NotNull(cloudEvent, nameof(cloudEvent));

            IEnumerable<CloudEvent> eventList = new List<CloudEvent>
            {
                cloudEvent
            };

            await PublishEventToTopicAsync(eventList, EventSchema.CloudEvent);
        }

        /// <summary>
        ///     Publish a many raw JSON payload as events
        /// </summary>
        /// <param name="rawEvents">The events to publish.</param>
        /// <param name="eventSchema">The schema in which the <paramref name="rawEvents"/> should be published.</param>
        public async Task PublishManyRawAsync(IEnumerable<RawEvent> rawEvents, EventSchema eventSchema = EventSchema.EventGrid)
        {
            Guard.NotNull(rawEvents, nameof(rawEvents), "No raw events were specified");
            Guard.For<ArgumentException>(() => !rawEvents.Any(), "No raw events were specified");
            Guard.For<ArgumentException>(() => rawEvents.Any(rawEvent => rawEvent is null), "Some raw events are 'null'");

            await PublishEventToTopicAsync(rawEvents, eventSchema);
        }

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <typeparam name="TEvent">Type of the specific EventData</typeparam>
        /// <param name="event">Event to publish</param>
        /// <param name="eventSchema">The schema in which the <paramref name="event"/> should be published.</param>
        public async Task PublishAsync<TEvent>(TEvent @event, EventSchema eventSchema = EventSchema.EventGrid)
            where TEvent : class, IEvent
        {
            Guard.NotNull(@event, nameof(@event), "No event was specified");

            IEnumerable<TEvent> eventList = new List<TEvent>
            {
                @event
            };

            await PublishEventToTopicAsync(eventList, eventSchema);
        }

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <typeparam name="TEvent">Type of the specific EventData</typeparam>
        /// <param name="events">Events to publish</param>
        /// <param name="eventSchema">The schema in which the <paramref name="events"/> should be published.</param>
        public async Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, EventSchema eventSchema = EventSchema.EventGrid)
            where TEvent : class, IEvent
        {
            Guard.NotNull(events, nameof(events), "No events was specified");
            Guard.For<ArgumentException>(() => !events.Any(), "No events were specified");
            Guard.For<ArgumentException>(() => events.Any(@event => @event is null), "Some events are 'null'");

            await PublishEventToTopicAsync(events, eventSchema);
        }

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <param name="events">Events to publish</param>
        public async Task PublishManyAsync(IEnumerable<CloudEvent> events)
        {
            Guard.NotNull(events, nameof(events));

            await PublishEventToTopicAsync(events, EventSchema.CloudEvent);
        }

        private async Task PublishEventToTopicAsync<TEvent>(IEnumerable<TEvent> eventList, EventSchema eventSchema) where TEvent : class
        {
            var response = await _resilientPolicy.ExecuteAsync(() => SendAuthorizedHttpPostRequestAsync(eventList, eventSchema));

            if (!response.IsSuccessStatusCode)
            {
                await ThrowApplicationExceptionAsync(response);
            }
        }

        private async Task<HttpResponseMessage> SendAuthorizedHttpPostRequestAsync<TEvent>(IEnumerable<TEvent> events, EventSchema eventSchema) where TEvent : class
        {
            IFlurlRequest authorizedRequest = 
                TopicEndpoint.WithHeader(name: "aeg-sas-key", value: _authenticationKey);

            switch (eventSchema)
            {
                case EventSchema.EventGrid:
                    return await authorizedRequest.PostJsonAsync(events);
                case EventSchema.CloudEvent:
                    // TODO: until abstracted event is fixed.
                    if (typeof(TEvent) == typeof(CloudEvent))
                    {
                        IEnumerable<CloudEvent> cloudEvents = events.Cast<CloudEvent>();
                        HttpContent content = CreateCloudEventHttpContent(cloudEvents);
                        return await authorizedRequest.SendAsync(HttpMethod.Post, content);
                    }
                    else if (typeof(TEvent) == typeof(RawEvent))
                    {
                        IEnumerable<CloudEvent> cloudEvents = events.Cast<RawEvent>().Select(CreateCloudEventFromRawEvent);
                        HttpContent content = CreateCloudEventHttpContent(cloudEvents);
                        return await authorizedRequest.SendAsync(HttpMethod.Post, content);
                    }

                    throw new InvalidOperationException("Can't publish events as cloud events because the passed along events aren't cloud events");
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventSchema), eventSchema, "Unknown event schema");
            }
        }

        private static HttpContent CreateCloudEventHttpContent(IEnumerable<CloudEvent> cloudEvents)
        {
            if (cloudEvents.Count() == 1)
            {
                var content = new CloudEventContent(cloudEvents.First(), ContentMode.Binary, new JsonEventFormatter());
                return content;
            }
            else
            {
                var content = new CloudEventBatchContent(cloudEvents, ContentMode.Binary, new JsonEventFormatter());
                return content;
            }
        }

        private static CloudEvent CreateCloudEventFromRawEvent(RawEvent rawEvent)
        {
            // TODO: what should the source be?
            var source = new Uri("http://source");
            var cloudEvent = new CloudEvent(rawEvent.EventType, source, rawEvent.Id, rawEvent.EventTime)
            {
                Data = rawEvent.Data,
                Subject = rawEvent.Subject,
                DataContentType = new ContentType("application/json")
            };

            return cloudEvent;
        }

        private static async Task ThrowApplicationExceptionAsync(HttpResponseMessage response)
        {
            var rawResponse = String.Empty;

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