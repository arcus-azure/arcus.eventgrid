using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts
{
    public class EventConsumerHost
    {
        private static readonly Dictionary<string, string> _receivedEvents = new Dictionary<string, string>();
        protected readonly ILogger _logger;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="logger">Logger to use for writing event information</param>
        public EventConsumerHost(ILogger logger)
        {
            Guard.NotNull(logger, nameof(logger));

            _logger = logger;
        }

        /// <summary>
        ///     Handles new events that are being received
        /// </summary>
        /// <param name="rawReceivedEvents">Raw payload containing all events</param>
        protected static void EventsReceived(string rawReceivedEvents)
        {
            var parsedEvents = JArray.Parse(rawReceivedEvents);
            foreach (var parsedEvent in parsedEvents)
            {
                var eventId = parsedEvent["Id"]?.ToString();

                _receivedEvents[eventId] = rawReceivedEvents;
            }
        }

        /// <summary>
        ///     Gets the event envelope that includes a requested event (Uses exponential back-off)
        /// </summary>
        /// <param name="eventId">Event id for requested event</param>
        /// <param name="retryCount">Amount of retries while waiting for the event to come in</param>
        public string GetReceivedEvent(string eventId, int retryCount = 10)
        {
            var retryPolicy = Policy.HandleResult<string>(string.IsNullOrWhiteSpace)
                .WaitAndRetry(retryCount, currentRetryCount => TimeSpan.FromSeconds(Math.Pow(2, currentRetryCount)));

            var matchingEvent = retryPolicy.Execute(() =>
            {
                _logger.LogInformation($"Received events are : {string.Join(", ", _receivedEvents.Keys)}");

                _receivedEvents.TryGetValue(eventId, out var rawEvent);
                return rawEvent;
            });

            return matchingEvent;
        }

        /// <summary>
        ///     Stop receiving traffic
        /// </summary>
        public virtual Task StopAsync()
        {
            _logger.LogInformation("Host stopped");

            return Task.CompletedTask;
        }
    }
}