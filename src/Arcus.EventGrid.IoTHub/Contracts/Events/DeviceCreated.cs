using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.IoTHub.Contracts.Events.Data;

namespace Arcus.EventGrid.IoTHub.Contracts.Events
{
    public class DeviceCreated : Event<IoTDeviceEventData>
    {
        public DeviceCreated()
        {
        }

        public DeviceCreated(string id) : base(id)
        {
        }

        public DeviceCreated(string id, string subject) : base(id, subject)
        {
        }

        public override string DataVersion { get; } = "1";
        public override string EventType { get;  } = "Microsoft.Devices.DeviceCreated";
    }
}