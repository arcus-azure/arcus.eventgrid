using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.IoTHub.Contracts.Events.v1.Data;
using Microsoft.Azure.EventGrid.Models;
using System;

namespace Arcus.EventGrid.IoTHub.Contracts.Events.v1
{
    [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(EventGridEvent<IotHubDeviceDeletedEventData>) + "<" + nameof(IotHubDeviceDeletedEventData) + "> instead")]
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