namespace Arcus.EventGrid.Tests.Integration.Publishing.Fixture
{
    /// <summary>
    /// Represents a data model to count the calls to the <see cref="SabotageMockTopicEndpoint"/>.
    /// </summary>
    public class EndpointCallCount
    {
        /// <summary>
        /// Gets or sets the amount of calls to the <see cref="SabotageMockTopicEndpoint"/>.
        /// </summary>
        public int Count { get; set; }
    }
}