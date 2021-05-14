using System;
using Polly.CircuitBreaker;

namespace Arcus.EventGrid.Publishing.Interfaces
{
    /// <summary>
    /// Represents a contract for the adding resilience to the <see cref="IBuilder"/> 
    /// after the <see cref="IEventGridPublisherBuilderWithExponentialRetry.WithExponentialRetry{TException}"/> is called.
    /// </summary>
    public interface IEventGridPublisherBuilderWithCircuitBreaker : IBuilder
    {
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
        IBuilder WithCircuitBreaker<TException>(int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak) where TException : Exception;
    }
}