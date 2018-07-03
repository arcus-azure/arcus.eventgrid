using System;

namespace Arcus.EventGrid.Contracts
{
    /// <summary>
    ///     Represents an event published on a topic.
    ///     (Offical schema documentation - https://docs.microsoft.com/en-us/azure/event-grid/event-schema#event-schema)
    /// </summary>
    /// <typeparam name="TData">Type of data payload</typeparam>
    public class Event<TData>
    {
        /// <summary>
        ///     Event data specific to the resource provider.
        /// </summary>
        public TData Data { get; set; }

        /// <summary>
        ///     The schema version of the data object. The publisher defines the schema version.
        /// </summary>
        public string DataVersion { get; set; }

        /// <summary>
        ///     The time the event is generated based on the provider's UTC time.
        /// </summary>
        public DateTimeOffset EventTime { get; set; }

        /// <summary>
        ///     One of the registered event types for this event source.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        ///     Unique identifier for the event.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     The schema version of the event metadata. Event Grid defines the schema of the top-level properties. Event Grid
        ///     provides this value.
        /// </summary>
        public string MetadataVersion { get; set; }

        /// <summary>
        ///     Publisher-defined path to the event subject.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        ///     Full resource path to the event source. This field is not writeable. Event Grid provides this value.
        /// </summary>
        public string Topic { get; set; }
    }
}