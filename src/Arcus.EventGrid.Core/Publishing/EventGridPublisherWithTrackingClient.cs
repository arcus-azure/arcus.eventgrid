using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.EventGrid
{
    /// <summary>
    /// Represents an <see cref="EventGridPublisherClient"/> implementation that provides built-in correlation tracking during event publishing.
    /// </summary>
    public class EventGridPublisherWithTrackingClient : EventGridPublisherClient
    {
        private readonly string _topicEndpoint, _authenticationKeySecretName;
        private readonly ISecretProvider _secretProvider;
        private readonly ICorrelationInfoAccessor _correlationAccessor;

        private EventGridPublisherClient _publisher;
        private static readonly SemaphoreSlim LockCreateEventGridPublisherClient = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisherWithTrackingClient" /> class.
        /// </summary>
        /// <param name="topicEndpoint">The Azure Event Grid topic endpoint to where the events should be published.</param>
        /// <param name="authenticationKeySecretName">The secret name where the authentication key to initiate Azure Event Grid interaction is stored in the Arcus secret store.</param>
        /// <param name="secretProvider">The Arcus secret store implementation to retrieve the authentication key from the <paramref name="authenticationKeySecretName"/>.</param>
        /// <param name="correlationAccessor">The correlation accessor implementation to retrieve the current set correlation when enriching the publishing events</param>
        /// <param name="options">The additional options to influence the correlation tracking and internal HTTP request which represents the publishing event.</param>
        /// <param name="logger">The logger instance to track the Azure Event Grid dependency with correlation information.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> or the <paramref name="authenticationKeySecretName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/>, <paramref name="options"/>, or the <paramref name="logger"/> is <c>null</c>.</exception>
        public EventGridPublisherWithTrackingClient(
            string topicEndpoint,
            string authenticationKeySecretName,
            ISecretProvider secretProvider,
            ICorrelationInfoAccessor correlationAccessor,
            EventGridPublisherClientWithTrackingOptions options,
            ILogger<EventGridPublisherClient> logger)
        {
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "Requires a non-blank Azure Event Grid topic endpoint to register the Azure Event Grid publisher with built-in correlation tracking");
            Guard.NotNullOrWhitespace(authenticationKeySecretName, nameof(authenticationKeySecretName), "Requires a non-blank authentication key to initiate interaction with the Azure Event Grid when registering the Azure Event Grid publisher with built-in correlation tracking");
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires an Arcus secret store implementation to retrieve the authentication key to initiate interaction with Azure Event Grid");
            Guard.NotNull(options, nameof(options), "Requires a set of additional options to influence the correlation tracking and internal HTTP request which represents the publishing event");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure Event Grid dependency with correlation information");

            _topicEndpoint = topicEndpoint;
            _authenticationKeySecretName = authenticationKeySecretName;
            _secretProvider = secretProvider;
            _correlationAccessor = correlationAccessor;

            Options = options;
            Logger = logger;
        }

        /// <summary>
        /// Gets the additional options to influence the correlation tracking and internal HTTP request which represents the publishing event.
        /// </summary>
        protected EventGridPublisherClientWithTrackingOptions Options { get; }

        /// <summary>
        /// Gets the logger instance to track the Azure Event Grid dependency with correlation information.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Publishes a CloudEvent to an Event Grid topic.
        /// </summary>
        /// <param name="cloudEvent"> The set of events to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override Response SendEvent(CloudEvent cloudEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(cloudEvent, nameof(cloudEvent), "Requires a cloud event instance to publish the event to the Azure Event Grid topic");
            return TrackPublishEvent<CloudEvent>(cloudEvent, ev => _publisher.SendEvent(ev, cancellationToken));
        }

        /// <summary>
        /// Publishes a CloudEvent to an Event Grid topic.
        /// </summary>
        /// <param name="cloudEvent"> The set of events to be published to Event Grid.</param>
        /// <param name="channelName">The partner topic channel to publish the event to.</param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override Response SendEvent(CloudEvent cloudEvent, string channelName, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(cloudEvent, nameof(cloudEvent), "Requires a cloud event instance to publish the event to the Azure Event Grid topic");
            return TrackPublishEvent<CloudEvent>(cloudEvent, ev => _publisher.SendEvent(ev, channelName, cancellationToken));
        }
        /// <summary>
        /// Publishes a CloudEvent to an Event Grid topic.
        /// </summary>
        /// <param name="cloudEvent"> The set of events to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override async Task<Response> SendEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(cloudEvent, nameof(cloudEvent), "Requires a cloud event instance to publish the event to the Azure Event Grid topic");
            return await TrackPublishEventAsync<CloudEvent>(cloudEvent, ev => _publisher.SendEventAsync(ev, cancellationToken));
        }

        /// <summary>
        /// Publishes a CloudEvent to an Event Grid topic.
        /// </summary>
        /// <param name="cloudEvent"> The set of events to be published to Event Grid.</param>
        /// <param name="channelName">The partner topic channel to publish the event to.</param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override async Task<Response> SendEventAsync(CloudEvent cloudEvent, string channelName, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(cloudEvent, nameof(cloudEvent), "Requires a cloud event instance to publish the event to the Azure Event Grid topic");
            return await TrackPublishEventAsync<CloudEvent>(cloudEvent, ev => _publisher.SendEventAsync(ev, channelName, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of CloudEvents to an Event Grid topic.
        /// </summary>
        /// <param name="cloudEvents"> The set of events to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override Response SendEvents(IEnumerable<CloudEvent> cloudEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(cloudEvents, nameof(cloudEvents), "Requires a set of cloud event instances to publish the event to the Azure Event Grid topic");
            return TrackPublishEvent<IEnumerable<CloudEvent>>(cloudEvents, events => _publisher.SendEvents(events, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of CloudEvents to an Event Grid topic.
        /// </summary>
        /// <param name="cloudEvents"> The set of events to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override async Task<Response> SendEventsAsync(IEnumerable<CloudEvent> cloudEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(cloudEvents, nameof(cloudEvents), "Requires a set of cloud event instances to publish the event to the Azure Event Grid topic");
            return await TrackPublishEventAsync<IEnumerable<CloudEvent>>(cloudEvents, events => _publisher.SendEventsAsync(events, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of CloudEvents to an Event Grid topic.
        /// </summary>
        /// <param name="cloudEvents"> The set of events to be published to Event Grid. </param>
        /// <param name="channelName">The partner topic channel to publish the event to.</param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override Response SendEvents(IEnumerable<CloudEvent> cloudEvents, string channelName, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(cloudEvents, nameof(cloudEvents), "Requires a set of cloud event instances to publish the event to the Azure Event Grid topic");
            return TrackPublishEvent<IEnumerable<CloudEvent>>(cloudEvents, events => _publisher.SendEvents(events, channelName, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of CloudEvents to an Event Grid topic.
        /// </summary>
        /// <param name="cloudEvents"> The set of events to be published to Event Grid.</param>
        /// <param name="channelName">The partner topic channel to publish the event to.</param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override async Task<Response> SendEventsAsync(IEnumerable<CloudEvent> cloudEvents, string channelName, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(cloudEvents, nameof(cloudEvents), "Requires a set of cloud event instances to publish the event to the Azure Event Grid topic");
            return await TrackPublishEventAsync<IEnumerable<CloudEvent>>(cloudEvents, events => _publisher.SendEventsAsync(events, channelName, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of encoded cloud events to an Event Grid topic.
        /// </summary>
        /// <param name="cloudEvents"> The set of encoded cloud events to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override Response SendEncodedCloudEvents(ReadOnlyMemory<byte> cloudEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.For(() => cloudEvents.IsEmpty, new ArgumentException("Requires a set of cloud event instances to publish the event to the Azure Event Grid topic", nameof(cloudEvents)));
            return TrackPublishEvent<ReadOnlyMemory<byte>>(cloudEvents, events => _publisher.SendEncodedCloudEvents(events, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of encoded cloud events to an Event Grid topic.
        /// </summary>
        /// <param name="cloudEvents"> The set of encoded cloud events to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override async Task<Response> SendEncodedCloudEventsAsync(ReadOnlyMemory<byte> cloudEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.For(() => cloudEvents.IsEmpty, new ArgumentException("Requires a set of cloud event instances to publish the event to the Azure Event Grid topic", nameof(cloudEvents)));
            return await TrackPublishEventAsync<ReadOnlyMemory<byte>>(cloudEvents, events => _publisher.SendEncodedCloudEventsAsync(events, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of EventGridEvents to an Event Grid topic.
        /// </summary>
        /// <param name="eventGridEvent"> The set of events to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override Response SendEvent(EventGridEvent eventGridEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(eventGridEvent, nameof(eventGridEvent), "Requires a Event Grid event instance to publish the event to the Azure Event Grid topic");
            return TrackPublishEvent<EventGridEvent>(eventGridEvent, ev => _publisher.SendEvent(ev, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of EventGridEvents to an Event Grid topic.
        /// </summary>
        /// <param name="eventGridEvent"> The event to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override async Task<Response> SendEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(eventGridEvent, nameof(eventGridEvent), "Requires a Event Grid event instance to publish the event to the Azure Event Grid topic");
            return await TrackPublishEventAsync<EventGridEvent>(eventGridEvent, ev => _publisher.SendEventAsync(ev, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of EventGridEvents to an Event Grid topic.
        /// </summary>
        /// <param name="eventGridEvents"> The set of events to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override Response SendEvents(IEnumerable<EventGridEvent> eventGridEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(eventGridEvents, nameof(eventGridEvents), "Requires a set of Event Grid event instances to publish the event to the Azure Event Grid topic");
            return TrackPublishEvent<IEnumerable<EventGridEvent>>(eventGridEvents, events => _publisher.SendEvents(events, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of EventGridEvents to an Event Grid topic.
        /// </summary>
        /// <param name="eventGridEvents"> The set of events to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override async Task<Response> SendEventsAsync(IEnumerable<EventGridEvent> eventGridEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(eventGridEvents, nameof(eventGridEvents), "Requires a set of Event Grid event instances to publish the event to the Azure Event Grid topic");
            return await TrackPublishEventAsync<IEnumerable<EventGridEvent>>(eventGridEvents, events => _publisher.SendEventsAsync(events, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of custom schema events to an Event Grid topic.
        /// </summary>
        /// <param name="customEvent">A custom schema event to be published to Event Grid.</param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override Response SendEvent(BinaryData customEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(customEvent, nameof(customEvent), "Requires a custom event instances to publish the event to the Azure Event Grid topic");
            return TrackPublishEvent<BinaryData>(customEvent, ev => _publisher.SendEvent(ev, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of custom schema events to an Event Grid topic.
        /// </summary>
        /// <param name="customEvent"> A custom schema event to be published to Event Grid. </param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override Task<Response> SendEventAsync(BinaryData customEvent, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(customEvent, nameof(customEvent), "Requires a custom event instance to publish the event to the Azure Event Grid topic");
            return TrackPublishEventAsync<BinaryData>(customEvent, ev => _publisher.SendEventAsync(ev, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of custom schema events to an Event Grid topic.
        /// </summary>
        /// <param name="customEvents">The set of custom schema events to be published to Event Grid.</param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override Response SendEvents(IEnumerable<BinaryData> customEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(customEvents, nameof(customEvents), "Requires a set of custom event instances to publish the event to the Azure Event Grid topic");
            return TrackPublishEvent<IEnumerable<BinaryData>>(customEvents, events => _publisher.SendEvents(events, cancellationToken));
        }

        /// <summary>
        /// Publishes a set of custom schema events to an Event Grid topic.
        /// </summary>
        /// <param name="customEvents">The set of custom schema events to be published to Event Grid.</param>
        /// <param name="cancellationToken"> An optional cancellation token instance to signal the request to cancel the operation.</param>
        public override async Task<Response> SendEventsAsync(IEnumerable<BinaryData> customEvents, CancellationToken cancellationToken = new CancellationToken())
        {
            Guard.NotNull(customEvents, nameof(customEvents), "Requires a set of custom event instances to publish the event to the Azure Event Grid topic");
            return await TrackPublishEventAsync<IEnumerable<BinaryData>>(customEvents, events => _publisher.SendEventsAsync(events, cancellationToken));
        }

        private Response TrackPublishEvent<TEvent>(object @event, Func<TEvent, Response> publishEvent)
        {
            AuthenticateClient();

            string dependencyId = Options.GenerateDependencyId();
            string transactionId = _correlationAccessor.GetCorrelationInfo()?.TransactionId;
            @event = SetCorrelationPropertiesInEvent(@event, dependencyId, transactionId);

            bool isSuccessful = false;
            using (var measurement = DurationMeasurement.Start())
            {
                try
                {
                    Response response = Options.SyncPolicy.Execute(() => publishEvent((TEvent)@event));
                    isSuccessful = true;

                    return response;
                }
                finally
                {
                    string eventType = DetermineEventType(@event);
                    LogEventGridDependency(eventType, isSuccessful, measurement, dependencyId);
                }
            }
        }

        private void AuthenticateClient()
        {
            LockCreateEventGridPublisherClient.Wait();

            try
            {
                if (_publisher is null)
                {
                    string authenticationKey = _secretProvider.GetRawSecretAsync(_authenticationKeySecretName).GetAwaiter().GetResult();
                    var credential = new AzureKeyCredential(authenticationKey);

                    _publisher = new EventGridPublisherClient(new Uri(_topicEndpoint), credential, Options);
                }
            }
            finally
            {
                LockCreateEventGridPublisherClient.Release();
            }
        }

        private async Task<Response> TrackPublishEventAsync<TEvent>(object @event, Func<TEvent, Task<Response>> publishEvent)
        {
            await AuthenticateClientAsync();

            string dependencyId = Options.GenerateDependencyId();
            string transactionId = _correlationAccessor.GetCorrelationInfo()?.TransactionId;
            @event = SetCorrelationPropertiesInEvent(@event, dependencyId, transactionId);

            bool isSuccessful = false;
            using (var measurement = DurationMeasurement.Start())
            {
                try
                {
                    Response response = await Options.AsyncPolicy.ExecuteAsync(() => publishEvent((TEvent)@event));
                    isSuccessful = true;

                    return response;
                }
                finally
                {
                    string eventType = DetermineEventType(@event);
                    LogEventGridDependency(eventType, isSuccessful, measurement, dependencyId);
                }
            }
        }

        private async Task AuthenticateClientAsync()
        {
            await LockCreateEventGridPublisherClient.WaitAsync();

            try
            {
                if (_publisher is null)
                {
                    string authenticationKey = await _secretProvider.GetRawSecretAsync(_authenticationKeySecretName);
                    var credential = new AzureKeyCredential(authenticationKey);

                    _publisher = new EventGridPublisherClient(new Uri(_topicEndpoint), credential, Options);
                }
            }
            finally
            {
                LockCreateEventGridPublisherClient.Release();
            }
        }

        private static string DetermineEventType(object @event)
        {
            switch (@event)
            {
                case CloudEvent _:
                case IEnumerable<CloudEvent> _:
                case ReadOnlyMemory<byte> _:
                    return "CloudEvent";

                case EventGridEvent _:
                case IEnumerable<EventGridEvent> _:
                    return "EventGridEvent";
                    
                default:
                    return "Custom";
            }
        }

        protected object SetCorrelationPropertiesInEvent(object @event, string dependencyId, string transactionId)
        {
            if (!Options.EnableDependencyTracking)
            {
                return @event;
            }

            string upstreamServicePropertyName = Options.UpstreamServicePropertyName;
            string transactionIdPropertyName = Options.TransactionIdEventDataPropertyName;

            switch (@event)
            {
                case BinaryData data:
                    data = SetCorrelationPropertyInCustomEvent(data, upstreamServicePropertyName, dependencyId);
                    data = SetCorrelationPropertyInCustomEvent(data, transactionIdPropertyName, transactionId);
                    return data;
                case IEnumerable<BinaryData> datas:
                    datas = SetCorrelationPropertyInCustomEvents(datas, upstreamServicePropertyName, dependencyId);
                    datas = SetCorrelationPropertyInCustomEvents(datas, transactionIdPropertyName, transactionId);
                    return datas;
                case EventGridEvent ev:
                    ev = SetCorrelationPropertyInEventGridEvent(ev, upstreamServicePropertyName, dependencyId);
                    ev = SetCorrelationPropertyInEventGridEvent(ev, transactionIdPropertyName, transactionId);
                    return ev;
                case IEnumerable<EventGridEvent> events:
                    events = SetCorrelationPropertyInEventGridEvents(events, upstreamServicePropertyName, dependencyId);
                    events = SetCorrelationPropertyInEventGridEvents(events, transactionIdPropertyName, transactionId);
                    return events;
                case CloudEvent ev:
                    ev = SetCorrelationPropertyInCloudEvent(ev, upstreamServicePropertyName, dependencyId);
                    ev = SetCorrelationPropertyInCloudEvent(ev, transactionIdPropertyName, transactionId);
                    return ev;
                case IEnumerable<CloudEvent> events:
                    events = SetCorrelationPropertyInCloudEvents(events, upstreamServicePropertyName, dependencyId);
                    events = SetCorrelationPropertyInCloudEvents(events, transactionIdPropertyName, transactionId);
                    return events;
                case ReadOnlyMemory<byte> encodedCloudEvents:
                    encodedCloudEvents = SetCorrelationPropertyInEncodedCloudEvents(encodedCloudEvents, upstreamServicePropertyName, dependencyId);
                    encodedCloudEvents = SetCorrelationPropertyInEncodedCloudEvents(encodedCloudEvents, transactionIdPropertyName, transactionId);
                    return encodedCloudEvents;
                default:
                    throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown event type");
            }
        }

        private IEnumerable<EventGridEvent> SetCorrelationPropertyInEventGridEvents(IEnumerable<EventGridEvent> eventGridEvents, string propertyName, string dependencyId)
        {
            var results = new Collection<EventGridEvent>();
            foreach (EventGridEvent eventGridEvent in eventGridEvents)
            {
                EventGridEvent result = SetCorrelationPropertyInEventGridEvent(eventGridEvent, propertyName, dependencyId);
                results.Add(result);
            }

            return results.ToArray();
        }

        protected virtual EventGridEvent SetCorrelationPropertyInEventGridEvent(EventGridEvent eventGridEvent, string propertyName, string propertyValue)
        {
            JsonNode node = JsonNode.Parse(eventGridEvent.Data);
            node![propertyName] = propertyValue;

            eventGridEvent.Data = BinaryData.FromString(node.ToJsonString());
            return eventGridEvent;
        }

        private ReadOnlyMemory<byte> SetCorrelationPropertyInEncodedCloudEvents(ReadOnlyMemory<byte> encodedCloudEvents, string propertyName, string propertyValue)
        {
            CloudEvent[] parsedEvents = CloudEvent.ParseMany(BinaryData.FromBytes(encodedCloudEvents));

            IEnumerable<CloudEvent> cloudEvents = SetCorrelationPropertyInCloudEvents(parsedEvents, propertyName, propertyValue);

            BinaryData data = BinaryData.FromObjectAsJson(cloudEvents);
            return new ReadOnlyMemory<byte>(data.ToArray());
        }

        private IEnumerable<CloudEvent> SetCorrelationPropertyInCloudEvents(IEnumerable<CloudEvent> events, string propertyName, string propertyValue)
        {
            var results = new Collection<CloudEvent>();
            foreach (CloudEvent cloudEvent in events)
            {
                CloudEvent result = SetCorrelationPropertyInCloudEvent(cloudEvent, propertyName, propertyValue);
                results.Add(result);
            }

            return results.ToArray();
        }

        protected virtual CloudEvent SetCorrelationPropertyInCloudEvent(CloudEvent cloudEvent, string upstreamServicePropertyName, string propertyValue)
        {
            JsonNode node = JsonNode.Parse(cloudEvent.Data);
            node![upstreamServicePropertyName] = propertyValue;

            cloudEvent.Data = BinaryData.FromString(node.ToJsonString());
            return cloudEvent;
        }

        private IEnumerable<BinaryData> SetCorrelationPropertyInCustomEvents(IEnumerable<BinaryData> datas, string propertyName, string propertyValue)
        {
            var results = new Collection<BinaryData>();
            foreach (BinaryData data in datas)
            {
                BinaryData result = SetCorrelationPropertyInCustomEvent(data, propertyName, propertyValue);
                results.Add(result);
            }

            return results.ToArray();
        }

        protected virtual BinaryData SetCorrelationPropertyInCustomEvent(BinaryData data, string upstreamServicePropertyName, string propertyValue)
        {
            JsonNode node = JsonNode.Parse(data);
            node![upstreamServicePropertyName] = propertyValue;

            return BinaryData.FromString(node.ToJsonString());
        }

        private void LogEventGridDependency(string eventType, bool isSuccessful, DurationMeasurement measurement, string dependencyId)
        {
            if (Options.EnableDependencyTracking)
            {
                Logger.LogDependency(
                    dependencyType: "Azure Event Grid",
                    dependencyData: eventType,
                    targetName: _topicEndpoint,
                    isSuccessful: isSuccessful,
                    measurement: measurement,
                    dependencyId: dependencyId,
                    context: Options.TelemetryContext);
            }
        }
    }
}