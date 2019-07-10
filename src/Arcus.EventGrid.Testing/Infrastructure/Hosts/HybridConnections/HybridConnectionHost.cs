using System;
using System.IO;
using System.Threading.Tasks;
using Arcus.EventGrid.Testing.Logging;
using GuardNet;
using Microsoft.Azure.Relay;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace for backwards compatibility reasons
namespace Arcus.EventGrid.Testing.Infrastructure.Hosts
{
    /// <summary>
    ///     Event consumer host for receiving Azure Event Grid events via Azure Relay with Hybrid Connections
    /// </summary>
    [Obsolete("Use ServiceBusEventConsumerHost instead. Azure Relay with Hybrid Connections uses a round-robin approach and does not work nicely when running integration tests on multiple machines.")]
    public class HybridConnectionHost : EventConsumerHost
    {
        private readonly HybridConnectionListener _hybridConnectionListener;

        private HybridConnectionHost(HybridConnectionListener hybridConnectionListener, ILogger logger)
            : base(logger)
        {
            Guard.NotNull(hybridConnectionListener, nameof(hybridConnectionListener));

            _hybridConnectionListener = hybridConnectionListener;
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

        /// <summary>
        ///     Stop receiving traffic
        /// </summary>
        public override async Task StopAsync()
        {
            await _hybridConnectionListener.CloseAsync();

            await base.StopAsync();
        }

        private static void HandleReceivedRequest(RelayedHttpListenerContext context, ILogger logger)
        {
            using (var requestStreamReader = new StreamReader(context.Request.InputStream))
            {
                var rawEvents = requestStreamReader.ReadToEnd();

                logger.LogInformation("New request was received - {rawEvents}", rawEvents);
                StoreReceivedEvents(rawEvents, logger);
            }

            // The context MUST be closed here
            context.Response.Close();
        }

        private static void StoreReceivedEvents(string rawEvents, ILogger logger)
        {
            try
            {
                EventsReceived(rawEvents, logger);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to persist raw events with exception '{exceptionMessage}'. Payload: {rawEventsPayload}", ex.Message, rawEvents);
            }
        }
    }
}