using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.EventHubs.Contracts.Events.Data;

namespace Arcus.EventGrid.EventHubs.Contracts.Events
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

        public override string DataVersion { get; set; } = "1";
        public override string EventType { get; set; } = "Microsoft.EventHub.CaptureFileCreated";
    }
}
