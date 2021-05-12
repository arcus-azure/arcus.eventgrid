using System;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;
using Polly;
using Polly.CircuitBreaker;
using Polly.NoOp;

namespace Arcus.EventGrid.Publishing
{
    /// <summary>
    ///     Result of the minimum required values to create <see cref="EventGridPublisher" /> instances, but also start-point of
    ///     extending the instance.
    ///     The required and optional values are therefore split in separate classes and cannot be manipulated with casting.
    /// </summary>
    internal class EventGridPublisherBuilderResult : IEventGridPublisherBuilderWithExponentialRetry
    {
        private readonly Uri _topicEndpoint;
        private readonly string _authenticationKey;
        private readonly AsyncPolicy _resilientPolicy;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventGridPublisherBuilderResult" /> class.
        /// </summary>
        /// <param name="topicEndpoint">Url of the custom Event Grid topic</param>
        /// <param name="authenticationKey">Authentication key for the custom Event Grid topic</param>
        /// <exception cref="ArgumentException">The topic endpoint must not be empty and is required</exception>
        /// <exception cref="ArgumentException">The authentication key must not be empty and is required</exception>
        /// <exception cref="UriFormatException">The topic endpoint must be a HTTP endpoint.</exception>
        internal EventGridPublisherBuilderResult(Uri topicEndpoint, string authenticationKey)
            : this(topicEndpoint, authenticationKey, Policy.NoOpAsync())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventGridPublisherBuilderResult" /> class.
        /// </summary>
        /// <param name="topicEndpoint">Url of the custom Event Grid topic</param>
        /// <param name="authenticationKey">Authentication key for the custom Event Grid topic</param>
        /// <param name="resilientPolicy">The policy to use making the publishing resilient.</param>
        /// <exception cref="ArgumentException">The topic endpoint must not be empty and is required</exception>
        /// <exception cref="ArgumentException">The authentication key must not be empty and is required</exception>
        /// <exception cref="ArgumentNullException">The resilient policy is required</exception>
        /// <exception cref="UriFormatException">The topic endpoint must be a HTTP endpoint.</exception>
        internal EventGridPublisherBuilderResult(
            Uri topicEndpoint,
            string authenticationKey,
            AsyncPolicy resilientPolicy)
        {
            Guard.NotNull(topicEndpoint, nameof(topicEndpoint), "The topic endpoint must be specified");
            Guard.NotNullOrWhitespace(authenticationKey, nameof(authenticationKey), "The authentication key must not be empty and is required");
            Guard.NotNull(resilientPolicy, nameof(resilientPolicy), "The resilient policy is required via this construction, use other constructor otherwise");
            Guard.For<UriFormatException>(
                () => topicEndpoint.Scheme != Uri.UriSchemeHttp
                      && topicEndpoint.Scheme != Uri.UriSchemeHttps,
                $"The topic endpoint must be and HTTP or HTTPS endpoint but is: {topicEndpoint.Scheme}");
            
            _topicEndpoint = topicEndpoint;
            _authenticationKey = authenticationKey;
            _resilientPolicy = resilientPolicy;
        }

        /// <summary>
        ///     Makes the <see cref="IEventGridPublisher" /> resilient by retrying <paramref name="retryCount" /> times with
        ///     exponential back-off.
        /// </summary>
        /// <typeparam name="TException">The type of the exception that has to be retired.</typeparam>
        /// <param name="retryCount">The amount of retries should happen when a failure occurs during the publishing.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="retryCount" /> must be greater than zero.</exception>
        public IEventGridPublisherBuilderWithCircuitBreaker WithExponentialRetry<TException>(int retryCount) where TException : Exception
        {
            Guard.NotLessThan(retryCount, 0, nameof(retryCount));

            AsyncPolicy exponentialRetryPolicy = 
                Policy.Handle<TException>()
                      .WaitAndRetryAsync(
                          retryCount,
                          retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            return new EventGridPublisherBuilderResult(_topicEndpoint, _authenticationKey, exponentialRetryPolicy);
        }

        /// <summary>
        ///     Makes the <see cref="IEventGridPublisher" /> resilient by breaking the circuit/function if the
        ///     <paramref name="exceptionsAllowedBeforeBreaking" />
        ///     are handled by the policy. The circuit will stay broken for <paramref name="durationOfBreak" />.
        ///     Any attempt to execute the function while the circuit is broken will result in a
        ///     <see cref="BrokenCircuitException" />.
        /// </summary>
        /// <typeparam name="TException">The type of the exception that has to be retired.</typeparam>
        /// <param name="exceptionsAllowedBeforeBreaking">The amount of exceptions that are allowed before breaking the circuit.</param>
        /// <param name="durationOfBreak">The duration the circuit must stay broken.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The <paramref name="exceptionsAllowedBeforeBreaking" /> must be greater
        ///     than zero.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="durationOfBreak" /> must be a positive time interval.</exception>
        public IBuilder WithCircuitBreaker<TException>(int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak) where TException : Exception
        {
            Guard.NotLessThanOrEqualTo(exceptionsAllowedBeforeBreaking, 0, nameof(exceptionsAllowedBeforeBreaking));
            Guard.NotLessThan(durationOfBreak, TimeSpan.Zero, nameof(durationOfBreak));

            AsyncPolicy circuitBreakerPolicy = 
                Policy.Handle<TException>()
                      .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking, durationOfBreak);

            var resilientPolicy = _resilientPolicy is AsyncNoOpPolicy
                ? circuitBreakerPolicy
                : _resilientPolicy.WrapAsync(circuitBreakerPolicy);

            return new EventGridPublisherBuilderResult(_topicEndpoint, _authenticationKey, resilientPolicy);
        }

        /// <summary>
        ///     Creates a <see cref="IEventGridPublisher" /> instance for the specified builder values.
        /// </summary>
        public IEventGridPublisher Build()
        {
            return new EventGridPublisher(_topicEndpoint, _authenticationKey, _resilientPolicy);
        }
    }
}