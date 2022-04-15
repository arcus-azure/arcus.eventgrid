using System;

namespace Arcus.EventGrid.Contracts.Interfaces
{
    /// <summary>
    /// Represents an abstraction on the Azure EventGrid and CloudEvents event models to have a cannonical event structure.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        ///     The schema version of the data object. The publisher defines the schema version.
        /// </summary>
        string DataVersion { get; }

        /// <summary>
        ///     The time the event is generated based on the provider's UTC time.
        /// </summary>
        DateTimeOffset EventTime { get; }

        /// <summary>
        ///     One of the registered event types for this event source.
        /// </summary>
        string EventType { get; }

        /// <summary>
        ///     Unique identifier for the event.
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     The schema version of the event metadata. Event Grid defines the schema of the top-level properties. Event Grid
        ///     provides this value.
        /// </summary>
        string MetadataVersion { get; }

        /// <summary>
        ///     Publisher-defined path to the event subject.
        /// </summary>
        string Subject { get; }

        /// <summary>
        ///     Full resource path to the event source. This field is not writable. Event Grid provides this value.
        /// </summary>
        string Topic { get; }
    }
}