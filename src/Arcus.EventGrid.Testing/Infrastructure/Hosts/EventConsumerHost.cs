using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using GuardNet;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Timeout;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts
{
    /// <summary>
    ///     Foundation for all event consumer hosts that handle Azure Event Grid events to be consumed in integration tests
    /// </summary>
    public class EventConsumerHost
    {
        private readonly ConcurrentDictionary<string, string> _events = new ConcurrentDictionary<string, string>();

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

        /// <summary>
        /// Gets the logger associated with this event consumer.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Handles new received events into the event consumer that can later be retrieved.
        /// </summary>
        /// <param name="rawReceivedEvents">The raw payload containing all received events.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="rawReceivedEvents"/> is blank.</exception>
        protected void EventsReceived(string rawReceivedEvents)
        {
            Guard.NotNullOrWhitespace(rawReceivedEvents, nameof(rawReceivedEvents), "Requires a non-blank raw event payload containing the serialized received events");

            JToken token = JToken.Parse(rawReceivedEvents);
            JObject[] events = token.Type switch
            {
                JTokenType.Array => token.Children<JObject>().ToArray(),
                JTokenType.Object => new [] { (JObject) token },
                _ =>  throw new InvalidOperationException(
                    "Couldn't find a correct JSON structure (array or object) to parse the EventGridEvent/CloudEvents from")
            };

            foreach (JObject ev in events)
            {
                var eventId = ev["id"]?.ToString() ?? Guid.NewGuid().ToString();

                Logger.LogTrace("Received event '{EventId}' on event consumer host", eventId);
                _events.AddOrUpdate(eventId, ev.ToString(), (id, raw) => ev.ToString());
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
                if (result.FinalException is TimeoutRejectedException)
                {
                    throw new TimeoutException(
                        "Could not in the time available receive an event from Event Grid on the Service Bus topic");
                }

                throw result.FinalException;
            }

            return result.Result;
        }

        /// <summary>
        /// Gets the event envelope that includes a requested event (uses timeout).
        /// </summary>
        /// <param name="cloudEventFilter">The custom event filter to select a specific <see cref="CloudEvent "/> event.</param>
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
                CreateTimeoutPolicy<CloudEvent>(ev => ev is null, timeout);

            PolicyResult<CloudEvent> result =
                timeoutPolicy.ExecuteAndCapture(() =>
                {
                    return TryGetReceivedEvent(
                        received => CloudEvent.Parse(BinaryData.FromString(received)),
                        cloudEventFilter);
                });

            if (result.Outcome is OutcomeType.Failure)
            {
                if (result.FinalException is TimeoutRejectedException)
                {
                    throw new TimeoutException(
                        $"Could not in the time available ({timeout:g}) receive an CloudEvent event from Azure Event Grid on the Service Bus topic that matches the given filter");
                }

                throw result.FinalException;
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
                CreateTimeoutPolicy<EventGridEvent>(ev => ev is null, timeout);

            PolicyResult<EventGridEvent> result =
                timeoutPolicy.ExecuteAndCapture(() =>
                {
                    return TryGetReceivedEvent(
                        received => EventGridEvent.Parse(BinaryData.FromString(received)),
                        eventGridEventFilter);
                });

            if (result.Outcome is OutcomeType.Failure)
            {
                if (result.FinalException is TimeoutRejectedException)
                {
                    throw new TimeoutException(
                        $"Could not in the time available ({timeout:g}) receive an CloudEvent event from Azure Event Grid on the Service Bus topic that matches the given filter");
                }

                throw result.FinalException;
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

        private TEvent TryGetReceivedEvent<TEvent>(
            Func<string, TEvent> eventParser,
            Func<TEvent, bool> eventFilter)
            where TEvent : class
        {
            if (_events.IsEmpty)
            {
                Logger.LogTrace("No received events found");
            }
            else
            {
                Logger.LogTrace("Current received event batches are: {ReceivedEvents}", string.Join(", ", _events.Keys));

                foreach (KeyValuePair<string, string> received in _events)
                {
                    try
                    {
                        TEvent parsed = eventParser(received.Value);
                        if (parsed != null && eventFilter(parsed))
                        {
                            Logger.LogInformation("Found received event with ID: {EventId}", received.Key);
                            return parsed;
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.LogTrace(exception, "Could not parse event as {EventType}: {EventId}", typeof(TEvent).Name, received.Key);
                    }
                }

                Logger.LogInformation("None of the received events matches the event filter: {ReceivedEvents}", string.Join(Environment.NewLine, _events.Keys));
            }

            return null;
        }

        private string TryGetReceivedEvent(string eventId)
        {
            if (_events.IsEmpty)
            {
                Logger.LogTrace("No received events found with event ID: '{EventId}'", eventId);
            }
            else
            {
                Logger.LogTrace("Current received events are: {ReceivedEvents}", string.Join(", ", _events.Keys));
                if (_events.TryGetValue(eventId, out string originalEvent))
                {
                    Logger.LogInformation("Found received event with ID: {EventId}", eventId);
                    return originalEvent;
                }
            }

            return null;
        }
    }
}