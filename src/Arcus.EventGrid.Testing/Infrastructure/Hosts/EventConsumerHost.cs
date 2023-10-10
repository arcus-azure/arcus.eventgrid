using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Timeout;
using NewCloudEvent = Azure.Messaging.CloudEvent;
using NewEventGridEvent = Azure.Messaging.EventGrid.EventGridEvent; 
using OldCloudEvent = CloudNative.CloudEvents.CloudEvent;
using OldEventGridEvent = Microsoft.Azure.EventGrid.Models.EventGridEvent;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts
{
    /// <summary>
    ///     Foundation for all event consumer hosts that handle Azure Event Grid events to be consumed in integration tests
    /// </summary>
    public class EventConsumerHost
    {
        private readonly ConcurrentDictionary<string, (string originalEvent, Event parsedEvent)> _oldEvents = new ConcurrentDictionary<string, (string, Event)>();
        private readonly ConcurrentDictionary<string, string> _newEvents = new ConcurrentDictionary<string, string>();

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
        /// <exception cref="JsonReaderException">Thrown when the <paramref name="rawReceivedEvents"/> failed to be read as valid JSON.</exception>
        protected void EventsReceived(string rawReceivedEvents)
        {
            Guard.NotNullOrWhitespace(rawReceivedEvents, nameof(rawReceivedEvents), "Requires a non-blank raw event payload containing the serialized received events");

#pragma warning disable CS0618
            EventsReceived(rawReceivedEvents, Logger);
#pragma warning restore CS0618
        }

        /// <summary>
        /// Handles new received events into the event consumer that can later be retrieved.
        /// </summary>
        /// <param name="rawReceivedEvents">The raw payload containing all received events.</param>
        /// <param name="logger">The logger to use for writing event information of the received events.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="rawReceivedEvents"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="JsonReaderException">Thrown when the <paramref name="rawReceivedEvents"/> failed to be read as valid JSON.</exception>
        [Obsolete("Use the overload without the logger instead")]
        protected void EventsReceived(string rawReceivedEvents, ILogger logger)
        {
            Guard.NotNullOrWhitespace(rawReceivedEvents, nameof(rawReceivedEvents), "Requires a non-blank raw event payload containing the serialized received events");
            Guard.NotNull(logger, nameof(logger), "Requires an logger instance to write event information of the received events");

            EventBatch<Event> eventBatch = EventParser.Parse(rawReceivedEvents);
            foreach (Event receivedEvent in eventBatch.Events)
            {
                logger.LogTrace("Received event '{EventId}' on event consumer host", receivedEvent.Id);
                _oldEvents.AddOrUpdate(receivedEvent.Id, (rawReceivedEvents, receivedEvent), (id, ev) => (rawReceivedEvents, receivedEvent));
                _newEvents.AddOrUpdate(receivedEvent.Id, rawReceivedEvents, (id, ev) => rawReceivedEvents);
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
        /// <param name="cloudEventFilter">The custom event filter to select a specific <see cref="NewCloudEvent "/> event.</param>
        /// <param name="timeout">The time period in which the event should be consumed.</param>
        /// <returns>
        ///     The deserialized <see cref="NewCloudEvent"/> event that matches the specified <paramref name="cloudEventFilter"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cloudEventFilter"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="timeout"/> is a negative time range.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when no event could be received within the specified <paramref name="timeout"/> time range that matches the given <paramref name="cloudEventFilter"/>.
        /// </exception>
        public NewCloudEvent GetReceivedEvent(Func<NewCloudEvent, bool> cloudEventFilter, TimeSpan timeout)
        {
            Guard.NotNull(cloudEventFilter, nameof(cloudEventFilter), "Requires a function to filter out received CloudEvent events");
            Guard.NotLessThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout), "Requires a timeout span representing a positive time range");

            Policy<NewCloudEvent> timeoutPolicy =
                CreateTimeoutPolicy<NewCloudEvent>(ev => ev is null, timeout);

            PolicyResult<NewCloudEvent> result =
                timeoutPolicy.ExecuteAndCapture(() =>
                {
                    return TryGetReceivedEvent(
                        received => NewCloudEvent.Parse(BinaryData.FromString(received)),
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
        /// <param name="cloudEventFilter">The custom event filter to select a specific <see cref="OldCloudEvent"/> event.</param>
        /// <param name="timeout">The time period in which the event should be consumed.</param>
        /// <returns>
        ///     The deserialized <see cref="OldCloudEvent"/> event that matches the specified <paramref name="cloudEventFilter"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cloudEventFilter"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="timeout"/> is a negative time range.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when no event could be received within the specified <paramref name="timeout"/> time range that matches the given <paramref name="cloudEventFilter"/>.
        /// </exception>
        [Obsolete("Use 'CloudEvent' overload from 'Azure.Messaging.EventGrid' package")] 
        public OldCloudEvent GetReceivedEvent(Func<OldCloudEvent, bool> cloudEventFilter, TimeSpan timeout)
        {
            Guard.NotNull(cloudEventFilter, nameof(cloudEventFilter), "Requires a function to filter out received CloudEvent events");
            Guard.NotLessThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout), "Requires a timeout span representing a positive time range");

            Policy<OldCloudEvent> timeoutPolicy = 
                CreateTimeoutPolicy<OldCloudEvent>(ev => ev is null, timeout);
            
            PolicyResult<OldCloudEvent> result =
                timeoutPolicy.ExecuteAndCapture(() => 
                    TryGetReceivedEvent(
                        ev => cloudEventFilter(ev)));

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
        /// <param name="eventGridEventFilter">The custom event filter to select a specific <see cref="NewEventGridEvent"/> event.</param>
        /// <param name="timeout">The time period in which the event should be consumed.</param>
        /// <returns>
        ///     The deserialized <see cref="NewEventGridEvent"/> event that matches the specified <paramref name="eventGridEventFilter"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventGridEventFilter"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="timeout"/> is a negative time range.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when no event could be received within the specified <paramref name="timeout"/> time range that matches the given <paramref name="eventGridEventFilter"/>.
        /// </exception>
        public NewEventGridEvent GetReceivedEvent(Func<NewEventGridEvent, bool> eventGridEventFilter, TimeSpan timeout)
        {
            Guard.NotNull(eventGridEventFilter, nameof(eventGridEventFilter), "Requires a function to filter out received CloudEvent events");
            Guard.NotLessThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout), "Requires a timeout span representing a positive time range");

            Policy<NewEventGridEvent> timeoutPolicy =
                CreateTimeoutPolicy<NewEventGridEvent>(ev => ev is null, timeout);

            PolicyResult<NewEventGridEvent> result =
                timeoutPolicy.ExecuteAndCapture(() =>
                {
                    return TryGetReceivedEvent(
                        received => NewEventGridEvent.Parse(BinaryData.FromString(received)),
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
        /// Gets the event envelope that includes a requested event (uses timeout).
        /// </summary>
        /// <param name="eventGridEventFilter">The custom event filter to select a specific <see cref="OldEventGridEvent"/> event.</param>
        /// <param name="timeout">The time period in which the event should be consumed.</param>
        /// <returns>
        ///     The deserialized <see cref="OldEventGridEvent"/> event that matches the specified <paramref name="eventGridEventFilter"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventGridEventFilter"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="timeout"/> is a negative time range.</exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when no event could be received within the specified <paramref name="timeout"/> time range that matches the given <paramref name="eventGridEventFilter"/>.
        /// </exception>
        [Obsolete("Use 'EventGridEvent' overload from 'Azure.Messaging.EventGrid' package")] 
        public OldEventGridEvent GetReceivedEvent(Func<OldEventGridEvent, bool> eventGridEventFilter, TimeSpan timeout)
        {
            Guard.NotNull(eventGridEventFilter, nameof(eventGridEventFilter), "Requires a function to filter out received CloudEvent events");
            Guard.NotLessThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout), "Requires a timeout span representing a positive time range");

            Policy<OldEventGridEvent> timeoutPolicy = 
                CreateTimeoutPolicy<OldEventGridEvent>(ev => ev is null, timeout);
            
            PolicyResult<OldEventGridEvent> result =
                timeoutPolicy.ExecuteAndCapture(() => 
                    TryGetReceivedEvent(ev => eventGridEventFilter(ev)));

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
        [Obsolete("Use either 'CloudEvent' or 'EventGridEvent' overloads")]
        public Event GetReceivedEvent<TEventPayload>(Func<TEventPayload, bool> eventPayloadFilter, TimeSpan timeout)
        {
            Guard.NotNull(eventPayloadFilter, nameof(eventPayloadFilter), "Requires a function to filter out received CloudEvent events");
            Guard.NotLessThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout), "Requires a timeout span representing a positive time range");
            
            Policy<Event> timeoutPolicy = 
                CreateTimeoutPolicy<Event>(ev => ev is null, timeout);

            PolicyResult<Event> result =
                timeoutPolicy.ExecuteAndCapture(() =>
                    TryGetReceivedEvent(ev =>
                    {
                        var payload = ev.GetPayload<TEventPayload>();
                        return payload != null && eventPayloadFilter(payload);
                    }));
            
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
            if (_newEvents.IsEmpty)
            {
                Logger.LogTrace("No received events found");
            }
            else
            {
                Logger.LogTrace("Current received event batches are: {ReceivedEvents}", string.Join(", ", _newEvents.Keys));

                foreach (KeyValuePair<string, string> received in _newEvents)
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

                Logger.LogInformation("None of the received events matches the event filter: {ReceivedEvents}", string.Join(Environment.NewLine, _newEvents.Keys));
            }

            return null;
        }

        private Event TryGetReceivedEvent(Func<Event, bool> eventFilter)
        {
            if (_oldEvents.IsEmpty)
            {
                Logger.LogTrace("No received events found");
            }
            else
            {
                Logger.LogTrace("Current received event batches are: {ReceivedEvents}", String.Join(", ", _oldEvents.Keys));

                (string eventId, (string originalEvent, Event parsedEvent)) = _oldEvents.FirstOrDefault(ev => eventFilter(ev.Value.parsedEvent));
                if (parsedEvent != null)
                {
                    Logger.LogInformation("Found received event with ID: {EventId}", eventId);
                }
                else
                {
                    Logger.LogInformation("None of the received events matches the event filter: {ReceivedEvents}", String.Join(Environment.NewLine, _oldEvents.Keys));
                }

                return parsedEvent;
            }

            return null;
        }

        private string TryGetReceivedEvent(string eventId)
        {
            if (_newEvents.IsEmpty)
            {
                Logger.LogTrace("No received events found with event ID: '{EventId}'", eventId);
            }
            else
            {
                Logger.LogTrace("Current received events are: {ReceivedEvents}", string.Join(", ", _newEvents.Keys));
                if (_newEvents.TryGetValue(eventId, out string originalEvent))
                {
                    Logger.LogInformation("Found received event with ID: {EventId}", eventId);
                    return originalEvent;
                }
            }

            return null;
        }
    }
}