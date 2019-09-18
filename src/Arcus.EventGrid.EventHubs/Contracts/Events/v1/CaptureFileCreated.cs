using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.EventHubs.Contracts.Events.v1.Data;
using Microsoft.Azure.EventGrid.Models;
using System;

namespace Arcus.EventGrid.EventHubs.Contracts.Events.v1
{
    [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(EventGridEvent<EventHubCaptureFileCreatedEventData>) + "<" + nameof(EventHubCaptureFileCreatedEventData) + "> instead")]
    public class CaptureFileCreated : Event<EventHubCaptureEventData>
    {
        public CaptureFileCreated()
        {
        }

        public CaptureFileCreated(string id) : base(id)
        {
        }

        public CaptureFileCreated(string id, string subject) : base(id, subject)
        {
        }

        public override string DataVersion { get;  } = "1";
        public override string EventType { get;  } = "Microsoft.EventHub.CaptureFileCreated";
    }
}
