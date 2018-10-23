using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.IoTHub.Contracts.Events.Data;

namespace Arcus.EventGrid.IoTHub.Contracts.Events
{
    public class DeviceDeleted : Event<IoTDeviceEventData>
    {
        public DeviceDeleted()
        {
        }

        public DeviceDeleted(string id) : base(id)
        {
        }

        public DeviceDeleted(string id, string subject) : base(id, subject)
        {
        }

        public override string DataVersion { get;  } = "1";
        public override string EventType { get;  } = "Microsoft.Devices.DeviceDeleted";
    }
}