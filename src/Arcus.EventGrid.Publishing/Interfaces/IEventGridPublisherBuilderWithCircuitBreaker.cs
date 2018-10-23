using System;
using Polly.CircuitBreaker;

namespace Arcus.EventGrid.Publishing.Interfaces
{
    /// <summary>
    /// Contract for the adding resilience to the <see cref="IBuilder"/> 
    /// after the <see cref="IEventGridPublisherBuilderWithExponentialRetry.WithExponentialRetry{TException}"/> is called.
    /// </summary>
    public interface IEventGridPublisherBuilderWithCircuitBreaker : IBuilder
    {
        /// <summary>
        /// Makes the <see cref="IEventGridPublisher"/> resilient by breaking the circuit/function if the <param name="exceptionsAllowedBeforeBreaking"></param>
        /// are handled by the policy. The circuit will stay broken for <paramref name="durationOfBreak"/>.
        /// Any attempt to execute the function while the circuit is broken will result in a <see cref="BrokenCircuitException"/>.
        /// </summary>
        /// <typeparam name="TException">The type of the exception that has to be retired.</typeparam>
        /// <param name="exceptionsAllowedBeforeBreaking">The amount of exceptions that are allowed before breaking the circuit.</param>
        /// <param name="durationOfBreak">The duration the circuit must stay broken.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="exceptionsAllowedBeforeBreaking"/> must be greater than zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="durationOfBreak"/> must be a positive time interval.</exception>
        IBuilder WithCircuitBreaker<TException>(int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak) where TException : Exception;
    }
}