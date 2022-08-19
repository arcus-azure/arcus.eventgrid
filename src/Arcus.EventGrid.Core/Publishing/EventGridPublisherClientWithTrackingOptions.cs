using System;
using System.Collections.Generic;
using GuardNet;
using Microsoft.Extensions.Azure;
using Polly;
using Polly.CircuitBreaker;
using Polly.NoOp;
using Polly.Retry;

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.EventGrid
{
    /// <summary>
    /// Represents additional options that can be configured during the registration of the <see cref="EventGridPublisherClient"/> (<see cref="AzureClientFactoryBuilderExtensions.AddEventGridPublisherClient(AzureClientFactoryBuilder,string,string,Action{EventGridPublisherClientWithTrackingOptions})"/>)
    /// to influence the correlation tracking during event publishing or influence the internal HTTP request which represents the publishing event.
    /// </summary>
    public class EventGridPublisherClientWithTrackingOptions : EventGridPublisherClientOptions
    {
        private string _upstreamServicePropertyName = "operationParentId", _transactionIdPropertyName = "transactionId";
        private Func<string> _generateDependencyId = () => Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the name of the JSON property that represents the transaction ID that will be added to the event data of the published event (default: transactionId).
        /// The transaction ID will be available as 'data.transactionId' (if default configured) and can be used as dynamic event delivery property: <a href="https://docs.microsoft.com/en-us/azure/event-grid/delivery-properties" />.
        /// </summary>
        /// <remarks>
        ///     Make sure that the system processing the event (Azure Service Bus, web hook, etc.) can retrieve this property so that the correlation holds.
        ///     When using Arcus' messaging, the event header name for the dynamic event delivery property should be: 'Transaction-Id'.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string TransactionIdEventDataPropertyName
        {
            get => _transactionIdPropertyName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank JSON property name to add the transaction ID to the event data of the published event");
                _transactionIdPropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the JSON property that represents the operation parent ID that will be added to the event data of the published event (default: operationParentId).
        /// The operation parent ID will be available as 'data.operationParentId' (if default configured) and can be used as dynamic event delivery property: <a href="https://docs.microsoft.com/en-us/azure/event-grid/delivery-properties" />.
        /// </summary>
        /// <remarks>
        ///     Make sure that the system processing the event (Azure Service Bus, web hook, etc.) can retrieve this property so that the correlation holds.
        ///     When using Arcus' messaging, the event header name for the dynamic event delivery property should be: 'Operation-Parent-Id'.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string UpstreamServicePropertyName
        {
            get => _upstreamServicePropertyName;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank JSON property name to add the operation parent ID to the event data of the published event");
                _upstreamServicePropertyName = value;
            }
        }

        /// <summary>
        /// Gets or sets the function to generate the dependency ID used when tracking the event publishing.
        /// This value corresponds with the operation parent ID on the receiver side, and is called the dependency ID on this side (sender).
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="value"/> is <c>null</c>.</exception>
        public Func<string> GenerateDependencyId
        {
            get => _generateDependencyId;
            set
            {
                Guard.NotNull(value, nameof(value), "Requires a function to generate the dependency ID used when tracking the event publishing");
                _generateDependencyId = value;
            }
        }

        /// <summary>
        /// Gets the telemetry context used during event publishing dependency tracking.
        /// </summary>
        internal Dictionary<string, object> TelemetryContext { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Adds a telemetry context while tracking the Azure Event Grid dependency.
        /// </summary>
        /// <param name="telemetryContext">The dictionary with the contextual information about the event publishing dependency tracking.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="telemetryContext"/> is <c>null</c>.</exception>
        public void AddTelemetryContext(Dictionary<string, object> telemetryContext)
        {
            Guard.NotNull(telemetryContext, nameof(telemetryContext), "Requires a telemetry context dictionary to add to the event publishing dependency tracking");
            foreach (KeyValuePair<string, object> item in telemetryContext)
            {
                TelemetryContext[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// <para>Gets or sets the flag indicating whether or not the <see cref="EventGridPublisherClient"/> should track the Azure Event Grid topic dependency.</para>
        /// <para>For more information about dependency tracking <see href="https://observability.arcus-azure.net/features/writing-different-telemetry-types#dependencies"/>.</para>
        /// </summary>
        public bool EnableDependencyTracking { get; set; } = true;

        /// <summary>
        /// Gets the configured resilient policy when publishing asynchronously publishing events.
        /// </summary>
        internal AsyncPolicy AsyncPolicy { get; private set; } = Policy.NoOpAsync();

        /// <summary>
        /// Gets the configured resilient policy when synchronously publishing events.
        /// </summary>
        internal Policy SyncPolicy { get; private set; } = Policy.NoOp();

        /// <summary>
        /// Makes the <see cref="EventGridPublisherClient" /> resilient by retrying <paramref name="retryCount" /> times with exponential back-off.
        /// </summary>
        /// <typeparam name="TException">The type of the exception that has to be retired.</typeparam>
        /// <param name="retryCount">The amount of retries should happen when a failure occurs during the publishing.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="retryCount" /> is less than zero.</exception>
        public void WithExponentialRetry<TException>(int retryCount) where TException : Exception
        {
            Guard.NotLessThanOrEqualTo(retryCount, 0, nameof(retryCount), "Requires a retry count for the exponential retry that's greater than zero");

            AsyncRetryPolicy asyncPolicy = Policy.Handle<TException>().WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            RetryPolicy syncPolicy = Policy.Handle<TException>().WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            SetOrUpdatePolicy(asyncPolicy, syncPolicy);
        }

        /// <summary>
        /// <para>
        ///     Makes the <see cref="EventGridPublisherClient" /> resilient by breaking the circuit-function
        ///     if the <paramref name="exceptionsAllowedBeforeBreaking" /> are handled by the policy.
        ///     The circuit will stay broken for <paramref name="durationOfBreak" />.
        /// </para>
        /// <para>
        ///     Any attempt to execute the function while the circuit is broken will result in a <see cref="BrokenCircuitException" />.
        /// </para>
        /// </summary>
        /// <typeparam name="TException">The type of the exception that has to be retired.</typeparam>
        /// <param name="exceptionsAllowedBeforeBreaking">The amount of exceptions that are allowed before breaking the circuit.</param>
        /// <param name="durationOfBreak">The duration the circuit must stay broken.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="exceptionsAllowedBeforeBreaking" /> is less or equal to zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="durationOfBreak" /> is a negative time range.</exception>
        public void WithCircuitBreaker<TException>(
            int exceptionsAllowedBeforeBreaking,
            TimeSpan durationOfBreak)
            where TException : Exception
        {
            Guard.NotLessThanOrEqualTo(exceptionsAllowedBeforeBreaking, 0, nameof(exceptionsAllowedBeforeBreaking), "Requires a allowed exceptions count before the circuit breaker activates that's greater than zero");
            Guard.NotLessThanOrEqualTo(durationOfBreak, TimeSpan.Zero, nameof(durationOfBreak), "Requires a circuit breaker time duration that's a positive time range");

            AsyncCircuitBreakerPolicy asyncPolicy = Policy.Handle<TException>().CircuitBreakerAsync(exceptionsAllowedBeforeBreaking, durationOfBreak);
            CircuitBreakerPolicy syncPolicy = Policy.Handle<TException>().CircuitBreaker(exceptionsAllowedBeforeBreaking, durationOfBreak);
            SetOrUpdatePolicy(asyncPolicy, syncPolicy);
        }

        private void SetOrUpdatePolicy(AsyncPolicy asyncPolicy, Policy syncPolicy)
        {
            if (AsyncPolicy is AsyncNoOpPolicy && SyncPolicy is NoOpPolicy)
            {
                AsyncPolicy = asyncPolicy;
                SyncPolicy = syncPolicy;
            }
            else
            {
                AsyncPolicy.WrapAsync(asyncPolicy);
                SyncPolicy.Wrap(syncPolicy);
            }
        }
    }
}