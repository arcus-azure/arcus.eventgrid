using Arcus.Observability.Correlation;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Arcus.EventGrid.Tests.Unit.Publishing.Fixtures
{
    public class StubEventGridPublisherClientWithTracking : EventGridPublisherClientWithTracking
    {
        public StubEventGridPublisherClientWithTracking(
            EventGridPublisherClientWithTrackingOptions options = null,
            ILogger<EventGridPublisherClient> logger = null) 
            : base("https://savanh-grid-lab.westcentralus-1.eventgrid.azure.net/api/events",
                    new InMemoryEventGridPublisherClient(),
                   Mock.Of<ICorrelationInfoAccessor>(), 
                   options ?? new EventGridPublisherClientWithTrackingOptions(), 
                   logger ?? NullLogger<EventGridPublisherClient>.Instance)
        {
        }
    }
}
