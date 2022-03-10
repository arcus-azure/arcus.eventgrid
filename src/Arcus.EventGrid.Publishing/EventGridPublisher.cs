using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Contracts.Interfaces;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Observability.Telemetry.Core;
using CloudNative.CloudEvents;
using Flurl.Http;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Polly;

namespace Arcus.EventGrid.Publishing
{
    /// <summary>
    /// Represents a Event Grid publisher that publishes events to a custom Azure Event Grid topic
    /// </summary>
    /// <seealso cref="IEventGridPublisher"/>
    public class EventGridPublisher : IEventGridPublisher
    {
        private readonly AsyncPolicy _resilientPolicy;
        private readonly string _authenticationKey;
        private readonly ILogger _logger;
        private readonly EventGridPublisherOptions _options;

        private static readonly JsonEventFormatter JsonEventFormatter = new JsonEventFormatter();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisher"/> class.
        /// </summary>
        /// <param name="topicEndpoint">The URL of custom Azure Event Grid topic.</param>
        /// <param name="authenticationKey">The authentication key for the custom Azure Event Grid topic.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid topic.</param>
        /// <param name="options">The optional settings on the <see cref="IEventGridPublisher"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="topicEndpoint"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="authenticationKey"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> is not a valid HTTP endpoint.</exception>
        internal EventGridPublisher(Uri topicEndpoint, string authenticationKey, ILogger logger, EventGridPublisherOptions options)
            : this(topicEndpoint, authenticationKey, Policy.NoOpAsync(), logger, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisher"/> class.
        /// </summary>
        /// <param name="topicEndpoint">The URL of custom Azure Event Grid topic.</param>
        /// <param name="authenticationKey">The authentication key for the custom Azure Event Grid topic.</param>
        /// <param name="resilientPolicy">The policy to use making the publishing resilient.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid topic</param>
        /// <param name="options">The optional settings on the <see cref="IEventGridPublisher"/>.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="topicEndpoint"/>, the <paramref name="resilientPolicy"/>, or the <paramref name="options"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="authenticationKey"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> is not a valid HTTP endpoint.</exception>
        internal EventGridPublisher(Uri topicEndpoint, string authenticationKey, AsyncPolicy resilientPolicy, ILogger logger, EventGridPublisherOptions options)
        {
            Guard.NotNull(topicEndpoint, nameof(topicEndpoint), "Requires an Azure Event Grid topic endpoint");
            Guard.NotNullOrWhitespace(authenticationKey, nameof(authenticationKey), "Requires an non-blank authentication key to authenticate to a Azure Event Grid topic");
            Guard.NotNull(resilientPolicy, nameof(resilientPolicy), "Requires a resilient policy this Azure Event Grid topic publisher, otherwise use other constructor");
            Guard.NotNull(options, nameof(options), "Requires a set of options to configure the Azure Event Grid publisher");
            Guard.For<UriFormatException>(
                () => topicEndpoint.Scheme != Uri.UriSchemeHttp
                      && topicEndpoint.Scheme != Uri.UriSchemeHttps,
                "Requires an Azure Event Grid topic endpoint with either a HTTP or HTTPS scheme");

            TopicEndpoint = topicEndpoint.OriginalString;

            _authenticationKey = authenticationKey;
            _resilientPolicy = resilientPolicy;
            _logger = logger ?? NullLogger.Instance;
            _options = options;
        }

        /// <summary>
        /// Gets the URL of the custom Azure Event Grid topic.
        /// </summary>
        public string TopicEndpoint { get; }

        /// <summary>
        /// Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventId"/>, the <paramref name="eventType"/>, or the <paramref name="eventBody"/> is blank;
        ///     or the <paramref name="eventBody"/> is not a valid JSON payload.
        /// </exception>
        public async Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId), "Requires a non-blank event ID to publish a raw event to Azure Event Grid");
            Guard.NotNullOrWhitespace(eventType, nameof(eventType), "Requires a non-blank event type to publish a raw event to Azure Event Grid");
            Guard.NotNullOrWhitespace(eventBody, nameof(eventBody), "Requires a non-blank event body to publish a raw event to Azure Event Grid");
            Guard.For(() => eventBody.IsValidJson() == false, new ArgumentException(
                "Requires a valid JSON raw event body payload to publish to Azure Event Grid", nameof(eventBody)));

