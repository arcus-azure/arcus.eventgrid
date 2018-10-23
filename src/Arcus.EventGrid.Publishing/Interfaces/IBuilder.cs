namespace Arcus.EventGrid.Publishing.Interfaces
{
    /// <summary>
    /// Final builder state contract from which the <see cref="EventGridPublisher"/> can be created.
    /// This interface can later be used to return different kinds of <see cref="EventGridPublisherBuilderResult"/>s.
    /// </summary>
    public interface IBuilder
    {
        /// <summary>
        /// Creates a <see cref="EventGridPublisher"/> instance for the specified builder values.
        /// </summary>
        IEventGridPublisher Build();
    }
}