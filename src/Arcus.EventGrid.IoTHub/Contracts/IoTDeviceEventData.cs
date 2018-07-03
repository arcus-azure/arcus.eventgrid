using System;

namespace Arcus.EventGrid.IoTHub.Contracts
{
    /// <summary>
    ///     Event data contract for IoT Hub device events
    /// </summary>
    public class IoTDeviceEventData
    {
        public string DeviceId { get; set; }
        public string HubName { get; set; }
        public DateTime OperationTimestamp { get; set; }
        public string OpType { get; set; }
        public Twin Twin { get; set; }
    }
}