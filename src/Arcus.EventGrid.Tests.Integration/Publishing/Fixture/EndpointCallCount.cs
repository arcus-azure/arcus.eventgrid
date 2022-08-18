namespace Arcus.EventGrid.Tests.Integration.Publishing.Fixture
{
    /// <summary>
    /// Represents a data model to count the calls to the <see cref="MockTopicEndpoint"/>.
    /// </summary>
    public class EndpointCallCount
    {
        /// <summary>
        /// Gets or sets the amount of calls to the <see cref="MockTopicEndpoint"/>.
        /// </summary>
        public int Count { get; set; }
    }
}