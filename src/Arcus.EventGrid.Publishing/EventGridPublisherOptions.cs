using Arcus.EventGrid.Publishing.Interfaces;

namespace Arcus.EventGrid.Publishing
{
    /// <summary>
    /// Represents the additional options to configure the <see cref="IEventGridPublisher"/> instance via the <see cref="EventGridPublisherBuilder"/>.
    /// </summary>
    public class EventGridPublisherOptions
    {
        /// <summary>
        /// <para>Gets or sets the flag indicating whether or not the <see cref="IEventGridPublisher"/> should track the Azure Event Grid topic dependency.</para>
        /// <para>For more information about dependency tracking <see href="https://observability.arcus-azure.net/features/writing-different-telemetry-types#dependencies"/>.</para>
        /// </summary>
        public bool EnableDependencyTracking { get; set; } = false;
    }
}
