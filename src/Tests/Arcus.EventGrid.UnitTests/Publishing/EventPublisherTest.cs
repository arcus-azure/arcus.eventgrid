using System;
using Arcus.EventGrid.Publishing;
using Xunit;

namespace Arcus.EventGrid.UnitTests.Publishing
{
    public class EventPublisherTest
    {
        [Theory]
        [InlineData("https://savanh-grid-lab.westcentralus-1.eventgrid.azure.net/api/events", "")]
        [InlineData("https://savanh-grid-lab.westcentralus-1.eventgrid.azure.net/api/events", null)]
        [InlineData("", "IvjeulNI4OzwQOAA+Ba7gefZr230oqmBQptaz6UOUMc=")]
        [InlineData(null, "IvjeulNI4OzwQOAA+Ba7gefZr230oqmBQptaz6UOUMc=")]
        public void TestCreatePublisherShouldFail(string topicEndpoint, string endpointKey)
        {
            Assert.Throws<ArgumentException>(
                () => EventGridPublisher.Create(topicEndpoint, endpointKey));
        }

    }
}
