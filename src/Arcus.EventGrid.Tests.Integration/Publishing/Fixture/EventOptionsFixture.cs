using System;
using System.Collections.Generic;
using Azure.Messaging.EventGrid;
using Xunit;
using GuardNet;

namespace Arcus.EventGrid.Tests.Integration.Publishing.Fixture
{
    /// <summary>
    /// Represents a test fixture to verify whether event options are being passed correctly.
    /// </summary>
    public class EventOptionsFixture
    {
        private readonly string _key1 = $"key-{Guid.NewGuid()}", _value1 = $"value-{Guid.NewGuid()}";
        private readonly string _key2 = $"key-{Guid.NewGuid()}", _value2 = $"value-{Guid.NewGuid()}";

        /// <summary>
        /// Gets the custom dependency ID used during the event publishing options.
        /// </summary>
        public string DependencyId { get; } = $"parent-{Guid.NewGuid()}";

        /// <summary>
        /// Applies additional values to the <paramref name="options"/> to verify the changed behavior later.
        /// </summary>
        /// <param name="options">The user options to influence the correlation during event publishing.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public void ApplyOptions(EventGridPublisherClientWithTrackingOptions options)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of user options that influence the correlation during event publishing");

            options.GenerateDependencyId = () => DependencyId;
            options.AddTelemetryContext(new Dictionary<string, object> { [_key1] = _value1 });
            options.AddTelemetryContext(new Dictionary<string, object> { [_key2] = _value2, [_key1] = _value2 });
        }

        /// <summary>
        /// Asserts on the influenced correlation during event publishing.
        /// </summary>
        /// <param name="logMessage">The log message that should contain the influenced correlation during event publishing.</param>
        public void AssertTelemetry(string logMessage)
        {
            Assert.NotNull(logMessage);
            Assert.Contains(_key1, logMessage);
            Assert.DoesNotContain(_value1, logMessage);
            Assert.Contains(_key2, logMessage);
            Assert.Contains(_value2, logMessage);
        }
    }
}
