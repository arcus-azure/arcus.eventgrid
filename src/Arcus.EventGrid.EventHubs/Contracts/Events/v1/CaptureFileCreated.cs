using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.EventHubs.Contracts.Events.v1.Data;

namespace Arcus.EventGrid.EventHubs.Contracts.Events.v1
{
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
