using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.InteropExtensions;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts
{
    public class EventConsumerHost
    {
        private static readonly Dictionary<string, string> _receivedEvents = new Dictionary<string, string>();
        private readonly SubscriptionClient _subscriptionClient;
        private readonly ManagementClient _managementClient;
        private readonly ILogger _logger;

        private static bool _isHostShuttingDown = false;

        private EventConsumerHost(string topicPath, string subscriptionName, SubscriptionClient subscriptionClient, ManagementClient managementClient, ILogger logger)
        {
            Guard.NotNullOrWhitespace(topicPath, nameof(topicPath));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));
            Guard.NotNull(subscriptionClient, nameof(subscriptionClient));
            Guard.NotNull(managementClient, nameof(managementClient));
            Guard.NotNull(logger, nameof(logger));

            TopicPath = topicPath;
            SubscriptionName = subscriptionName;
            _subscriptionClient = subscriptionClient;
            _managementClient = managementClient;
            _logger = logger;
        }

        /// <summary>
        ///     Path of the topic relative to the namespace base address.
        /// </summary>
        public string TopicPath { get; }

        /// <summary>
        ///     Name of the subscription that was created
        /// </summary>
        public string SubscriptionName { get; }

        /// <summary>
        ///     Start receiving traffic
        /// </summary>
        /// <param name="topicPath">Path of the topic relative to the namespace base address</param>
        /// <param name="serviceBusConnectionString">
        ///     Connection string of the Azure Service Bus namespace to use to consume
        ///     messages
        /// </param>
        /// <param name="logger">Logger to use for writing event information during the hybrid connection</param>
        public static async Task<EventConsumerHost> Start(string topicPath, string serviceBusConnectionString, ILogger logger)
        {
            Guard.NotNullOrWhitespace(topicPath, nameof(topicPath));
            Guard.NotNullOrWhitespace(serviceBusConnectionString, nameof(serviceBusConnectionString));
            Guard.NotNull(logger, nameof(logger));

            var managementClient = new ManagementClient(serviceBusConnectionString);

            var subscriptionName = $"Test-{Guid.NewGuid().ToString()}";
            await CreateSubscriptionAsync(topicPath, managementClient, subscriptionName);

            var subscriptionClient = new SubscriptionClient(serviceBusConnectionString, topicPath, subscriptionName);
            StartMessagePump(subscriptionClient, logger);

            return new EventConsumerHost(topicPath, subscriptionName, subscriptionClient, managementClient, logger);
        }

        /// <summary>
        ///     Stop receiving traffic
        /// </summary>
        public async Task Stop()
        {
            _logger.LogInformation("Stopping host");
            _isHostShuttingDown = true;

            await _managementClient.DeleteSubscriptionAsync(TopicPath, SubscriptionName);
            _logger.LogInformation($"Subscription '{SubscriptionName}' deleted on topic '{TopicPath}'");

            await _subscriptionClient.CloseAsync();
            _logger.LogInformation("Host stopped");
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

        private static void StartMessagePump(SubscriptionClient subscriptionClient, ILogger logger)
        {
            var messageHandlerOptions = new MessageHandlerOptions(exceptionReceivedEventArgs => HandleException(exceptionReceivedEventArgs, logger))
            {
                AutoComplete = false,
                MaxConcurrentCalls = 10
            };

            subscriptionClient.RegisterMessageHandler((receivedMessage, cancellationToken) => HandleNewMessage(receivedMessage, subscriptionClient, cancellationToken, logger), messageHandlerOptions);
        }

        private static async Task HandleNewMessage(Message receivedMessage, SubscriptionClient subscriptionClient, CancellationToken cancellationToken, ILogger logger)
        {
            if (receivedMessage == null || _isHostShuttingDown)
            {
                return;
            }

            var rawReceivedEvents = Encoding.UTF8.GetString(receivedMessage.Body);

            try
            {
                var parsedEvents = JArray.Parse(rawReceivedEvents);
                foreach (var parsedEvent in parsedEvents)
                {
                    var eventId = parsedEvent["Id"]?.ToString();
                    _receivedEvents[eventId] = rawReceivedEvents;
                }

                await subscriptionClient.CompleteAsync(receivedMessage.SystemProperties.LockToken);
            }
            catch (Exception)
            {
                logger.LogError($"Failed to persist raw events - {rawReceivedEvents}");
            }
        }

        private static Task HandleException(ExceptionReceivedEventArgs exceptionReceivedEventArgs, ILogger logger)
        {
            logger.LogCritical(exceptionReceivedEventArgs.Exception.Message);
            return Task.CompletedTask;
        }

        private static async Task CreateSubscriptionAsync(string topicPath, ManagementClient managementClient, string subscriptionName)
        {
            var subscriptionDescription = new SubscriptionDescription(topicPath, subscriptionName)
            {
                AutoDeleteOnIdle = TimeSpan.FromHours(1),
                MaxDeliveryCount = 3,
                UserMetadata = "Subscription created by Arcus in order to run integration tests"
            };

            var ruleDescription = new RuleDescription("Accept All", new TrueFilter());

            await managementClient.CreateSubscriptionAsync(subscriptionDescription, ruleDescription);
        }
    }
}