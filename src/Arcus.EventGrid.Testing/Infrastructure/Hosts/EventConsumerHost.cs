using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts
{
    /// <summary>
    ///     Foundation for all event consumer hosts that handle Azure Event Grid events to be consumed in integration tests
    /// </summary>
    public class EventConsumerHost
    {
        private static readonly ConcurrentDictionary<string, string> ReceivedEvents = new ConcurrentDictionary<string, string>();

        /// <summary>
        ///     Gets the logger associated with this event consumer.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="logger">Logger to use for writing event information</param>
        public EventConsumerHost(ILogger logger)
        {
            Guard.NotNull(logger, nameof(logger));

            Logger = logger;
        }

        /// <summary>
        ///     Handles new events that are being received
        /// </summary>
        /// <param name="rawReceivedEvents">Raw payload containing all events</param>
        /// <param name="logger">Logger to use for writing event information</param>
        protected static void EventsReceived(string rawReceivedEvents, ILogger logger)
        {
            Guard.NotNullOrWhitespace(rawReceivedEvents, nameof(rawReceivedEvents));

            JArray parsedEvents = JArray.Parse(rawReceivedEvents);
            foreach (JToken parsedEvent in parsedEvents)
            {
                string eventId = DetermineEventId(parsedEvent);
                if (eventId == null)
                {
                    logger.LogWarning($"Event was received without an event id. Payload : {parsedEvent}");
                }
                else
                {
                    ReceivedEvents.AddOrUpdate(eventId, rawReceivedEvents, (key, value) => rawReceivedEvents);
                }
            }
        }

        /// <summary>
        ///     Gets the event envelope that includes a requested event (Uses exponential back-off)
        /// </summary>
        /// <param name="eventId">Event id for requested event</param>
        /// <param name="retryCount">Amount of retries while waiting for the event to come in</param>
        public string GetReceivedEvent(string eventId, int retryCount = 5)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId));

            Policy<string> retryPolicy =
                Policy.HandleResult<string>(String.IsNullOrWhiteSpace)
                      .WaitAndRetry(retryCount, currentRetryCount => TimeSpan.FromSeconds(Math.Pow(2, currentRetryCount)));

            PolicyResult<string> result = 
                retryPolicy.ExecuteAndCapture(() => TryGetReceivedEvent(eventId));
            
            if (result.Outcome == OutcomeType.Failure)
            {
                throw new TimeoutException(
                    "Could not in the available retry counts receive an event from Event Grid on the Service Bus topic");
            }

            return result.Result;
        }

        /// <summary>
        ///     Gets the event envelope that includes a requested event (Uses timeout)
        /// </summary>
        /// <param name="eventId">Event id for requested event</param>
        /// <param name="timeout">Time period in which the event should be received.</param>
        public string GetReceivedEvent(string eventId, TimeSpan timeout)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId));
            Guard.NotLessThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout), "Timeout should be representing a positive time range");

            Policy<string> timeoutPolicy =
                Policy.Timeout(timeout)
                      .Wrap(Policy.HandleResult<string>(String.IsNullOrWhiteSpace)
                                  .WaitAndRetryForever(retryCount => TimeSpan.FromSeconds(1)));

            PolicyResult<string> result = 
                timeoutPolicy.ExecuteAndCapture(() => TryGetReceivedEvent(eventId));

            if (result.Outcome == OutcomeType.Failure)
            {
                throw new TimeoutException(
                    "Could not in the time available receive an event from Event Grid on the Service Bus topic");
            }

            return result.Result;
        }

        /// <summary>
        ///     Stop receiving traffic
        /// </summary>
        public virtual Task StopAsync()
        {
            Logger.LogInformation("Host stopped");

            return Task.CompletedTask;
        }

        private string TryGetReceivedEvent(string eventId)
        {
            Logger.LogInformation("Received events are : {receivedEvents}", String.Join(", ", ReceivedEvents.Keys));
            ReceivedEvents.TryGetValue(eventId, out string rawEvent);

            return rawEvent;
        }

        private static string DetermineEventId(JToken parsedEvent)
        {
            Guard.NotNull(parsedEvent, nameof(parsedEvent));

            if (((JObject) parsedEvent).TryGetValue("Id", StringComparison.InvariantCultureIgnoreCase, out JToken eventIdNode))
            {
                return eventIdNode.ToString();
            }

            return string.Empty;
        }
    }
}