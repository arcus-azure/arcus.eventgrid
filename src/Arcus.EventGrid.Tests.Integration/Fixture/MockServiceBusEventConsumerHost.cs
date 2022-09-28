﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid.Testing.Infrastructure.Hosts;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Sdk;

namespace Arcus.EventGrid.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents an event consumer host for receiving Azure Event Grid events via Azure Logic Apps &amp; Service Bus Topics
    /// </summary>
    public class MockServiceBusEventConsumerHost : EventConsumerHost
    {
        private readonly MockServiceBusEventConsumerHostOptions _consumerOptions;
        private readonly ServiceBusProcessor _topicProcessor;
        private readonly ServiceBusAdministrationClient _managementClient;
        private readonly IDisposable _loggingScope;
        private readonly SubscriptionBehavior _subscriptionBehavior;
        private readonly ICollection<Exception> _exceptions = new Collection<Exception>();

        private MockServiceBusEventConsumerHost(
            MockServiceBusEventConsumerHostOptions consumerHostOptions, 
            ServiceBusProcessor topicProcessor, 
            ServiceBusAdministrationClient managementClient, 
            ILogger logger,
            IDisposable loggingScope) : base(logger)
        {
            Guard.NotNull(consumerHostOptions, nameof(consumerHostOptions));
            Guard.NotNull(topicProcessor, nameof(topicProcessor));
            Guard.NotNull(managementClient, nameof(managementClient));

            TopicPath = consumerHostOptions.TopicPath;
            SubscriptionName = consumerHostOptions.SubscriptionName;

            _subscriptionBehavior = consumerHostOptions.SubscriptionBehavior;
            _consumerOptions = consumerHostOptions;
            _topicProcessor = topicProcessor;
            _managementClient = managementClient;
            _loggingScope = loggingScope;
        }

        /// <summary>
        /// Gets the unique ID to identify this Azure Service Bus topic consumer host from other test infrastructure.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets the path of the Azure Service Bus Topic relative to the Azure Service Bus namespace base address.
        /// </summary>
        public string TopicPath { get; }

        /// <summary>
        /// Gets the name of the Azure Service Bus topic subscription that was created.
        /// </summary>
        public string SubscriptionName { get; }

        /// <summary>
        /// Starts the Azure Service Bus topic consumer host so it can start receiving traffic.
        /// </summary>
        /// <param name="consumerOptions">
        ///     The configuration options that indicate what Azure Service Bus entities to use and how they should behave.
        /// </param>
        /// <param name="logger">The logger instance for writing event information upon received events.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="consumerOptions"/> is <c>null</c>.</exception>
        public static async Task<MockServiceBusEventConsumerHost> StartAsync(
            MockServiceBusEventConsumerHostOptions consumerOptions, 
            ILogger logger)
        {
            Guard.NotNull(consumerOptions, nameof(consumerOptions), "Requires a set of Azure Service Bus consumer host options to start the consumer host to receive traffic");
            logger = logger ?? NullLogger.Instance;
            
            string jobId = Guid.NewGuid().ToString();
            IDisposable loggingScope = logger.BeginScope("Test consumer host: {JobId}", jobId);

            logger.LogTrace("Starting Azure Service Bus Topic event consumer host...");
            var managementClient = new ServiceBusAdministrationClient(consumerOptions.ConnectionString);
            await CreateSubscriptionAsync(managementClient, consumerOptions.TopicPath, consumerOptions.SubscriptionName)
                .ConfigureAwait(continueOnCapturedContext: false);
            logger.LogInformation("Created subscription '{subscription}' on topic '{topic}'", consumerOptions.SubscriptionName, consumerOptions.TopicPath);

            ServiceBusProcessor topicProcessor = CreateServiceBusProcessor(consumerOptions);
            
            var consumerHost = new MockServiceBusEventConsumerHost(consumerOptions, topicProcessor, managementClient, logger, loggingScope);
            await consumerHost.StartProcessingMessagesAsync(topicProcessor);
            
            return consumerHost;
        }

        private static ServiceBusProcessor CreateServiceBusProcessor(ServiceBusEventConsumerHostOptions consumerOptions)
        {
            var serviceBusClient = new ServiceBusClient(consumerOptions.ConnectionString);
            var processorOptions = new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 10
            };
            
            ServiceBusProcessor topicProcessor = serviceBusClient.CreateProcessor(consumerOptions.TopicPath, consumerOptions.SubscriptionName, processorOptions);
            return topicProcessor;
        }
        
        private async Task StartProcessingMessagesAsync(ServiceBusProcessor topicProcessor)
        {
            topicProcessor.ProcessMessageAsync += HandleNewMessageAsync;
            topicProcessor.ProcessErrorAsync += HandleExceptionAsync;
            await topicProcessor.StartProcessingAsync();
            
            Logger.LogInformation("Azure Service Bus event consumer host started on '{SubscriptionName}' (topic '{TopicPath}' for endpoint '{ServiceBusEndpoint}')", SubscriptionName, TopicPath, topicProcessor.FullyQualifiedNamespace);
        }

        private async Task HandleNewMessageAsync(ProcessMessageEventArgs eventArgs)
        {
            if (eventArgs?.Message is null)
            {
                return;
            }

            ServiceBusReceivedMessage receivedMessage = eventArgs.Message;
            Logger.LogInformation("Message '{messageId}' was received", receivedMessage.MessageId);

            string rawReceivedEvents = string.Empty;
            try
            {
                _consumerOptions.AssertMessage(eventArgs);

                rawReceivedEvents = receivedMessage.Body.ToString();
                EventsReceived(rawReceivedEvents);

                Logger.LogInformation("Message '{messageId}' was successfully handled", receivedMessage.MessageId);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to persist raw events with exception '{exceptionMessage}'. Payload: {rawEventsPayload}", exception.Message, rawReceivedEvents);
                _exceptions.Add(exception);
            }
            finally
            {
                await eventArgs.CompleteMessageAsync(eventArgs.Message);
            }
        }

        private Task HandleExceptionAsync(ProcessErrorEventArgs eventArgs)
        {
            Logger.LogCritical(eventArgs.Exception, "Failed to process Azure Service Bus message due to an exception '{Message}'", eventArgs.Exception.Message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the event envelope that includes a requested event (uses exponential back-off) or fail when a failure occurred during retrieval.
        /// </summary>
        /// <param name="eventId">The vent ID for requested event.</param>
        /// <returns>
        ///     The raw received event contents with the given <paramref name="eventId"/>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventId"/> is blank.</exception>
        /// <exception cref="XunitException">Thrown when a single assertion failure occurred during retrieval.</exception>
        /// <exception cref="AggregateException">Thrown when multiple failures happened during retrieval.</exception>
        public string GetReceivedEventOrFail(string eventId)
        {
            string receivedEvent = GetReceivedEvent(eventId);
            if (_exceptions.Count == 1)
            {
                throw _exceptions.ElementAt(0);
            }

            if (_exceptions.Count > 1)
            {
                throw new AggregateException("Failed to receive event due to a failure during retrieval", _exceptions);
            }

            return receivedEvent;
        }

        /// <summary>
        /// Stops the Azure Service Bus topic consumer host from receiving traffic.
        /// </summary>
        public override async Task StopAsync()
        {
            Logger.LogTrace("Stopping Azure Service Bus Topic consumer host...");

            await DeleteSubscriptionAsync();
            await StopProcessingMessages();
            await base.StopAsync();
        }

        private async Task DeleteSubscriptionAsync()
        {
            if (_subscriptionBehavior == SubscriptionBehavior.DeleteOnClosure)
            {
                await _managementClient.DeleteSubscriptionAsync(TopicPath, SubscriptionName)
                                       .ConfigureAwait(continueOnCapturedContext: false);
                
                Logger.LogTrace("Subscription '{SubscriptionName}' deleted on topic '{TopicPath}'", SubscriptionName, TopicPath);
            }
        }

        private async Task StopProcessingMessages()
        {
            await _topicProcessor.StopProcessingAsync();
            _topicProcessor.ProcessErrorAsync -= HandleExceptionAsync;
            _topicProcessor.ProcessMessageAsync -= HandleNewMessageAsync;
            await _topicProcessor.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
        }

        private static async Task CreateSubscriptionAsync(
            ServiceBusAdministrationClient managementClient,
            string topicPath,
            string subscriptionName)
        {
            var createSubscriptionOptions = new CreateSubscriptionOptions(topicPath, subscriptionName)
            {
                AutoDeleteOnIdle = TimeSpan.FromHours(1),
                MaxDeliveryCount = 3,
                UserMetadata = "Subscription created by Arcus in order to run integration tests"
            };

            var createRuleOptions = new CreateRuleOptions("Accept-All", new TrueRuleFilter());

            await managementClient.CreateSubscriptionAsync(createSubscriptionOptions, createRuleOptions)
                                  .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}