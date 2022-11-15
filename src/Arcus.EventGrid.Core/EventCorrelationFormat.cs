namespace Arcus.EventGrid.Core
{
    /// <summary>
    /// Represents the event correlation format of the send-out events.
    /// </summary>
    public enum EventCorrelationFormat
    {
        /// <summary>
        /// Uses the W3C event correlation system with traceparent and tracestate to represent parent-child relationship.
        /// </summary>
        W3C,

        /// <summary>
        /// Uses the hierarchical event correlation system with Root-Id and Request-Id to represent parent-child relationship.
        /// </summary>
        Hierarchical
    }
}
