namespace Arcus.EventGrid.Contracts
{
    /// <summary>
    /// Represents the schema structure of the event to be published or received on an Azure Event Grid resource.
    /// </summary>
    public enum EventSchema
    {
        /// <summary>
        /// Represents the events according to the Event Grid event schema.
        /// </summary>
        EventGrid = 1,

        /// <summary>
        /// Represents the events according to the CloudEvents event schema.
        /// </summary>
        CloudEvent = 2
    }
}
