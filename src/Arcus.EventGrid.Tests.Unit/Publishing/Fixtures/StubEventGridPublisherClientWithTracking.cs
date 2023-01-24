using System;
using Arcus.Observability.Correlation;
using Azure;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Arcus.EventGrid.Tests.Unit.Publishing.Fixtures
{
    public class StubEventGridPublisherClientWithTracking : EventGridPublisherClientWithTracking
    {
        public StubEventGridPublisherClientWithTracking(EventGridPublisherClientWithTrackingOptions options = null) 
            : base("https://savanh-grid-lab.westcentralus-1.eventgrid.azure.net/api/events",
                    new EventGridPublisherClient(new Uri("https://savanh-grid-lab.westcentralus-1.eventgrid.azure.net/api/events"), new AzureKeyCredential("IvjeulNI4OzwQOAA+Ba7gefZr230oqmBQptaz6UOUMc=")),
                   Mock.Of<ICorrelationInfoAccessor>(), 
                   options ?? new EventGridPublisherClientWithTrackingOptions(), 
                   NullLogger<EventGridPublisherClient>.Instance)
        {
        }
    }
}
