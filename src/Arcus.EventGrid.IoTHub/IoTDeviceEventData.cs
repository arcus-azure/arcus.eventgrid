using System;

namespace Arcus.EventGrid.IoTHub
{
    /// <summary>
    /// Contract for IoT Hub device event
    /// </summary>
    public class IoTDeviceEventData
    {
        public Twin Twin { get; set; }
        public string HubName { get; set; }
        public string DeviceId { get; set; }
        public DateTime OperationTimestamp { get; set; }
        public string OpType { get; set; }
    }
}