using Microsoft.Azure.EventGrid.Models;
using System;

namespace Arcus.EventGrid.IoTHub.Contracts.Events.v1.Data
{
    [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(IotHubDeviceCreatedEventData) + " for example or any  other  'IotHubDevice...' event data models" )]
    public class Twin
    {
        public string AuthenticationType { get; set; }
        public int CloudToDeviceMessageCount { get; set; }
        public string ConnectionState { get; set; }
        public object DeviceEtag { get; set; }
        public string DeviceId { get; set; }
        public string Etag { get; set; }
        public DateTime LastActivityTime { get; set; }
        public TwinProperties Properties { get; set; }
        public string Status { get; set; }
        public DateTime StatusUpdateTime { get; set; }
        public int Version { get; set; }
        public X509Thumbprint X509Thumbprint { get; set; }
    }
}