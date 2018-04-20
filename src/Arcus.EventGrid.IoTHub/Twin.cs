using System;

namespace Arcus.EventGrid.IoTHub
{
    public class Twin
    {
        public string DeviceId { get; set; }
        public string Etag { get; set; }
        public object DeviceEtag { get; set; }
        public string Status { get; set; }
        public DateTime StatusUpdateTime { get; set; }
        public string ConnectionState { get; set; }
        public DateTime LastActivityTime { get; set; }
        public int CloudToDeviceMessageCount { get; set; }
        public string AuthenticationType { get; set; }
        public X509Thumbprint X509Thumbprint { get; set; }
        public int Version { get; set; }
        public TwinProperties Properties { get; set; }
    }
}