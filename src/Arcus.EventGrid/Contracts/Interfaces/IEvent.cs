using System;

namespace Arcus.EventGrid.Contracts.Interfaces
{
    public interface IEvent
    {
        /// <summary>
        ///     The schema version of the data object. The publisher defines the schema version.
        /// </summary>
        string DataVersion { get; set; }

        /// <summary>
        ///     The time the event is generated based on the provider's UTC time.
        /// </summary>
        DateTimeOffset EventTime { get; set; }

        /// <summary>
        ///     One of the registered event types for this event source.
        /// </summary>
        string EventType { get; set; }

        /// <summary>
        ///     Unique identifier for the event.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        ///     The schema version of the event metadata. Event Grid defines the schema of the top-level properties. Event Grid
        ///     provides this value.
        /// </summary>
        string MetadataVersion { get; set; }

        /// <summary>
        ///     Publisher-defined path to the event subject.
        /// </summary>
        string Subject { get; set; }

        /// <summary>
        ///     Full resource path to the event source. This field is not writeable. Event Grid provides this value.
        /// </summary>
        string Topic { get; set; }
    }
}