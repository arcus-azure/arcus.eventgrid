using System;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;
using Polly;
using Polly.CircuitBreaker;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="EventGridPublishingServiceCollection"/> for cleaner and more user-friendly Azure EventGrid publishing setup.
    /// </summary>
    public static class EventGridPublishingServiceCollectionExtensions
    {
        /// <summary>
        /// Makes the <see cref="IEventGridPublisher" /> resilient by retrying <paramref name="retryCount" /> times with exponential back-off.
        /// </summary>
        /// <typeparam name="TException">The type of the exception that has to be retired.</typeparam>
        /// <param name="services">The registered application services available in the application.</param>
        /// <param name="retryCount">The amount of retries should happen when a failure occurs during the publishing.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="retryCount" /> is less than zero.</exception>
        public static EventGridPublishingServiceCollection WithExponentialRetry<TException>(
            this EventGridPublishingServiceCollection services,
            int retryCount) where TException : Exception
        {
            Guard.NotLessThan(retryCount, 0, nameof(retryCount), "Requires a retry count for the exponential retry that's greater than zero");

            AsyncPolicy exponentialRetryPolicy =
                Policy.Handle<TException>()
                      .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            services.Services.AddSingleton<IEventGridPublisher>(serviceProvider =>
            {
                var publisher = serviceProvider.GetService<IEventGridPublisher>();
                if (publisher is null)
                {
                    throw new InvalidOperationException(
                        "Cannot add the exponential back-off resilience to the Azure EventGrid publisher because no such publisher was registered before this point, " +
                        "make sure that you correctly call the 'AddEventGridPublishing' before calling this extension");
                }

                return new ResilientEventGridPublisher(exponentialRetryPolicy, publisher);
            });

            return services;
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
        /// <param name="services">The registered application services available in the application.</param>
        /// <param name="exceptionsAllowedBeforeBreaking">The amount of exceptions that are allowed before breaking the circuit.</param>
        /// <param name="durationOfBreak">The duration the circuit must stay broken.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="exceptionsAllowedBeforeBreaking" /> is less or equal to zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="durationOfBreak" /> is a negative time range.</exception>
        public static EventGridPublishingServiceCollection WithCircuitBreaker<TException>(
            this EventGridPublishingServiceCollection services,
            int exceptionsAllowedBeforeBreaking,
            TimeSpan durationOfBreak) where TException : Exception
        {
            Guard.NotLessThanOrEqualTo(exceptionsAllowedBeforeBreaking, 0, nameof(exceptionsAllowedBeforeBreaking), "Requires a allowed exceptions count before the circuit breaker activates that's greater than zero");
            Guard.NotLessThan(durationOfBreak, TimeSpan.Zero, nameof(durationOfBreak), "Requires a circuit breaker time duration that's a positive time range");

            AsyncPolicy circuitBreakerPolicy =
                Policy.Handle<TException>()
                      .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking, durationOfBreak);

            services.Services.AddSingleton<IEventGridPublisher>(serviceProvider =>
            {
                var publisher = serviceProvider.GetService<IEventGridPublisher>();
                if (publisher is null)
                {
                    throw new InvalidOperationException(
                        "Cannot add the circuit breaker resilience to the Azure EventGrid publisher because no such publisher was registered before this point, " +
                        "make sure that you correctly call the 'AddEventGridPublishing' before calling this extension");
                }

                return new ResilientEventGridPublisher(circuitBreakerPolicy, publisher);
            });

            return services;
        }
    }
}
