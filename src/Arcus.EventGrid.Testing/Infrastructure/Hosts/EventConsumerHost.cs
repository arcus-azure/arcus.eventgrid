using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts
{
    /// <summary>
    /// Represents a 
    /// </summary>
    public class EventEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventEntry" /> class.
        /// </summary>
        public EventEntry(string eventId, string eventPayload)
        {
            Guard.NotNull(eventId, nameof(eventId), "Requires a non-blank");
        }
        
        public string EventId { get; }
        public string EventPayload { get; }
    }
    
    /// <summary>
    ///     Foundation for all event consumer hosts that handle Azure Event Grid events to be consumed in integration tests
    /// </summary>
    public class EventConsumerHost
    {
        // TODO: is 'static' correct here? Multiple event consumers should have different sets of received events, right?
        private static readonly ConcurrentDictionary<string, EventEntry> ReceivedEvents = new ConcurrentDictionary<string, EventEntry>();
        
        /// <summary>
        ///     Gets the logger associated with this event consumer.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventConsumerHost"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for writing event information of the received events.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public EventConsumerHost(ILogger logger)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write event information of the received events");

            Logger = logger;
        }

        // TODO: is 'static' correct here? Couldn't we provide a instance member where we use the constructor-provided logger instead?
        /// <summary>
        /// Handles new received events into the event consumer that can later be retrieved.
        /// </summary>
        /// <param name="rawReceivedEvents">The raw payload containing all received events.</param>
        /// <param name="logger">The logger to use for writing event information of the received events.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="rawReceivedEvents"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="JsonReaderException">Thrown when the <paramref name="rawReceivedEvents"/> failed to be read as valid JSON.</exception>
        protected static void EventsReceived(string rawReceivedEvents, ILogger logger)
        {
            Guard.NotNullOrWhitespace(rawReceivedEvents, nameof(rawReceivedEvents), "Requires a non-blank raw event payload containing the serialized received events");
            Guard.NotNull(logger, nameof(logger), "Requires an logger instance to write event information of the received events");
            
            JToken jToken = JToken.Parse(rawReceivedEvents);
            if (jToken.Type is JTokenType.Array)
            {
                foreach (JToken parsedEvent in jToken.Children())
                {
                    SaveEvent(rawReceivedEvents, logger, parsedEvent);
                }
            }
            else if (jToken.Type is JTokenType.Object)
            {
                SaveEvent(rawReceivedEvents, logger, jToken);
            }
            else
            {
                logger.LogWarning("Could not save event because it doesn't represents either a JSON array (multiple events) or object (single event): '{EventPayload}'", rawReceivedEvents);
            }
        }

        private static void SaveEvent(string rawReceivedEvents, ILogger logger, JToken parsedEvent)
        {
            string eventId = DetermineEventId(parsedEvent);
            if (eventId is null)
            {
                logger.LogWarning("Could not save event because event was received without an event ID and payload: {EventPayload}", parsedEvent);
            }
            else
            {
                logger.LogTrace("Received event with ID: {EventId} and payload: {EventPayload}", eventId, parsedEvent);
                ReceivedEvents.AddOrUpdate(eventId, new EventEntry(eventId, rawReceivedEvents), (key, value) => new EventEntry(key, rawReceivedEvents));
            }
        }

        /// <summary>
        /// Gets the event envelope that includes a requested event (uses exponential back-off).
        /// </summary>
        /// <param name="eventId">The vent ID for requested event.</param>
        /// <param name="retryCount">The amount of retries while waiting for the event to come in.</param>
        /// <returns>
        ///     The raw received event contents with the given <paramref name="eventId"/>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventId"/> is blank.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="retryCount"/> is a negative amount of retries.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when the no event could be received with the exponential back-off of the given amount of <paramref name="retryCount"/>.
        /// </exception>
        public string GetReceivedEvent(string eventId, int retryCount = 5)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId), "Requires a non-blank event ID to identify the consumed event");
            Guard.NotLessThanOrEqualTo(retryCount, 0, nameof(retryCount), "Requires a positive amount of retries while waiting for the event to come in");
            
            Policy<string> retryPolicy =
                Policy.HandleResult<string>(String.IsNullOrWhiteSpace)
                      .WaitAndRetry(retryCount, currentRetryCount => TimeSpan.FromSeconds(Math.Pow(2, currentRetryCount)));

            PolicyResult<string> result = 
                retryPolicy.ExecuteAndCapture(() => TryGetReceivedEvent(eventId));
            
            if (result.Outcome is OutcomeType.Failure)
            {
                throw new TimeoutException(
                    "Could not in the available retry counts receive an event from Event Grid on the Service Bus topic");
            }

            return result.Result;
        }

        /// <summary>
        /// Gets the event envelope that includes a requested event (uses timeout).
        /// </summary>
        /// <param name="eventId">The event ID for requested event.</param>
        /// <param name="timeout">The time period in which the event should be consumed.</param>
        /// <returns>
        ///     The raw received event contents with the given <paramref name="eventId"/>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventId"/> is blank.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="timeout"/> is a negative time range.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when no event could be received within the specified <paramref name="timeout"/> time range with the given <paramref name="eventId"/>.
        /// </exception>
        public string GetReceivedEvent(string eventId, TimeSpan timeout)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId), "Requires a non-blank event ID to identify the consumed event");
            Guard.NotLessThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout), "Timeout should be representing a positive time range");

            Policy<string> timeoutPolicy = 
                CreateTimeoutPolicy<string>(String.IsNullOrWhiteSpace, timeout);
            
            PolicyResult<string> result = 
                timeoutPolicy.ExecuteAndCapture(() => TryGetReceivedEvent(eventId));

            if (result.Outcome is OutcomeType.Failure)
            {
                throw new TimeoutException(
                    "Could not in the time available receive an event from Event Grid on the Service Bus topic");
            }

            return result.Result;
        }
        
        /// <summary>
        /// Gets the event envelope that includes a requested event (uses timeout).
        /// </summary>
        /// <param name="cloudEventFilter">The custom event filter to select a specific <see cref="CloudEvent"/> event.</param>
        /// <param name="timeout">The time period in which the event should be consumed.</param>
        /// <returns>
        ///     The deserialized <see cref="CloudEvent"/> event that matches the specified <paramref name="cloudEventFilter"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cloudEventFilter"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="timeout"/> is a negative time range.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when no event could be received within the specified <paramref name="timeout"/> time range that matches the given <paramref name="cloudEventFilter"/>.
        /// </exception>
        public CloudEvent GetReceivedEvent(Func<CloudEvent, bool> cloudEventFilter, TimeSpan timeout)
        {
            Guard.NotNull(cloudEventFilter, nameof(cloudEventFilter), "Requires a function to filter out received CloudEvent events");
            Guard.NotLessThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout), "Requires a timeout span representing a positive time range");

            Policy<CloudEvent> timeoutPolicy = 
                CreateTimeoutPolicy<CloudEvent>(ev => ev != null, timeout);
            
            PolicyResult<CloudEvent> result =
                timeoutPolicy.ExecuteAndCapture(() => 
                    TryGetReceivedEvent(ev => cloudEventFilter(ev)));

            if (result.Outcome is OutcomeType.Failure)
            {
                throw new TimeoutException(
                    $"Could not in the time available ({timeout:g}) receive an CloudEvent event from Azure Event Grid on the Service Bus topic that matches the given filter");
            }

            return result.Result;
        }

        /// <summary>
        /// Gets the event envelope that includes a requested event (uses timeout).
        /// </summary>
        /// <param name="eventGridEventFilter">The custom event filter to select a specific <see cref="EventGridEvent"/> event.</param>
        /// <param name="timeout">The time period in which the event should be consumed.</param>
        /// <returns>
        ///     The deserialized <see cref="EventGridEvent"/> event that matches the specified <paramref name="eventGridEventFilter"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventGridEventFilter"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="timeout"/> is a negative time range.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when no event could be received within the specified <paramref name="timeout"/> time range that matches the given <paramref name="eventGridEventFilter"/>.
        /// </exception>
        public EventGridEvent GetReceivedEvent(Func<EventGridEvent, bool> eventGridEventFilter, TimeSpan timeout)
        {
            Guard.NotNull(eventGridEventFilter, nameof(eventGridEventFilter), "Requires a function to filter out received CloudEvent events");
            Guard.NotLessThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout), "Requires a timeout span representing a positive time range");

            Policy<EventGridEvent> timeoutPolicy = 
                CreateTimeoutPolicy<EventGridEvent>(ev => ev != null, timeout);
            
            PolicyResult<EventGridEvent> result =
                timeoutPolicy.ExecuteAndCapture(() => 
                    TryGetReceivedEvent(ev => eventGridEventFilter(ev)));

            if (result.Outcome is OutcomeType.Failure)
            {
                throw new TimeoutException(
                    $"Could not in the time available ({timeout:g}) receive an CloudEvent event from Azure Event Grid on the Service Bus topic that matches the given filter");
            }

            return result.Result;
        }

        /// <summary>
        /// Gets the event envelope that includes the requested event (uses timeout).
        /// </summary>
        /// <typeparam name="TEventPayload">The custom event payload of the requested consumed event.</typeparam>
        /// <param name="eventPayloadFilter">The custom event filter to select an <see cref="Event"/> with a specific event payload.</param>
        /// <param name="timeout">The time period in which the event should be consumed.</param>
        /// <returns>
        ///     The deserialized abstract <see cref="Event"/> whose payload matches the specified <paramref name="eventPayloadFilter"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventPayloadFilter"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="timeout"/> is a negative time range.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when no event could be retrieved within the specified <paramref name="timeout"/> time range
        ///     whose event payload matches the given <paramref name="eventPayloadFilter"/>.
        /// </exception>
        public Event GetReceivedEvent<TEventPayload>(Func<TEventPayload, bool> eventPayloadFilter, TimeSpan timeout)
        {
            Guard.NotNull(eventPayloadFilter, nameof(eventPayloadFilter), "Requires a function to filter out received CloudEvent events");
            Guard.NotLessThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout), "Requires a timeout span representing a positive time range");
            
            Policy<Event> timeoutPolicy = 
                CreateTimeoutPolicy<Event>(ev => ev != null, timeout);

            PolicyResult<Event> result =
                timeoutPolicy.ExecuteAndCapture(() =>
                    TryGetReceivedEvent(ev =>
                    {
                        var payload = ev.GetPayload<TEventPayload>();
                        return payload != null && eventPayloadFilter(payload);
                    }));
            
            if (result.Outcome is OutcomeType.Failure)
            {
                throw new TimeoutException(
                    $"Could not in the time available ({timeout:g}) receive an CloudEvent event from Azure Event Grid on the Service Bus topic that matches the given filter");
            }

            return result.Result;
        }

        /// <summary>
        ///     Stop receiving traffic
        /// </summary>
        public virtual Task StopAsync()
        {
            // TODO: job ID to identify event consumer?
            Logger.LogInformation("Host stopped");

            return Task.CompletedTask;
        }

        private static Policy<TResult> CreateTimeoutPolicy<TResult>(Func<TResult, bool> resultPredicate, TimeSpan timeout)
        {
            // TODO: configurable retry count?
            Policy<TResult> timeoutPolicy =
                Policy.Timeout(timeout)
                      .Wrap(Policy.HandleResult(resultPredicate)
                                  .WaitAndRetryForever(retryCount => TimeSpan.FromSeconds(1)));

            return timeoutPolicy;
        }

        private static Event TryGetReceivedEvent(Func<Event, bool> eventFilter)
        {
            Event @event = 
                ReceivedEvents.Values
                    .Select(value => EventParser.Parse(value.EventPayload))
                    .SelectMany(batch => batch.Events)
                    .FirstOrDefault(eventFilter);

            return @event;
        }

        private string TryGetReceivedEvent(string eventId)
        {
            Logger.LogTrace("Current received events are: {receivedEvents}", String.Join(", ", ReceivedEvents.Keys));
            if (ReceivedEvents.TryGetValue(eventId, out EventEntry rawEvent))
            {
                Logger.LogInformation("Found received event with ID: {EventId}", eventId);
                return rawEvent?.EventPayload;
            }

            return null;
        }

        private static string DetermineEventId(JToken parsedEvent)
        {
            if (parsedEvent is null)
            {
                throw new InvalidOperationException(
                    "Could not parse the incoming raw event to a valid JSON structure, make sure that the consumed events in the test are serialized as JSON tokens");
            }
            
            if (parsedEvent is JObject jObject 
                && jObject.TryGetValue("Id", StringComparison.InvariantCultureIgnoreCase, out JToken eventIdNode))
            {
                return eventIdNode.ToString();
            }

            return null;
        }
    }
}