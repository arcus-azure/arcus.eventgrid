using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;
using Polly;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts
{
    using Guard;

    public class HybridConnectionHost
    {
        private static readonly List<string> rawReceivedEvents = new List<string>();
        private readonly HybridConnectionListener _hybridConnectionListener;

        private HybridConnectionHost(HybridConnectionListener hybridConnectionListener)
        {
            Guard.NotNull(hybridConnectionListener, nameof(hybridConnectionListener));

            _hybridConnectionListener = hybridConnectionListener;
        }

        /// <summary>
        ///     Gets the payload for a received event (Uses exponentional backoff)
        /// </summary>
        /// <param name="eventId">Event id for requested event</param>
        /// <param name="retryCount">Amount of retries while waiting for the event to come in</param>
        public string GetReceivedEvent(string eventId, int retryCount = 5)
        {
            var retryPolicy = Policy.HandleResult<string>(string.IsNullOrWhiteSpace)
                .WaitAndRetry(retryCount, currentRetryCount => TimeSpan.FromSeconds(Math.Pow(2, currentRetryCount)));

            var matchingEvent = retryPolicy.Execute(() =>
            {
                var eventPayload = rawReceivedEvents.FirstOrDefault(rawEvent => rawEvent.Contains(eventId));
                return eventPayload;
            });

            return matchingEvent;
        }

        /// <summary>
        ///     Start receiving traffic
        /// </summary>
        public static async Task<HybridConnectionHost> Start(string relayNamespaceName, string hybridConnectionName, string accessPolicyName, string accessPolicyKey)
        {
            Guard.NotNullOrWhitespace(relayNamespaceName, nameof(relayNamespaceName));
            Guard.NotNullOrWhitespace(hybridConnectionName, nameof(hybridConnectionName));
            Guard.NotNullOrWhitespace(accessPolicyName, nameof(accessPolicyName));
            Guard.NotNullOrWhitespace(accessPolicyKey, nameof(accessPolicyKey));

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(accessPolicyName, accessPolicyKey);
            var hybridConnectionListener = new HybridConnectionListener(new Uri(string.Format(format: "sb://{0}/{1}", arg0: relayNamespaceName, arg1: hybridConnectionName)), tokenProvider);

            hybridConnectionListener.Connecting += (o, e) => { Console.WriteLine(value: "Connecting"); };
            hybridConnectionListener.Offline += (o, e) => { Console.WriteLine(value: "Offline"); };
            hybridConnectionListener.Online += (o, e) => { Console.WriteLine(value: "Online"); };

            hybridConnectionListener.RequestHandler = context =>
            {
                using (var requestStreamReader = new StreamReader(context.Request.InputStream))
                {
                    var request = requestStreamReader.ReadToEnd();
                    Console.WriteLine($"Request - {request}");
                    rawReceivedEvents.Add(request);
                }

                // The context MUST be closed here
                context.Response.Close();
            };

            await hybridConnectionListener.OpenAsync();

            return new HybridConnectionHost(hybridConnectionListener);
        }

        /// <summary>
        ///     Stop receiving traffic
        /// </summary>
        public async Task Stop()
        {
            await _hybridConnectionListener.CloseAsync();
        }
    }
}