            await PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject: "/");
        }

        /// <summary>
        /// Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The subject of the event.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventId"/>, the <paramref name="eventType"/>, or the <paramref name="eventBody"/> is blank;
        ///     or the <paramref name="eventBody"/> is not a valid JSON payload.
        /// </exception>
        public async Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody, string eventSubject)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId), "Requires a non-blank event ID to publish a raw event to Azure Event Grid");
            Guard.NotNullOrWhitespace(eventType, nameof(eventType), "Requires a non-blank event type to publish a raw event to Azure Event Grid");
            Guard.NotNullOrWhitespace(eventBody, nameof(eventBody), "Requires a non-blank event body to publish a raw event to Azure Event Grid");
            Guard.For(() => eventBody.IsValidJson() == false, new ArgumentException(
                "Requires a valid JSON raw event body payload to publish to Azure Event Grid", nameof(eventBody)));

            await PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion: "1.0", eventTime: DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The subject of the event.</param>
        /// <param name="dataVersion">The data version of the event body.</param>
        /// <param name="eventTime">The time when the event occurred.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventId"/>, the <paramref name="eventType"/>, the <paramref name="eventBody"/>, or the <paramref name="dataVersion"/> is blank;
        ///     or the <paramref name="eventBody"/> is not a valid JSON payload.
        /// </exception>
        public async Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody, string eventSubject, string dataVersion, DateTimeOffset eventTime)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId), "Requires a non-blank event ID to publish a raw event to Azure Event Grid");
            Guard.NotNullOrWhitespace(eventType, nameof(eventType), "Requires a non-blank event type to publish a raw event to Azure Event Grid");
            Guard.NotNullOrWhitespace(eventBody, nameof(eventBody), "Requires a non-blank event body to publish a raw event to Azure Event Grid");
            Guard.NotNullOrWhitespace(dataVersion, nameof(dataVersion), "Requires a non-blank data version of the raw event body to publish to Azure Event Grid");
            Guard.For(() => eventBody.IsValidJson() == false, new ArgumentException(
                "Requires a valid JSON raw event body payload to publish to Azure Event Grid", nameof(eventBody)));

            var rawEvent = new RawEvent(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime);
            await PublishAsync(rawEvent);
        }

        /// <summary>
        /// Publish a raw JSON payload as CloudEvent event.
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
        /// Publish a raw JSON payload as CloudEvent event.
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
        /// Publish a raw JSON payload as CloudEvent event.
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
        /// Publish an Azure Event Grid event as CloudEvent.
        /// </summary>
        /// <param name="cloudEvent">The event to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cloudEvent"/> is <c>null</c>.</exception>
        public async Task PublishAsync(CloudEvent cloudEvent)
        {
            Guard.NotNull(cloudEvent, nameof(cloudEvent), "Requires a CloudEvent to publish to Azure Event Grid");

            var content = new CloudEventContent(cloudEvent, ContentMode.Structured, JsonEventFormatter);
            await PublishContentToTopicAsync(content, cloudEvent.Type);
        }

        /// <summary>
        /// Publish many Azure Event Grid events as CloudEvents.
        /// </summary>
        /// <param name="events">The events to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="events"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="events"/> is empty or contains <c>null</c> elements.</exception>
        public async Task PublishManyAsync(IEnumerable<CloudEvent> events)
        {
            Guard.NotNull(events, nameof(events), "Requires a set of cloud events to be published to Azure Event Grid");
            Guard.NotAny(events, nameof(events), "Requires at least a single cloud event to be published to Azure Event Grid");
            Guard.For(() => events.Any(@event => @event is null), 
                new ArgumentException("Requires all cloud events to be non-null when publishing to Azure Event Grid", nameof(events)));

            var content = new CloudEventBatchContent(events, ContentMode.Structured, JsonEventFormatter);
            await PublishContentToTopicAsync(content, logEventType: $"[{String.Join(", ", events.Select(ev => ev.Type ?? "<no-event-type>"))}]");
        }

        /// <summary>
        /// Publish an Azure Event Grid event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the specific EventData.</typeparam>
        /// <param name="event">The event to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="event"/> is <c>null</c>.</exception>
        public async Task PublishAsync<TEvent>(TEvent @event)
            where TEvent : class, IEvent
        {
            Guard.NotNull(@event, nameof(@event), "Requires an custom event to publish to Azure Event Grid");

            IEnumerable<TEvent> eventList = new[] {@event};
            await PublishManyAsync(eventList);
        }

        /// <summary>
        /// Publish an Azure Event Grid event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the specific EventData.</typeparam>
        /// <param name="events">The events to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="events"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="events"/> is empty or contains <c>null</c> elements.</exception>
        public async Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events)
            where TEvent : class, IEvent
        {
            Guard.NotNull(events, nameof(events), "Requires a set of events to be published to Azure Event Grid");
            Guard.NotAny(events, nameof(events), "Requires at least a single event to be published to Azure Event Grid");
            Guard.For(() => events.Any(@event => @event is null), 
                new ArgumentException("Requires all events to be non-null when publishing to Azure Event Grid", nameof(events)));

            HttpContent content = CreateSerializedJsonHttpContent(events);
            await PublishContentToTopicAsync(content, logEventType: $"[{String.Join(", ", events.Select(ev => ev.EventType ?? "<no-event-type>"))}]");
        }

        private static HttpContent CreateSerializedJsonHttpContent<TEvent>(IEnumerable<TEvent> events) where TEvent : class, IEvent
        {
            string json = JsonConvert.SerializeObject(events.ToArray());
            var content = new StringContent(json);

            return content;
        }

        private async Task PublishContentToTopicAsync(HttpContent content, string logEventType)
        {
            using (DurationMeasurement measurement = DurationMeasurement.Start())
            {
                var isSuccessful = false;
                try
                {
                    using (HttpResponseMessage response =
                        await _resilientPolicy.ExecuteAsync(() => SendAuthorizedHttpPostRequestAsync(content)))
                    {
                        isSuccessful = response.IsSuccessStatusCode;
                        if (!response.IsSuccessStatusCode)
                        {
                            await ThrowApplicationExceptionAsync(response);
                        }
                    }
                }
                finally
                {
                    if (_options.EnableDependencyTracking)
                    {
                        _logger.LogDependency(
                            dependencyType: "Azure Event Grid",
                            dependencyData: logEventType ?? "<no-event-type>", 
                            targetName: TopicEndpoint, 
                            isSuccessful: isSuccessful, 
                            measurement: measurement); 
                    }
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
                throw new ApplicationException($"Event grid publishing failed with status {response.StatusCode} and content {rawResponse}");
            }
        }
    }
}