using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus
{
    /// <summary>
    ///     Event consumer host for receiving Azure Event Grid events via Azure Logic Apps & Service Bus Topics
    /// </summary>
    public class ServiceBusEventConsumerHost : EventConsumerHost
    {
        private readonly SubscriptionClient _subscriptionClient;
        private readonly ManagementClient _managementClient;
        private readonly SubscriptionBehavior _subscriptionBehavior;
        public string Id { get; } = Guid.NewGuid().ToString();

        private ServiceBusEventConsumerHost(ServiceBusEventConsumerHostOptions consumerHostOptions, string subscriptionName, SubscriptionClient subscriptionClient, ManagementClient managementClient, ILogger logger)
            : base(logger)
        {
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));
            Guard.NotNull(consumerHostOptions, nameof(consumerHostOptions));
            Guard.NotNull(subscriptionClient, nameof(subscriptionClient));
            Guard.NotNull(managementClient, nameof(managementClient));

            TopicPath = consumerHostOptions.TopicPath;
            SubscriptionName = subscriptionName;

            _subscriptionBehavior = consumerHostOptions.SubscriptionBehavior;
            _subscriptionClient = subscriptionClient;
            _managementClient = managementClient;
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
        /// <param name="consumerHostOptions">
        ///     Configuration options that indicate what Service Bus entities to use and how they should behave
        /// </param>
        /// <param name="logger">Logger to use for writing event information during the hybrid connection</param>
        public static async Task<ServiceBusEventConsumerHost> Start(ServiceBusEventConsumerHostOptions consumerHostOptions, ILogger logger)
        {
            Guard.NotNull(consumerHostOptions, nameof(consumerHostOptions));
            Guard.NotNull(logger, nameof(logger));

            logger.LogInformation("Starting Service Bus event consumer host");

            var managementClient = new ManagementClient(consumerHostOptions.ConnectionString);

            var subscriptionName = $"Test-{Guid.NewGuid().ToString()}";
            await CreateSubscriptionAsync(consumerHostOptions.TopicPath, managementClient, subscriptionName).ConfigureAwait(continueOnCapturedContext: false);
            logger.LogInformation("Created subscription '{subscription}' on topic '{topic}'", subscriptionName, consumerHostOptions.TopicPath);

            var subscriptionClient = new SubscriptionClient(consumerHostOptions.ConnectionString, consumerHostOptions.TopicPath, subscriptionName);
            StartMessagePump(subscriptionClient, logger);
            logger.LogInformation("Message pump started on '{SubscriptionName}' (topic '{TopicPath}' for endpoint '{ServiceBusEndpoint}')", subscriptionName, consumerHostOptions.TopicPath, subscriptionClient.ServiceBusConnection?.Endpoint?.AbsoluteUri);

            return new ServiceBusEventConsumerHost(consumerHostOptions, subscriptionName, subscriptionClient, managementClient, logger);
        }

        /// <summary>
        ///     Stop receiving traffic
        /// </summary>
        public override async Task Stop()
        {
            Logger.LogInformation("Stopping host");

            if (_subscriptionBehavior == SubscriptionBehavior.DeleteOnClosure)
            {
                await _managementClient.DeleteSubscriptionAsync(TopicPath, SubscriptionName).ConfigureAwait(continueOnCapturedContext: false);
                Logger.LogInformation("Subscription '{SubscriptionName}' deleted on topic '{TopicPath}'", SubscriptionName, TopicPath);
            }

            await _subscriptionClient.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);

            await base.Stop();
        }

        private static void StartMessagePump(SubscriptionClient subscriptionClient, ILogger logger)
        {
            var messageHandlerOptions = new MessageHandlerOptions(async exceptionReceivedEventArgs => await HandleException(exceptionReceivedEventArgs, logger))
            {
                AutoComplete = false,
                MaxConcurrentCalls = 10
            };

            subscriptionClient.RegisterMessageHandler(async (receivedMessage, cancellationToken) => await HandleNewMessage(receivedMessage, subscriptionClient, cancellationToken, logger), messageHandlerOptions);
        }

        private static async Task HandleNewMessage(Message receivedMessage, SubscriptionClient subscriptionClient, CancellationToken cancellationToken, ILogger logger)
        {
            if (receivedMessage == null)
            {
                return;
            }

            logger.LogInformation("Message '{messageId}' was received", receivedMessage.MessageId);

            string rawReceivedEvents = string.Empty;
            try
            {
                rawReceivedEvents = Encoding.UTF8.GetString(receivedMessage.Body);
                EventsReceived(rawReceivedEvents);

                await subscriptionClient.CompleteAsync(receivedMessage.SystemProperties.LockToken).ConfigureAwait(continueOnCapturedContext: false);

                logger.LogInformation("Message '{messageId}' was successfully handled", receivedMessage.MessageId);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to persist raw events with exception '{exceptionMessage}'. Payload: {rawEventsPayload}", ex.Message, rawReceivedEvents);
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

            await managementClient.CreateSubscriptionAsync(subscriptionDescription, ruleDescription).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}