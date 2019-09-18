using Microsoft.Azure.EventGrid.Models;
using System;

namespace Arcus.EventGrid.IoTHub.Contracts.Events.v1.Data
{
    /// <summary>
    ///     Event data contract for IoT Hub device events
    /// </summary>
    [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(IotHubDeviceCreatedEventData) + " for example or any  other  'IotHubDevice...' event data models" )]
    public class IoTDeviceEventData
    {
        public string DeviceId { get; set; }
        public string HubName { get; set; }
        public DateTime OperationTimestamp { get; set; }
        public string OpType { get; set; }
        public Twin Twin { get; set; }
    }
}