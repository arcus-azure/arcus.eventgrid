using System;
using System.Collections.Generic;
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
        // TODO: shouldn't this be a 'ConcurrentDictionary'?
        private static readonly Dictionary<string, string> ReceivedEvents = new Dictionary<string, string>();

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
        protected static void EventsReceived(string rawReceivedEvents)
        {
            Guard.NotNullOrWhitespace(rawReceivedEvents, nameof(rawReceivedEvents));

            var parsedEvents = JArray.Parse(rawReceivedEvents);
            foreach (var parsedEvent in parsedEvents)
            {
                var eventId = parsedEvent["Id"]?.ToString();

                ReceivedEvents[eventId] = rawReceivedEvents;
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

            var retryPolicy = Policy.HandleResult<string>(String.IsNullOrWhiteSpace)
                .WaitAndRetry(retryCount, currentRetryCount => TimeSpan.FromSeconds(Math.Pow(2, currentRetryCount)));

            var matchingEvent = retryPolicy.Execute(() =>
            {
                Logger.LogInformation("Received events are : {receivedEvents}", String.Join(", ", ReceivedEvents.Keys));

                ReceivedEvents.TryGetValue(eventId, out var rawEvent);
                return rawEvent;
            });

            return matchingEvent;
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

            var timeoutPolicy = Policy.Timeout(timeout)
                                      .Wrap(Policy.HandleResult<string>(String.IsNullOrWhiteSpace)
                                                  .RetryForever());

            string matchingEvent = timeoutPolicy.Execute(() =>
            {
                Logger.LogInformation("Received events are : {receivedEvents}",
                                       String.Join(", ", ReceivedEvents.Keys));
                ReceivedEvents.TryGetValue(eventId, out string rawEvent);
                return rawEvent;
            });

            return matchingEvent;
        }

        /// <summary>
        ///     Stop receiving traffic
        /// </summary>
        public virtual Task Stop()
        {
            Logger.LogInformation("Host stopped");

            return Task.CompletedTask;
        }
    }
}