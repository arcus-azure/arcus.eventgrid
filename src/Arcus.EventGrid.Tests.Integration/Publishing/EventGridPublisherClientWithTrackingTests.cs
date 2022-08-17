using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Tests.Integration.Fixture;
using Arcus.EventGrid.Tests.Integration.Publishing.Fixture;
using Arcus.Observability.Correlation;
using Arcus.Security.Core;
using Arcus.Testing.Logging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    public abstract class EventGridPublisherClientWithTrackingTests
    {
        private readonly string _transactionId = $"transaction-{Guid.NewGuid()}";
        private readonly TestConfig _config = TestConfig.Create();
        private readonly ITestOutputHelper _testOutput;

        private readonly EventSchema _eventSchema;
        private readonly InMemoryLogger _spyLogger;

        private Action<ProcessMessageEventArgs> _customAssertOperationParentIdProperty, _customAssertTransactionIdProperty;

        private static readonly Regex DependencyIdRegex = new Regex(@"ID [a-z0-9]{8}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{12}", RegexOptions.Compiled);
        protected static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisherClientWithTrackingTests"/> class.
        /// </summary>
        protected EventGridPublisherClientWithTrackingTests(
            EventSchema eventSchema,
            ITestOutputHelper testOutput)
        {
            _eventSchema = eventSchema;
            _testOutput = testOutput;
            _spyLogger = new InMemoryLogger();
        }

        protected async Task<EventGridTopicEndpoint> CreateEventConsumerHostWithTrackingAsync()
        {
            return await EventGridTopicEndpoint.CreateAsync(_eventSchema, _config, _testOutput, options =>
            {
                options.AddMessageAssertion(message =>
                {
                    if (_customAssertOperationParentIdProperty is null)
                    {
                        var operationParentId = (string) Assert.Contains("Operation-Parent-Id", message.Message.ApplicationProperties);
                        Assert.False(string.IsNullOrWhiteSpace(operationParentId), "Should contain non-blank operation parent ID");
                    }
                    else
                    {
                        _customAssertOperationParentIdProperty(message);
                    }
                });
                options.AddMessageAssertion(message =>
                {
                    if (_customAssertTransactionIdProperty is null)
                    {
                        var transactionId = (string) Assert.Contains("Transaction-Id", message.Message.ApplicationProperties);
                        Assert.False(string.IsNullOrWhiteSpace(transactionId), "Should contain non-blank transaction ID");
                        Assert.Equal(_transactionId, transactionId);
                    }
                    else
                    {
                        _customAssertTransactionIdProperty(message);
                    }
                });
            });
        }

        protected EventGridPublisherClient CreateRegisteredClientWithCustomOptions(string dependencyId, Dictionary<string, object> telemetryContext)
        {
            _customAssertOperationParentIdProperty = message =>
            {
                var operationParentId = (string) Assert.Contains("Custom-Operation-Parent-Id", message.Message.ApplicationProperties);
                Assert.False(string.IsNullOrWhiteSpace(operationParentId), "Should contain non-blank operation parent ID");
                Assert.Equal(dependencyId, operationParentId);
            };
            _customAssertTransactionIdProperty = eventArgs =>
            {
                var transactionId = (string) Assert.Contains("Custom-Transaction-Id", eventArgs.Message.ApplicationProperties);
                Assert.False(string.IsNullOrWhiteSpace(transactionId), "Should contain non-blank transaction ID");
                Assert.Equal(_transactionId, transactionId);
            };

            return CreateRegisteredClient(options =>
            {
                options.UpstreamServicePropertyName = "customOperationParentId";
                options.TransactionIdEventDataPropertyName = "customTransactionId";
                options.GenerateDependencyId = () => dependencyId;
                options.AddTelemetryContext(telemetryContext);
            });
        }

        protected EventGridPublisherClient CreateRegisteredClient(
            Action<EventGridPublisherClientWithTrackingOptions> configureOptions = null)
        {
            return CreateRegisteredClient((clients, topicEndpoint, authenticationKeySecretName) =>
            {
                clients.AddEventGridPublisherClient(topicEndpoint, authenticationKeySecretName, configureOptions);
            });
        }

        protected EventGridPublisherClient CreateRegisteredClientWithCustomImplementation()
        {
            _customAssertOperationParentIdProperty = message =>
            {
                var operationParentId = (string) Assert.Contains("Custom-Operation-Parent-Id", message.Message.ApplicationProperties);
                Assert.False(string.IsNullOrWhiteSpace(operationParentId), "Should contain non-blank operation parent ID");
            };
            _customAssertTransactionIdProperty = eventArgs =>
            {
                var transactionId = (string) Assert.Contains("Custom-Transaction-Id", eventArgs.Message.ApplicationProperties);
                Assert.False(string.IsNullOrWhiteSpace(transactionId), "Should contain non-blank transaction ID");
                Assert.Equal(_transactionId, transactionId);
            };

            return CreateRegisteredClient((clients, topicEndpoint, authenticationKeySecretName) =>
            {
                clients.AddEventGridPublisherClient(provider =>
                {
                    return new CustomEventGridPublisherClientWithTracking(
                        topicEndpoint,
                        authenticationKeySecretName,
                        provider.GetRequiredService<ISecretProvider>(),
                        provider.GetRequiredService<ICorrelationInfoAccessor>(),
                        new EventGridPublisherClientWithTrackingOptions(),
                        provider.GetRequiredService<ILogger<EventGridPublisherClient>>());
                });
            });
        }

        protected EventGridPublisherClient CreateRegisteredClient(
            Action<AzureClientFactoryBuilder, string, string> registration)
        {
            var services = new ServiceCollection();
            services.AddCorrelation();
            services.AddLogging(logging => logging.AddProvider(new CustomLoggerProvider(_spyLogger)));

            string topicEndpoint = _config.GetEventGridTopicEndpoint(_eventSchema);
            string authenticationKey = _config.GetEventGridEndpointKey(_eventSchema);
            string authenticationKeySecretName = "Arcus_EventGrid_AuthenticationKey";
            
            services.AddSecretStore(stores => stores.AddInMemory(authenticationKeySecretName, authenticationKey));
            services.AddAzureClients(clients => registration(clients, topicEndpoint, authenticationKeySecretName));

            IServiceProvider provider = services.BuildServiceProvider();
            var correlationAccessor = provider.GetRequiredService<ICorrelationInfoAccessor>();
            correlationAccessor.SetCorrelationInfo(new CorrelationInfo("operation-id", _transactionId));
            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();

            EventGridPublisherClient client = factory.CreateClient("Default");
            return client;
        }

        protected string AssertDependencyTracking(string dependencyId = null)
        {
            string logMessage = Assert.Single(_spyLogger.Messages, msg => msg.Contains("Azure Event Grid"));
            string topicEndpoint = _config.GetEventGridTopicEndpoint(_eventSchema);

            Assert.Matches($"{_eventSchema}|Custom", logMessage);
            Assert.Contains(topicEndpoint, logMessage);
            if (dependencyId is null)
            {
                Assert.Matches(DependencyIdRegex, logMessage);
            }
            else
            {
                Assert.Contains(dependencyId, logMessage);
            }

            return logMessage;
        }
    }
}