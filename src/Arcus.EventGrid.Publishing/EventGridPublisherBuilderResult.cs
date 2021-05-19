using System;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.CircuitBreaker;
using Polly.NoOp;

namespace Arcus.EventGrid.Publishing
{
    /// <summary>
    /// <para>Result of the minimum required values to create <see cref="EventGridPublisher" /> instances, but also start-point of extending the instance.</para>
    /// <para>The required and optional values are therefore split in separate classes and cannot be manipulated with casting.</para>
    /// </summary>
    internal class EventGridPublisherBuilderResult : IEventGridPublisherBuilderWithExponentialRetry
    {
        private readonly Uri _topicEndpoint;
        private readonly string _authenticationKey;
        private readonly AsyncPolicy _resilientPolicy;
        private readonly ILogger _logger;
        private readonly Action<EventGridPublisherOptions> _configureOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisherBuilderResult" /> class.
        /// </summary>
        /// <param name="topicEndpoint">The URL of custom Azure Event Grid topic.</param>
        /// <param name="authenticationKey">The authentication key for the custom Azure Event Grid topic.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid topic.</param>
        /// <param name="configureOptions">The additional function to configure optional settings on the <see cref="IEventGridPublisher"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="topicEndpoint"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="authenticationKey"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> is not a valid HTTP endpoint.</exception>
        internal EventGridPublisherBuilderResult(Uri topicEndpoint, string authenticationKey, ILogger logger, Action<EventGridPublisherOptions> configureOptions)
            : this(topicEndpoint, authenticationKey, Policy.NoOpAsync(), logger, configureOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisherBuilderResult" /> class.
        /// </summary>
        /// <param name="topicEndpoint">The URL of custom Azure Event Grid topic.</param>
        /// <param name="authenticationKey">The authentication key for the custom Azure Event Grid topic.</param>
        /// <param name="resilientPolicy">The policy to use making the publishing resilient.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid topic</param>
        /// <param name="configureOptions">The additional function to configure optional settings on the <see cref="IEventGridPublisher"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="topicEndpoint"/> or the <paramref name="resilientPolicy"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="authenticationKey"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> is not a valid HTTP endpoint.</exception>
        internal EventGridPublisherBuilderResult(
            Uri topicEndpoint,
            string authenticationKey,
            AsyncPolicy resilientPolicy,
            ILogger logger,
            Action<EventGridPublisherOptions> configureOptions)
        {
            Guard.NotNull(topicEndpoint, nameof(topicEndpoint), "The topic endpoint must be specified");
            Guard.NotNullOrWhitespace(authenticationKey, nameof(authenticationKey), "The authentication key must not be empty and is required");
            Guard.NotNull(resilientPolicy, nameof(resilientPolicy), "The resilient policy is required via this construction, use other constructor otherwise");
            Guard.For(() => topicEndpoint.Scheme != Uri.UriSchemeHttp 
                            && topicEndpoint.Scheme != Uri.UriSchemeHttps,
                new UriFormatException("Requires a topic endpoint that has a HTTP or HTTPS scheme"));
            
            _topicEndpoint = topicEndpoint;
            _authenticationKey = authenticationKey;
            _resilientPolicy = resilientPolicy;
            _logger = logger ?? NullLogger.Instance;
            _configureOptions = configureOptions;
        }

        /// <summary>
        /// Makes the <see cref="IEventGridPublisher" /> resilient by retrying <paramref name="retryCount" /> times with exponential back-off.
        /// </summary>
        /// <typeparam name="TException">The type of the exception that has to be retired.</typeparam>
        /// <param name="retryCount">The amount of retries should happen when a failure occurs during the publishing.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="retryCount" /> is less than zero.</exception>
        public IEventGridPublisherBuilderWithCircuitBreaker WithExponentialRetry<TException>(int retryCount) where TException : Exception
        {
            Guard.NotLessThan(retryCount, 0, nameof(retryCount), "Requires a retry count for the exponential retry that's greater than zero");

            AsyncPolicy exponentialRetryPolicy = 
                Policy.Handle<TException>()
                      .WaitAndRetryAsync(
                          retryCount,
                          retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            return new EventGridPublisherBuilderResult(_topicEndpoint, _authenticationKey, exponentialRetryPolicy, _logger, _configureOptions);
        }

        /// <summary>
        /// <para>
        ///     Makes the <see cref="IEventGridPublisher" /> resilient by breaking the circuit-function
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
        public IBuilder WithCircuitBreaker<TException>(int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak) where TException : Exception
        {
            Guard.NotLessThanOrEqualTo(exceptionsAllowedBeforeBreaking, 0, nameof(exceptionsAllowedBeforeBreaking), 
                "Requires a allowed exceptions count before the circuit breaker activates that's greater than zero");
            Guard.NotLessThan(durationOfBreak, TimeSpan.Zero, nameof(durationOfBreak), 
                "Requires a circuit breaker time duration that's a positive time range");

            AsyncPolicy circuitBreakerPolicy = 
                Policy.Handle<TException>()
                      .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking, durationOfBreak);

            AsyncPolicy resilientPolicy = _resilientPolicy is AsyncNoOpPolicy
                ? circuitBreakerPolicy
                : _resilientPolicy.WrapAsync(circuitBreakerPolicy);

            return new EventGridPublisherBuilderResult(_topicEndpoint, _authenticationKey, resilientPolicy, _logger, _configureOptions);
        }

        /// <summary>
        /// Creates a <see cref="IEventGridPublisher" /> instance for the specified builder values.
        /// </summary>
        public IEventGridPublisher Build()
        {
            var options = new EventGridPublisherOptions();
            _configureOptions?.Invoke(options);
            
            return new EventGridPublisher(_topicEndpoint, _authenticationKey, _resilientPolicy, _logger, options);
        }
    }
}