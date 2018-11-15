using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Arcus.EventGrid.Testing.Logging;
using GuardNet;
using Microsoft.Azure.Relay;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts
{
    public class HybridConnectionHost
    {
        private static readonly Dictionary<string, string> receivedEvents = new Dictionary<string, string>();
        private readonly HybridConnectionListener _hybridConnectionListener;
        private readonly ILogger _logger;

        private HybridConnectionHost(HybridConnectionListener hybridConnectionListener, ILogger logger)
        {
            Guard.NotNull(hybridConnectionListener, nameof(hybridConnectionListener));
            Guard.NotNull(logger, nameof(logger));

            _logger = logger;
            _hybridConnectionListener = hybridConnectionListener;
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
                _logger.LogInformation($"Received events are : {string.Join(", ", receivedEvents.Keys)}");

                receivedEvents.TryGetValue(eventId, out var rawEvent);
                return rawEvent;
            });

            return matchingEvent;
        }

        /// <summary>
        ///     Start receiving traffic
        /// </summary>
        /// <param name="relayNamespaceName">Name of the Azure Relay namespace</param>
        /// <param name="hybridConnectionName">Name of the Azure Relay Hybrid Connection</param>
        /// <param name="accessPolicyName">Name of the access policy</param>
        /// <param name="accessPolicyKey">Key of the access policy to authenticate with</param>
        public static async Task<HybridConnectionHost> Start(string relayNamespaceName, string hybridConnectionName, string accessPolicyName, string accessPolicyKey)
        {
            var hybridConnectionHost = await Start(relayNamespaceName, hybridConnectionName, accessPolicyName, accessPolicyKey, new NoOpLogger());
            return hybridConnectionHost;
        }

        /// <summary>
        ///     Start receiving traffic
        /// </summary>
        /// <param name="relayNamespaceName">Name of the Azure Relay namespace</param>
        /// <param name="hybridConnectionName">Name of the Azure Relay Hybrid Connection</param>
        /// <param name="accessPolicyName">Name of the access policy</param>
        /// <param name="accessPolicyKey">Key of the access policy to authenticate with</param>
        /// <param name="logger">Logger to use for writing event information during the hybrid connection</param>
        public static async Task<HybridConnectionHost> Start(string relayNamespaceName, string hybridConnectionName, string accessPolicyName, string accessPolicyKey, ILogger logger)
        {
            Guard.NotNullOrWhitespace(relayNamespaceName, nameof(relayNamespaceName));
            Guard.NotNullOrWhitespace(hybridConnectionName, nameof(hybridConnectionName));
            Guard.NotNullOrWhitespace(accessPolicyName, nameof(accessPolicyName));
            Guard.NotNullOrWhitespace(accessPolicyKey, nameof(accessPolicyKey));
            Guard.NotNull(logger, nameof(logger));

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(accessPolicyName, accessPolicyKey);
            var hybridConnectionUri = string.Format(format: "sb://{0}/{1}", arg0: relayNamespaceName, arg1: hybridConnectionName);
            var hybridConnectionListener = new HybridConnectionListener(new Uri(hybridConnectionUri), tokenProvider);

            hybridConnectionListener.Connecting += (o, e) => { logger.LogInformation("Connecting to Azure Relay Hybrid Connections"); };
            hybridConnectionListener.Offline += (o, e) => { logger.LogInformation("Azure Relay Hybrid Connections listener is offline"); };
            hybridConnectionListener.Online += (o, e) => { logger.LogInformation("Azure Relay Hybrid Connections listener is online"); };

            hybridConnectionListener.RequestHandler = relayedHttpListenerContext => HandleReceivedRequest(relayedHttpListenerContext, logger);

            logger.LogInformation($"Host connecting to {hybridConnectionUri}");
            await hybridConnectionListener.OpenAsync();

            return new HybridConnectionHost(hybridConnectionListener, logger);
        }

        private static void HandleReceivedRequest(RelayedHttpListenerContext context, ILogger logger)
        {
            using (var requestStreamReader = new StreamReader(context.Request.InputStream))
            {
                var rawEvents = requestStreamReader.ReadToEnd();

                logger.LogInformation($"New request was received - {rawEvents}");
                StoreReceivedEvents(rawEvents, logger);
            }

            // The context MUST be closed here
            context.Response.Close();
        }

        /// <summary>
        ///     Stop receiving traffic
        /// </summary>
        public async Task Stop()
        {
            _logger.LogInformation("Stopping host");
            await _hybridConnectionListener.CloseAsync();
        }

        private static void StoreReceivedEvents(string rawEvents, ILogger logger)
        {
            try
            {
                var parsedEvents = JArray.Parse(rawEvents);
                foreach (var parsedEvent in parsedEvents)
                {
                    var eventId = parsedEvent["Id"]?.ToString();
                    receivedEvents[eventId] = rawEvents;
                }
            }
            catch (Exception)
            {
                logger.LogError($"Failed to persist raw events - {rawEvents}");
            }
        }
    }
}