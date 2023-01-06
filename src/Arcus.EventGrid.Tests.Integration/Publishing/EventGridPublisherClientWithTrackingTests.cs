using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Core;
using Arcus.EventGrid.Tests.Core.Security;
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
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    public abstract class EventGridPublisherClientWithTrackingTests
    {
        private readonly string _transactionId = ActivityTraceId.CreateRandom().ToHexString();
        private readonly ITestOutputHelper _testOutput;

        private readonly EventSchema _eventSchema;
        private readonly InMemoryLogger _spyLogger;

        private EventOptionsFixture _optionsFixture;
        private Action<ProcessMessageEventArgs> _customAssertOperationParentIdProperty, _customAssertTransactionIdProperty, _customAssertTraceParentProperty;

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

        protected TestConfig Configuration { get; } = TestConfig.Create();

        protected async Task<EventGridTopicEndpoint> CreateEventConsumerHostWithTrackingAsync(EventCorrelationFormat format)
        {
            _testOutput.WriteLine("Create EventGrid subscription endpoint (Correlation: {0})", format);
            return await EventGridTopicEndpoint.CreateAsync(_eventSchema, Configuration, _testOutput, options =>
            {
                if (format is EventCorrelationFormat.Hierarchical)
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
                }

                if (format is EventCorrelationFormat.W3C)
                {
                    options.AddMessageAssertion(message =>
                    {
                        if (_customAssertTraceParentProperty is null)
                        {
                            var traceParent = (string) Assert.Contains("Diagnostic-Id", message.Message.ApplicationProperties);
                            Assert.False(string.IsNullOrWhiteSpace(traceParent), "Should contain non-blank 'traceparent'");
                            Assert.Contains(_transactionId, traceParent);
                        }
                    });
                }
            });
        }

        protected EventGridPublisherClient CreateRegisteredClientWithCustomOptions(EventCorrelationFormat format)
        {
            _optionsFixture = new EventOptionsFixture();
            SetupCustomCorrelationAssertions(_optionsFixture.DependencyId, format);
            return CreateRegisteredClient(format, options =>
            {
                ApplyCustomOptions(_optionsFixture.DependencyId, options);
                _optionsFixture.ApplyOptions(options);
            });
        }

        protected EventGridPublisherClient CreateRegisteredClientUsingManagedIdentityWithCustomOptions(EventCorrelationFormat format)
        {
            _optionsFixture = new EventOptionsFixture();
            SetupCustomCorrelationAssertions(_optionsFixture.DependencyId, format);
            return CreateRegisteredClientUsingManagedIdentity(format, options =>
            {
                ApplyCustomOptions(_optionsFixture.DependencyId, options);
                _optionsFixture.ApplyOptions(options);
            });
        }

        private static void ApplyCustomOptions(string dependencyId, EventGridPublisherClientWithTrackingOptions options)
        {
            options.UpstreamServicePropertyName = "customOperationParentId";
            options.TransactionIdEventDataPropertyName = "customTransactionId";
            options.TraceParentPropertyName = "customTraceparent";
            options.GenerateDependencyId = () => dependencyId;
        }

        private void SetupCustomCorrelationAssertions(string dependencyId, EventCorrelationFormat format)
        {
            if (format is EventCorrelationFormat.Hierarchical)
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
            }

            if (format is EventCorrelationFormat.W3C)
            {
                _customAssertTraceParentProperty = message =>
                {
                    var traceParent = (string) Assert.Contains("Custom-Diagnostic-Id", message.Message.ApplicationProperties);
                    Assert.False(string.IsNullOrWhiteSpace(traceParent), "Should contain non-blank 'traceparent'");
                    Assert.Equal($"00-{_transactionId}-{dependencyId}-00", traceParent);
                };
            }
        }

        protected EventGridPublisherClient CreateRegisteredClient(
            EventCorrelationFormat format,
            Action<EventGridPublisherClientWithTrackingOptions> configureOptions = null)
        {
            return CreateRegisteredClient((clients, topicEndpoint, authenticationKeySecretName) =>
            {
                clients.AddEventGridPublisherClient(topicEndpoint, authenticationKeySecretName, options =>
                {
                    options.Format = format;
                    configureOptions?.Invoke(options);
                });
            });
        }

        protected EventGridPublisherClient CreateRegisteredClientUsingManagedIdentity(
            EventCorrelationFormat format,
            Action<EventGridPublisherClientWithTrackingOptions> configureOptions = null)
        {
            return CreateRegisteredClient((clients, topicEndpoint, _) =>
            {
                clients.AddEventGridPublisherClientUsingManagedIdentity(topicEndpoint, options =>
                {
                    options.Format = format;
                    configureOptions?.Invoke(options);
                });
            });
        }

        protected EventGridPublisherClient CreateRegisteredClientWithCustomImplementation(EventCorrelationFormat format)
        {
            if (format is EventCorrelationFormat.Hierarchical)
            {
                _customAssertOperationParentIdProperty = message =>
                {
                    var operationParentId = (string)Assert.Contains("Custom-Operation-Parent-Id", message.Message.ApplicationProperties);
                    Assert.False(string.IsNullOrWhiteSpace(operationParentId), "Should contain non-blank operation parent ID");
                };
                _customAssertTransactionIdProperty = eventArgs =>
                {
                    var transactionId = (string)Assert.Contains("Custom-Transaction-Id", eventArgs.Message.ApplicationProperties);
                    Assert.False(string.IsNullOrWhiteSpace(transactionId), "Should contain non-blank transaction ID");
                    Assert.Equal(_transactionId, transactionId);
                }; 
            }

            if (format is EventCorrelationFormat.W3C)
            {
                _customAssertTraceParentProperty = message =>
                {
                    var traceParent = (string)Assert.Contains("Custom-Diagnostic-Id", message.Message.ApplicationProperties);
                    Assert.False(string.IsNullOrWhiteSpace(traceParent), "Should contain non-blank 'traceparent'");
                    Assert.Contains(_transactionId, traceParent);
                };
            }

            return CreateRegisteredClient((clients, topicEndpoint, authenticationKeySecretName) =>
            {
                clients.AddEventGridPublisherClient(provider =>
                {
                    return new CustomEventGridPublisherClientWithTracking(
                        topicEndpoint,
                        authenticationKeySecretName,
                        provider.GetRequiredService<ISecretProvider>(),
                        provider.GetRequiredService<ICorrelationInfoAccessor>(),
                        new EventGridPublisherClientWithTrackingOptions { Format = format },
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

            string topicEndpoint = Configuration.GetEventGridTopicEndpoint(_eventSchema);
            string authenticationKey = Configuration.GetEventGridEndpointKey(_eventSchema);
            string authenticationKeySecretName = "Arcus_EventGrid_AuthenticationKey";
            
            services.AddSecretStore(stores => stores.AddProvider(new StaticInMemorySecretProvider(authenticationKeySecretName, authenticationKey)));
            services.AddAzureClients(clients => registration(clients, topicEndpoint, authenticationKeySecretName));

            IServiceProvider provider = services.BuildServiceProvider();
            var correlationAccessor = provider.GetRequiredService<ICorrelationInfoAccessor>();
            correlationAccessor.SetCorrelationInfo(new CorrelationInfo("operation-id", _transactionId));
            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();

            EventGridPublisherClient client = factory.CreateClient("Default");
            return client;
        }

        protected void AssertDependencyTracking(EventCorrelationFormat format = EventCorrelationFormat.W3C)
        {
            if (format is EventCorrelationFormat.W3C)
            {
                return;
            }

            string logMessage = Assert.Single(_spyLogger.Messages, msg => msg.Contains("Azure Event Grid"));
            string topicEndpoint = Configuration.GetEventGridTopicEndpoint(_eventSchema);

            Assert.Matches($"{_eventSchema}|Custom", logMessage);
            Assert.Contains(topicEndpoint, logMessage);
            if (_optionsFixture?.DependencyId is null)
            {
                Assert.Matches(DependencyIdRegex, logMessage);
            }
            else
            {
                Assert.Contains(_optionsFixture?.DependencyId, logMessage);
            }

            _optionsFixture?.AssertTelemetry(logMessage);
        }
    }
}