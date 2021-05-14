﻿using System;

namespace Arcus.EventGrid.Publishing.Interfaces
{
    /// <summary>
    /// Represents a contract for the adding resilience to the <see cref="IBuilder"/> 
    /// after the <see cref="IEventGridPublisherBuilderWithAuthenticationKey.UsingAuthenticationKey"/> is called.
    /// </summary>
    public interface IEventGridPublisherBuilderWithExponentialRetry : IEventGridPublisherBuilderWithCircuitBreaker
    {
        /// <summary>
        /// Makes the <see cref="IEventGridPublisher" /> resilient by retrying <paramref name="retryCount" /> times with exponential back-off.
        /// </summary>
        /// <typeparam name="TException">The type of the exception that has to be retired.</typeparam>
        /// <param name="retryCount">The amount of retries should happen when a failure occurs during the publishing.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="retryCount" /> is less than zero.</exception>
        IEventGridPublisherBuilderWithCircuitBreaker WithExponentialRetry<TException>(int retryCount) where TException : Exception;
    }
}