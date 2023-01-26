using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.EventGrid.Testing.Infrastructure.Hosts;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;

namespace Arcus.EventGrid.Tests.Unit.Testing.Fixture
{
    public class InMemoryEventConsumerHost : EventConsumerHost
    {
        public InMemoryEventConsumerHost(ILogger logger) : base(logger)
        {
        }

        public void ReceiveEvent(CloudEvent cloudEvent)
        {
            string rawReceivedEvents = JsonSerializer.Serialize(cloudEvent);
            EventsReceived(rawReceivedEvents);
        }

        public void ReceiveEvent(EventGridEvent eventGridEvent)
        {
            string rawReceivedEvents = JsonSerializer.Serialize(eventGridEvent);
            EventsReceived(rawReceivedEvents);
        }

        public void ReceiveEvent(string raw)
        {
            EventsReceived(raw);
        }
    }
}
