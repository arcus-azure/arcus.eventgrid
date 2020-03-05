using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Contracts.Interfaces;
using Arcus.EventGrid.Publishing.Interfaces;
using CloudNative.CloudEvents;
using Flurl.Http;
using GuardNet;
using Newtonsoft.Json;
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

        private static readonly JsonEventFormatter JsonEventFormatter = new JsonEventFormatter();

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
        ///     Gets the url of the custom Event Grid topic.
        /// </summary>
        public string TopicEndpoint { get; }

        /// <summary>
        ///     Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        public async Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody)
        {
            await PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject: "/");
        }

        /// <summary>
        ///     Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The subject of the event.</param>
        public async Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody, string eventSubject)
        {
            await PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion: "1.0", eventTime: DateTimeOffset.UtcNow);
        }

        /// <summary>
        ///     Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The subject of the event.</param>
        /// <param name="dataVersion">The data version of the event body.</param>
        /// <param name="eventTime">The time when the event occured.</param>
        public async Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody, string eventSubject, string dataVersion, DateTimeOffset eventTime)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId), "No event id was specified");
            Guard.NotNullOrWhitespace(eventType, nameof(eventType), "No event type was specified");
            Guard.NotNullOrWhitespace(eventBody, nameof(eventBody), "No event body was specified");
            Guard.NotNullOrWhitespace(dataVersion, nameof(dataVersion), "No data version body was specified");
            Guard.For<ArgumentException>(() => eventBody.IsValidJson() == false, "The event body is not a valid JSON payload");

            var rawEvent = new RawEvent(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime);
            await PublishAsync(rawEvent);
        }

        /// <summary>
        ///     Publish a raw JSON payload as CloudEvent event.
        /// </summary>
        /// <param name="specVersion">The version of the CloudEvents specification which the event uses.</param>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="source">The source that identifies the context in which an event happened.</param>
        /// <param name="eventBody">The body of the event.</param>
        public async Task PublishRawCloudEventAsync(
            CloudEventsSpecVersion specVersion,
            string eventId,
            string eventType,
            Uri source,
            string eventBody)
        {
            await PublishRawCloudEventAsync(
                specVersion,
                eventId,
                eventType,
                source,
                eventBody: eventBody,
                eventSubject: "/");
        }

        /// <summary>
        ///     Publish a raw JSON payload as CloudEvent event.
        /// </summary>
        /// <param name="specVersion">The version of the CloudEvents specification which the event uses.</param>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="source">The source that identifies the context in which an event happened.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The value that describes the subject of the event in the context of the event producer.</param>
        public async Task PublishRawCloudEventAsync(
            CloudEventsSpecVersion specVersion,
            string eventId,
            string eventType,
            Uri source,
            string eventBody,
            string eventSubject)
        {
            await PublishRawCloudEventAsync(
                specVersion,
                eventId,
                eventType,
                source,
                eventBody: eventBody,
                eventSubject: eventSubject,
                eventTime: DateTimeOffset.UtcNow);
        }

        /// <summary>
        ///     Publish a raw JSON payload as CloudEvent event.
        /// </summary>
        /// <param name="specVersion">The version of the CloudEvents specification which the event uses.</param>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="source">The source that identifies the context in which an event happened.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The value that describes the subject of the event in the context of the event producer.</param>
        /// <param name="eventTime">The timestamp of when the occurrence happened.</param>
        public async Task PublishRawCloudEventAsync(
            CloudEventsSpecVersion specVersion,
            string eventId,
            string eventType,
            Uri source,
            string eventBody,
            string eventSubject,
            DateTimeOffset eventTime)
        {
            var cloudEvent = new CloudEvent(specVersion, eventType, source, id: eventId, time: eventTime.DateTime)
            {
                Subject = eventSubject,
                Data = eventBody,
                DataContentType = new ContentType("application/json")
            };

            await PublishAsync(cloudEvent);
        }

        /// <summary>
        ///     Publish an event grid message as CloudEvent.
        /// </summary>
        /// <param name="cloudEvent">The event to publish.</param>
        public async Task PublishAsync(CloudEvent cloudEvent)
        {
            Guard.NotNull(cloudEvent, nameof(cloudEvent));

            var content = new CloudEventContent(cloudEvent, ContentMode.Structured, JsonEventFormatter);
            await PublishContentToTopicAsync(content);
        }

        /// <summary>
        ///     Publish many event grid messages as CloudEvents.
        /// </summary>
        /// <param name="events">The events to publish.</param>
        public async Task PublishManyAsync(IEnumerable<CloudEvent> events)
        {
            Guard.NotNull(events, nameof(events), "No events was specified");
            Guard.For<ArgumentException>(() => !events.Any(), "No events were specified");
            Guard.For<ArgumentException>(() => events.Any(@event => @event is null), "Some events are 'null'");

            var content = new CloudEventBatchContent(events, ContentMode.Structured, JsonEventFormatter);
            await PublishContentToTopicAsync(content);
        }

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <typeparam name="TEvent">The type of the specific EventData.</typeparam>
        /// <param name="event">The event to publish.</param>
        public async Task PublishAsync<TEvent>(TEvent @event)
            where TEvent : class, IEvent
        {
            Guard.NotNull(@event, nameof(@event), "No event was specified");

            IEnumerable<TEvent> eventList = new List<TEvent>
            {
                @event
            };

            await PublishManyAsync(eventList);
        }

        /// <summary>
        ///     Publish an event grid message.
        /// </summary>
        /// <typeparam name="TEvent">The type of the specific EventData.</typeparam>
        /// <param name="events">The events to publish.</param>
        public async Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events)
            where TEvent : class, IEvent
        {
            Guard.NotNull(events, nameof(events), "No events was specified");
            Guard.For<ArgumentException>(() => !events.Any(), "No events were specified");
            Guard.For<ArgumentException>(() => events.Any(@event => @event is null), "Some events are 'null'");

            HttpContent content = CreateSerializedJsonHttpContent(events);
            await PublishContentToTopicAsync(content);
        }

        private static HttpContent CreateSerializedJsonHttpContent<TEvent>(IEnumerable<TEvent> events) where TEvent : class, IEvent
        {
            string json = JsonConvert.SerializeObject(events.ToArray());
            var content = new StringContent(json);

            return content;
        }

        private async Task PublishContentToTopicAsync(HttpContent content)
        {
            using (HttpResponseMessage response = 
                await _resilientPolicy.ExecuteAsync(() => SendAuthorizedHttpPostRequestAsync(content)))
            {
                if (!response.IsSuccessStatusCode)
                {
                    await ThrowApplicationExceptionAsync(response);
                }
            }
        }

        private async Task<HttpResponseMessage> SendAuthorizedHttpPostRequestAsync(HttpContent content)
        {
            IFlurlRequest authorizedRequest = 
                TopicEndpoint.WithHeader(name: "aeg-sas-key", value: _authenticationKey);
            
            HttpResponseMessage response = await authorizedRequest.SendAsync(HttpMethod.Post, content);
            return response;
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