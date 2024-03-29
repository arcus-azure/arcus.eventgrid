﻿using System;
using Microsoft.Extensions.Logging;

namespace Arcus.EventGrid.Testing.Logging
{
    /// <summary>
    /// Represents an <see cref="ILogger"/> model that writes log messages towards the standard console output.
    /// </summary>
    [Obsolete("Use our xUnit test logger 'XunitTestLogger' in the 'Arcus.Testing.Logging' package to delegate diagnostic information to the test output")]
    public class ConsoleLogger : ILogger
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
            var message = formatter(state, exception);
            Console.WriteLine($"{DateTimeOffset.UtcNow:s} {logLevel} > {message}");
        }

        /// <summary>
        /// Checks if the given <paramref name="logLevel" /> is enabled.
        /// </summary>
        /// <param name="logLevel">level to be checked.</param>
        /// <returns><c>true</c> if enabled.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
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