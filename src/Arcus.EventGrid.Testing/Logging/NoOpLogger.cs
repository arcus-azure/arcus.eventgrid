using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.EventGrid.Testing.Logging
{
    /// <summary>
    /// Represents an <see cref="ILogger"/> model without any implementation.
    /// </summary>
    [Obsolete("Unused logger implementation with the same functionality as " + nameof(NullLogger) + " will be removed in future major")]
    public class NoOpLogger : ILogger
    {
        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <param name="logLevel">The entry will be written on this level.</param>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">The function to create a <c>string</c> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Deliberately don't log anything
        }

        /// <summary>
        /// Checks if the given <paramref name="logLevel" /> is enabled.
        /// </summary>
        /// <param name="logLevel">level to be checked.</param>
        /// <returns><c>true</c> if enabled.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
