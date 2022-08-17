using Arcus.Observability.Correlation;
using Arcus.Security.Core;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Arcus.EventGrid.Tests.Unit.Publishing.Fixtures
{
    public class StubEventGridPublisherClientWithTracking : EventGridPublisherClientWithTracking
    {
        public StubEventGridPublisherClientWithTracking() 
            : base("<topic-endpoint>", 
                   "<authentication-key-secret-name>", 
                   Mock.Of<ISecretProvider>(), 
                   Mock.Of<ICorrelationInfoAccessor>(), 
                   new EventGridPublisherClientWithTrackingOptions(), 
                   NullLogger<EventGridPublisherClient>.Instance)
        {
        }
    }
}
