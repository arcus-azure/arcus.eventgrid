using System;
using Microsoft.Extensions.Logging;

namespace Arcus.EventGrid.Testing.Logging
{
    public class NoOpLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Deliberately don't log anything
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
